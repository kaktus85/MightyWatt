using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Management;
using System.Collections.Generic;
using System.Text;

namespace MightyWatt
{
    public delegate void DataUpdateDelegate();
    public delegate void ConnectionUpdateDelegate();

    class Com
    {
        // COM port
        private SerialPort port;
        private const Parity parity = Parity.None;
        private const StopBits stopBits = StopBits.One;
        private const int readTimeout = 300;
        private const int writeTimeout = 300;
        private const int loopDelay = 50; // delay between read/write attempts
        public const int LoadDelay = readTimeout + writeTimeout;
        private const int baudRate = 115200;
        private const int dataBits = 8;
        private readonly char[] newLine = new char[] {'\r', '\n'};
        private const byte SERIES_RESISTANCE_ID = 28;
        private const byte REMOTE_ID = 29;
        private const byte QDC = 30;
        private const byte IDN = 31;
        private const byte messageLength = 7;
        private const string IDN_RESPONSE = "MightyWatt";
        private string activePortName;
        public event ConnectionUpdateDelegate ConnectionUpdatedEvent;

        // communication (data in/out) loop
        private BackgroundWorker comLoop;
        public event DataUpdateDelegate DataUpdatedEvent;
        private byte[] readData;
        private byte[] dataToWrite;
        private string errorList;
        // private DateTime readDataTimeStamp;

        // device capabilities
        private string firmwareVersion, boardRevision;
        private double maxIdac, maxIadc, maxVdac, maxVadc, maxPower, dvmInputResistance;
        private int temperatureThreshold;

        // recent values
        private double current;
        private double voltage;
        private double seriesResistance = 0;
        private MeasurementValues values;

        public Com()
        {
            values = new MeasurementValues(voltage, current);
            // creates new serialport object and sets it
            port = new SerialPort();
            port.BaudRate = baudRate;
            port.DataBits = dataBits;
            port.Parity = parity;
            port.ReadTimeout = readTimeout;
            port.StopBits = stopBits;
            port.WriteTimeout = writeTimeout;
            port.NewLine = newLine;          

            readData = new byte[messageLength]; // initializes read data (data from the load) array
            dataToWrite = new byte[] { 0 }; // initializes write data (data from the load) array to default

            this.comLoop = new BackgroundWorker(); // background worker for the main communication loop
            this.comLoop.WorkerReportsProgress = false;
            this.comLoop.WorkerSupportsCancellation = true;
            this.comLoop.DoWork += new DoWorkEventHandler(comLoop_DoWork);

            this.DataUpdatedEvent += calculateValues;
        }

        // connects to a specific COM port
        public /*async*/ void Connect(byte portNumber, bool rtsDtrEnable)
        {
            if (port.IsOpen) // terminates any previous connection
            {
                Disconnect();
            }
            port.PortNumber = portNumber;
            if (rtsDtrEnable)
            {
                port.DtrControl = DTR_CONTROL.ENABLE;
                port.RtsControl = RTS_CONTROL.ENABLE;
            }
            else
            {
                port.DtrControl = DTR_CONTROL.DISABLE;
                port.RtsControl = RTS_CONTROL.DISABLE;
            }

            port.Open();
            while (port.IsOpen == false) { } // wait for port to open    
            Thread.Sleep(300); // give time to Arduinos that reset upon port opening     
            port.ReadExisting();

            bool identified = false;
            for (int i = 0; i < 3; i++)
            {
                if (identify())
                {
                    identified = true;
                    break;
                }
            }

            if (identified) // three attempts
            {
                activePortName = port.PortName;
                ConnectionUpdatedEvent?.Invoke(); // raise connection updated event
                this.comLoop.RunWorkerAsync();
            }
            else
            {
                port.Close();
                while (port.IsOpen) { } // wait for port to close
                throw new System.IO.IOException("Wrong device");
            }
        }

        // disconnect from a COM port
        public void Disconnect()
        {
            if (port.IsOpen)
            {
                try
                {
                    this.comLoop.CancelAsync(); // stops monitoring for new data    
                    port.Close();
                    while (port.IsOpen) { } // wait for port to close
                }
                catch (System.IO.IOException ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }

            // resets all read data
            for (int i = 0; i < readData.Length; i++)
            {
                readData[i] = 0;
            }

            // resets all port and device information
            activePortName = null;
            firmwareVersion = null;
            boardRevision = null;
            maxIdac = 0;
            maxIadc = 0;
            maxVdac = 0;
            maxVadc = 0;
            maxPower = 0;
            dvmInputResistance = 0;

            // raise connection updated event
            if (ConnectionUpdatedEvent != null)
            {
                ConnectionUpdatedEvent();
            }

            // last data update (with reset data)
            if (DataUpdatedEvent != null)
            {
                DataUpdatedEvent(); // event for data update complete
            }
        }

        // reads data from load and then raises update event 
        private void comLoop_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!comLoop.CancellationPending)
            {
                try
                {
                    setToLoad();
                    readFromLoad();                    
                    checkStatus();                    
                    if (this.DataUpdatedEvent != null)
                    {
                        this.DataUpdatedEvent(); // event for data update complete
                    }
                }
                catch (Exception ex)
                {
                    if (ex is TimeoutException || ex is System.IO.IOException)
                    {
                        Disconnect();
                        System.Windows.MessageBox.Show(ex.Message + "\nTo continue, please reconnect load.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                    if (ex is InvalidOperationException)
                    {
                        System.Windows.MessageBox.Show(ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                    throw;
                }
                Thread.Sleep(loopDelay);
            }
            e.Cancel = true;
        }

        // reads a line from the load
        private /*async Task<string>*/ string ReadLine()
        {
            return /*await*/ port.ReadLine/*Async*/();
        }

        // sends the current data block to the load
        private void setToLoad()
        {
            byte[] dataCopy = new byte[dataToWrite.Length];
            for (int i = 0; i < dataToWrite.Length; i++)
            {
                dataCopy[i] = dataToWrite[i];
            }
           
            port.Write/*Async*/(dataCopy);
            dataToWrite = new byte[] { 0 }; // reset the data block    
            Thread.Sleep(2);        
        }

        // reads available data block from the load
        private/* async*/ void readFromLoad()
        {
            byte[] newData = /*await*/ port.ReadBytes/*Async*/(messageLength);
            if (newData != null)
            {
                readData = newData;
            }            
            //port.ReadExisting(); // discards any excess data          
        }

        // tries to read identification string from the load, if succeedes, queries device capabilities
        private /*async Task<bool>*/ bool identify()
        {         
            try
            {
                port.ReadExisting(); // flush data
                port.Write/*Async*/(new byte[] { IDN });
                string response = /*await*/ ReadLine();
                if (response.Contains(IDN_RESPONSE))
                {
                    queryCapabilities();
                    return true;
                }
                return false;
            }
            catch (Exception /*ex*/)
            {
               // System.Windows.MessageBox.Show(ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        // reads device parameters
        private /*async*/ void queryCapabilities()
        {
            port.Write/*Async*/(new byte[] { QDC });
            firmwareVersion = /*await*/ ReadLine();
            boardRevision = /*await*/ ReadLine();
            maxIdac = Double.Parse(/*await*/ ReadLine()) / 1000;
            maxIadc = Double.Parse(/*await*/ ReadLine()) / 1000;
            maxVdac = Double.Parse(/*await*/ ReadLine()) / 1000;
            maxVadc = Double.Parse(/*await*/ ReadLine()) / 1000;
            maxPower = Double.Parse(/*await*/ ReadLine()) / 1000;
            dvmInputResistance = Double.Parse(/*await*/ ReadLine());
            temperatureThreshold = int.Parse(/*await*/ ReadLine());

            // check firmware version
            string[] firmware = firmwareVersion.Split('.');
            bool firmwareVersionOK = false;
            if (firmware.Length >= 3)
            {
                int[] fw = new int[3];
                if (int.TryParse(firmware[0], out fw[0]) && int.TryParse(firmware[1], out fw[1]) && int.TryParse(firmware[2], out fw[2]))
                {
                    if (fw[0] > Load.MinimumFWVersion[0])
                    {
                        firmwareVersionOK = true;
                    }
                    else if (fw[0] == Load.MinimumFWVersion[0])
                    {
                        if (fw[1] > Load.MinimumFWVersion[1])
                        {
                            firmwareVersionOK = true;
                        }
                        else if (fw[1] == Load.MinimumFWVersion[1])
                        {
                            if (fw[2] >= Load.MinimumFWVersion[2])
                            {
                                firmwareVersionOK = true;
                            }
                        }
                    }
                }
                if (!firmwareVersionOK)
                {
                    System.Windows.MessageBox.Show("Firmware version is lower than minimum required version for this software\nMinimum firmware version: " + Load.MinimumFirmwareVersion + "\nThis load firmware version: " + firmwareVersion, "Firmware version error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                }                
            }  
            else
            {
                System.Windows.MessageBox.Show("The load did not report its firmware version", "Unknown firmware version", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
            }             
        }

        // calculates voltage and current from received values
        private void calculateValues()
        {
            this.current = (Convert.ToDouble(readData[0]) * 256.0 + Convert.ToDouble(readData[1])) / 1000.0;
            this.voltage = (Convert.ToDouble(readData[2]) * 256.0 + Convert.ToDouble(readData[3])) / 1000.0;
            values.current = this.current;
            values.voltage = this.voltage;
        }

        // this method handles the communication protocol of sending the data to the load
        public void Set(Modes mode, double value)
        {
            value = validateValues(mode, value); // validate input
            byte[] data;
            uint val = Convert.ToUInt32(value * 1000);
            switch (mode)
            {
                case Modes.Current:
                case Modes.Voltage:
                case Modes.VoltageInvertedPhase:
                case Modes.MPPT:
                    {
                        data = new byte[3];
                        data[1] = Convert.ToByte((val >> 8) & 0xFF);
                        data[2] = Convert.ToByte(val & 0xFF);
                        break;
                    }
                case Modes.Power:
                case Modes.Resistance:
                    {
                        data = new byte[4];
                        data[1] = Convert.ToByte((val >> 16) & 0xFF);
                        data[2] = Convert.ToByte((val >> 8) & 0xFF);
                        data[3] = Convert.ToByte(val & 0xFF);
                        break;
                    }
                default:
                    {
                        data = new byte[1];
                        break;
                    }
            }            
            data[0] = Convert.ToByte((1 << 7) | ((data.Length - 1) << 5) | (byte)mode);
            dataToWrite = data;
        }

        // immediately stops the load by setting the current to zero
        public void ImmediateStop()
        {
            byte[] data = new byte[3];
            data[0] = Convert.ToByte((1 << 7) | ((data.Length - 1) << 5) | (byte)Modes.Current);
            data[1] = 0;
            data[2] = 0;
            dataToWrite = data;
            try
            {
                setToLoad();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // manages the 2-wire or 4-wire connection
        public void SetRemote(bool remoteEnabled)
        {
            byte[] data = new byte[2];
            data[0] = Convert.ToByte((1 << 7) | ((data.Length - 1) << 5) | REMOTE_ID);
            data[1] = Convert.ToByte(remoteEnabled);
            dataToWrite = data;
        }

        // checks values for validity
        private double validateValues(Modes mode, double value)
        {
            if (value < 0)
            {
                return 0;
            }
            else
            {
                if (activePortName != null) // values are validated only when a device is connected
                {
                    switch (mode)
                    {
                        case Modes.Current:
                        case Modes.MPPT:
                            {
                                if ((value > MaxIadc) && (value > MaxIdac))
                                {
                                    throw new ArgumentOutOfRangeException("Set current out of range.", (Exception)null);
                                }
                                break;
                            }
                        case Modes.Power:
                            {
                                if (value > MaxPower)
                                {
                                    throw new ArgumentOutOfRangeException("Set power out of range.", (Exception)null);
                                }
                                break;
                            }
                        case Modes.Resistance:
                            {
                                if (value > DvmInputResistance)
                                {
                                    throw new ArgumentOutOfRangeException("Set resistance out of range.", (Exception)null); ;
                                }
                                break;
                            }
                        case Modes.Voltage:
                            {
                                if ((value > MaxVadc) && (value > MaxVdac))
                                {
                                    throw new ArgumentOutOfRangeException("Set voltage out of range.", (Exception)null);
                                }
                                break;
                            }
                        case Modes.VoltageInvertedPhase:
                            {
                                if (value > MaxVadc)
                                {
                                    throw new ArgumentOutOfRangeException("Set voltage out of range.", (Exception)null);
                                }
                                break;
                            }
                        default:
                            {
                                throw new System.IO.InvalidDataException("Invalid input");
                            }
                    }
                }
            }
            return value;
        }

        // checks for errors reported by load
        private void checkStatus()
        {
            this.errorList = null;
            if (readData[6] > 0)
            {
                this.errorList = "Following errors were detected:";
                foreach (byte b in Enum.GetValues(typeof(Status)))
                {
                    if ((readData[6] & b) > 0)
                    {
                        this.errorList += "\n";
                        this.errorList += Enum.GetName(typeof(Status), b);
                    }
                }
            }
        }

        // gets selected value of I, P, R or V
        public double GetValue(Modes mode)
        {
            switch (mode)
            {
                case Modes.Current:
                case Modes.MPPT:
                    {
                        return this.current;
                    }
                case Modes.Power:                
                    {
                        return this.voltage * this.current;
                    }
                case Modes.Resistance:
                    {
                        if (this.current == 0)
                        {
                            return this.DvmInputResistance; // zero current resistance is the voltmeter input resistance
                        }
                        else
                        {
                            return this.voltage / this.current;
                        }
                    }
                case Modes.Voltage:
                case Modes.VoltageInvertedPhase:
                    {
                        return this.voltage;
                    }
            }
            return 0;
        }

        // gets or sets series resistance to the load
        public double SeriesResistance
        {
            get
            {
                return seriesResistance;
            }
            set
            {
                UInt16 val = Convert.ToUInt16(value * 1000);
                byte[] data = new byte[3];
                data[0] = Convert.ToByte((1 << 7) | (2 << 5) | SERIES_RESISTANCE_ID);
                data[1] = Convert.ToByte((val >> 8) & 0xFF);
                data[2] = Convert.ToByte(val & 0xFF);
                dataToWrite = data;
                seriesResistance = value;
            }
        }

        public MeasurementValues PresentValues
        {
            get
            {
                return values;
            }
        }

        public string PortName
        {
            get
            {
                return this.activePortName;
            }
        }

        public string FirmwareVersion
        {
            get
            {
                return this.firmwareVersion;
            }
        }

        public string BoardRevision
        {
            get
            {
                return this.boardRevision;
            }
        }

        public double MaxIdac
        {
            get
            {
                return this.maxIdac;
            }
        }

        public double MaxIadc
        {
            get
            {
                return this.maxIadc;
            }
        }

        public double MaxVdac
        {
            get
            {
                return this.maxVdac;
            }
        }

        public double MaxVadc
        {
            get
            {
                return this.maxVadc;
            }
        }

        public double MaxPower
        {
            get
            {
                return this.maxPower;
            }
        }

        public double DvmInputResistance
        {
            get
            {
                return this.dvmInputResistance;
            }
        }

        public int TemperatureThreshold
        {
            get
            {
                return this.temperatureThreshold;
            }
        }

        public double Temperature
        {
            get
            {
                return Convert.ToDouble(readData[4]);
            }
        }

        public bool Remote
        {
            get
            {
                return Convert.ToBoolean(readData[5]);
            }
        }

        public string ErrorList
        {
            get
            {
                return this.errorList;
            }
        }
    }    
}
