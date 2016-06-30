using System;
using System.IO.Ports;
using System.Threading;
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
        private const string newLine = "\r\n";
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

        public Com()
        {
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
        public void Connect(string portName)
        {
            if (port.IsOpen) // terminates any previous connection
            {
                Disconnect();
            }
            port.PortName = portName;
            port.Open();
            while (port.IsOpen == false) { } // wait for port to open            
            if (identify() || identify() || identify()) // for some stupid reason I do not know, it sometimes requires up to three attempts to connect
            {
                activePortName = port.PortName;
                if (ConnectionUpdatedEvent != null)
                {
                    ConnectionUpdatedEvent(); // raise connection updated event
                }
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
            if (this.DataUpdatedEvent != null)
            {
                this.DataUpdatedEvent(); // event for data update complete
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
                    if (ex is System.TimeoutException || ex is System.IO.IOException)
                    {
                        Disconnect();
                        System.Windows.MessageBox.Show(ex.Message + "\nTo continue, please reconnect load.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                    if (ex is System.InvalidOperationException)
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

        // sends the current data block to the load
        private void setToLoad()
        {
            port.Write(dataToWrite, 0, dataToWrite.Length);
            dataToWrite = new byte[] { 0 }; // reset the data block
            Thread.Sleep(2); // wait for write to finish
        }

        // reads available data block from the load
        private void readFromLoad()
        {
            for (byte i = 0; i < messageLength; i++)
            {
                readData[i] = (byte)(port.ReadByte() & 0xFF);
            }
            port.DiscardInBuffer(); // discards any excess data
        }

        // tries to read identification string from the load, if succeedes, queries device capabilities
        private bool identify()
        {
            byte[] buffer = new byte[] { IDN };
            try
            {
                port.ReadExisting(); // flush data
                port.Write(buffer, 0, 1);
                string response = port.ReadLine();
                if (response.Contains(IDN_RESPONSE))
                {
                    queryCapabilities();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        // reads device parameters
        private void queryCapabilities()
        {
            byte[] buffer = new byte[] { QDC };
            port.Write(buffer, 0, 1);
            this.firmwareVersion = port.ReadLine();
            this.boardRevision = port.ReadLine();
            this.maxIdac = Double.Parse(port.ReadLine()) / 1000;
            this.maxIadc = Double.Parse(port.ReadLine()) / 1000;
            this.maxVdac = Double.Parse(port.ReadLine()) / 1000;
            this.maxVadc = Double.Parse(port.ReadLine()) / 1000;
            this.maxPower = Double.Parse(port.ReadLine()) / 1000;
            this.dvmInputResistance = Double.Parse(port.ReadLine());
            this.temperatureThreshold = int.Parse(port.ReadLine());
        }

        // calculates voltage and current from received values
        private void calculateValues()
        {
            this.current = (Convert.ToDouble(readData[0]) * 256.0 + Convert.ToDouble(readData[1])) / 1000.0;
            this.voltage = (Convert.ToDouble(readData[2]) * 256.0 + Convert.ToDouble(readData[3])) / 1000.0;
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

    // Following code for retrieving COM port friendly names is based on code by Dario Santarelli https://dariosantarelli.wordpress.com/2010/10/18/c-how-to-programmatically-find-a-com-port-by-friendly-name/
    // WMI calls to the more obvious Win32_SerialPort run very slowly on certain machines, hence the code below which calls to Win32_PnPEntity. Thank you Microsoft again :/

    internal class ProcessConnection
    {
        public static ConnectionOptions ProcessConnectionOptions()
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Impersonation = ImpersonationLevel.Impersonate;
            options.Authentication = AuthenticationLevel.Default;
            options.EnablePrivileges = true;
            return options;
        }

        public static ManagementScope ConnectionScope(string machineName, ConnectionOptions options, string path)
        {
            ManagementScope connectScope = new ManagementScope();
            connectScope.Path = new ManagementPath(@"\\" + machineName + path);
            connectScope.Options = options;
            connectScope.Connect();
            return connectScope;
        }
    }

    public class COMPortInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public COMPortInfo() { }

        public static List<COMPortInfo> GetCOMPortsInfo()
        {
            List<COMPortInfo> comPortInfoList = new List<COMPortInfo>();

            ConnectionOptions options = ProcessConnection.ProcessConnectionOptions();
            ManagementScope connectionScope = ProcessConnection.ConnectionScope(Environment.MachineName, options, @"\root\CIMV2");

            ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0");
            ManagementObjectSearcher comPortSearcher = new ManagementObjectSearcher(connectionScope, objectQuery);

            using (comPortSearcher)
            {
                string caption = null;
                foreach (ManagementObject obj in comPortSearcher.Get())
                {
                    if (obj != null)
                    {
                        object captionObj = obj["Caption"];
                        if (captionObj != null)
                        {
                            caption = captionObj.ToString();
                            if (caption.Contains("(COM"))
                            {
                                COMPortInfo comPortInfo = new COMPortInfo();
                                comPortInfo.Name = caption.Substring(caption.LastIndexOf("(COM")).Replace("(", string.Empty).Replace(")", string.Empty).Trim();
                                comPortInfo.Description = caption.Trim();
                                comPortInfoList.Add(comPortInfo);
                            }
                        }
                    }
                }
            }
            return comPortInfoList;
        }
    }
}
