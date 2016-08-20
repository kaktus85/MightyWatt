using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.IO;
using System.ComponentModel;

namespace MightyWatt
{
    public enum Modes : byte { Current, Voltage, Power, Resistance, VoltageInvertedPhase, MPPT };
   // public enum Units : byte { A, V, W, Ω, VINV };
    public enum Status : byte { READY = 0, CURRENT_OVERLOAD = 1, VOLTAGE_OVERLOAD = 2, POWER_OVERLOAD = 4, OVERHEAT = 8 }
    public enum TimeUnits : byte { ms, s, min, h }
    public enum Comparison { LessThan, MoreThan }
    public enum ProgramModes : byte { Constant, Ramp };
    public enum Boards : byte { Zero, Uno };

    public delegate void GuiUpdateDelegate();
    public delegate void WatchdogStopDelegate();
    public delegate void ErrorDelegate(string error);    

    class Load
    {
        // connection
        private Com device;
        private bool isConnected = false;        

        // program
        public BindingList<ProgramItem> ProgramItems = new BindingList<ProgramItem>();
        private List<DateTime> programItemsStartTime;        
        private BackgroundWorker worker = new BackgroundWorker();        
        private bool isManual = true; // indicates whether the load control is manual or programmatic       
        private bool cancel = false; // this cancels execution of a single program item
        private int currentItemNumber; // number of currently executed program item

        // loops
        private int currentLoop; // current loop number
        public int TotalLoops { get; set; } // total number of loops, 0 for infinite

        // GUI
        public static readonly string[] ModeNames = { "Current", "Voltage", "Power", "Resistance", "Inverted phase voltage", "Max power point tracker" };
        public static readonly string[] UnitSymbols = { "A", "V", "W", "Ω", "V", "A"};
        DateTime lastGuiUpdate = DateTime.Now;
        private double guiUpdatePeriod = 0.3;
        public event GuiUpdateDelegate GuiUpdateEvent;
        public event GuiUpdateDelegate ProgramStartedEvent; // occurs when program starts
        public event GuiUpdateDelegate ProgramStoppedEvent; // occurs when program finishes
        public event ConnectionUpdateDelegate ConnectionUpdateEvent; // occurs when connection of load changes

        // error management
        public event ErrorDelegate Error;

        // watchdog
        public event WatchdogStopDelegate WatchdogStop; // event that is raised when watchdog has stopped the load
        public bool WatchdogEnabled { get; set; }
        public Modes WatchdogMode { get; set; }
        public Comparison WatchdogCompare { get; set; }
        public string WatchdogValue { get; set; }

        // logging
        private File file;
        private bool isLoggingManual = false;
        private bool isLoggingProgram = false;
        private double loggingPeriod = 1; // logging period in seconds
        public TimeUnits LoggingTimeUnit { get; set; } // time units for logging
        private DateTime lastManualLog = DateTime.MinValue;
        private DateTime lastProgramLog = DateTime.MinValue;

        // minimum firmware version
        public static readonly int[] MinimumFWVersion = new int[] { 2, 5, 7 };

        public Load()
        {
            this.device = new Com(); // load over COM port            

            // worker for program mode
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_Finished;
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = false;

            device.ConnectionUpdatedEvent += connectionUpdated; // pass connection updated event
            device.DataUpdatedEvent += updateGui; // updates 
            device.DataUpdatedEvent += checkError; // errors
            device.DataUpdatedEvent += watchdog; // watchdog
            device.DataUpdatedEvent += log; // data logging

            TotalLoops = 1; // standard single loop
            LoggingTimeUnit = TimeUnits.s; // default unit second
        }

        // connect to selected COM port
        public void Connect(string portName, Boards board)
        {
            Disconnect();
            try
            {
                device.Connect(byte.Parse(portName.Replace("COM", string.Empty)), board == Boards.Zero);
            }
            catch (System.IO.IOException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // disconnect from COM port
        public void Disconnect()
        {
            try
            {
                device.Set(Modes.Current, 0);
                System.Threading.Thread.Sleep(Com.LoadDelay); // wait for setting the load to zero current
                this.device.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // pass connection updated event and update isConnected property
        private void connectionUpdated()
        {
            if (device.PortName == null)
            {
                this.isConnected = false;
            }
            else
            {
                this.isConnected = true;                
            }
            ConnectionUpdateEvent();
        }

        // passes the set method
        public void Set(Modes mode, double value)
        {
            device.Set(mode, value);
        }

        // passes the set remote method
        public void SetRemote(bool remoteEnabled)
        {
            device.SetRemote(remoteEnabled);
        }

        // starts execution of program
        public void Start()
        {
            Stop(); // stops the previous program first
            this.isManual = false;

            if (ProgramStartedEvent != null)
            {
                ProgramStartedEvent(); // raise program started event
            }

            this.worker.RunWorkerAsync(); // starts execution in different thread
        }

        // cancels worker, stopping the execution of program or stops the load in manual mode
        public void Stop()
        {
            device.Set(Modes.Current, 0); // set load to zero
            if (this.worker.IsBusy)
            {
                this.cancel = true;
                this.worker.CancelAsync();
            }
        }

        // immediately stops the load, called in emergency (watchdog, overloads)
        public void ImmediateStop()
        {
            device.ImmediateStop();
        }

        // skips execution of single program line
        public void Skip()
        {
            if (this.worker.IsBusy)
            {
                this.cancel = true;
            }
        }

        // runs constant mode program item
        private void Constant(int programItemNumber)
        {
            if (ProgramItems[programItemNumber].Value != null)
            {
                device.Set(ProgramItems[programItemNumber].Mode, (double)(ProgramItems[programItemNumber].Value));
            }
            else
            {
                device.Set(ProgramItems[programItemNumber].Mode, device.GetValue(ProgramItems[programItemNumber].Mode));
            }
            programItemsStartTime.Add(DateTime.Now);
            // loop
            while (((DateTime.Now - programItemsStartTime[programItemNumber]).TotalSeconds < ProgramItems[programItemNumber].Duration) && (this.cancel == false))
            {
                if (ProgramItems[programItemNumber].SkipEnabled) // check skip condition
                {
                    if (valueComparer(ProgramItems[programItemNumber].SkipValue, ProgramItems[programItemNumber].SkipMode, ProgramItems[programItemNumber].SkipComparator))
                    {
                        break;
                    }
                }
                System.Threading.Thread.Sleep(2); // decrease CPU load
            }
            this.cancel = false;
        }

        // runs ramp mode program item
        private void Ramp(int programItemNumber)
        {
            double startValue, currentValue;
            if (ProgramItems[programItemNumber].StartingValue != null)
            {
                startValue = (double)(ProgramItems[programItemNumber].StartingValue);
            }
            else
            {
                startValue = device.GetValue(ProgramItems[programItemNumber].Mode);
            }
            currentValue = startValue;
            programItemsStartTime.Add(DateTime.Now);

            // loop
            while (((DateTime.Now - programItemsStartTime[programItemNumber]).TotalSeconds < ProgramItems[programItemNumber].Duration) && (cancel == false))
            {
                device.Set(ProgramItems[programItemNumber].Mode, currentValue);
                if (ProgramItems[programItemNumber].SkipEnabled) // check skip condition
                {
                    if (valueComparer(ProgramItems[programItemNumber].SkipValue, ProgramItems[programItemNumber].SkipMode, ProgramItems[programItemNumber].SkipComparator))
                    {
                        break;
                    }
                }
                currentValue = startValue + (ProgramItems[programItemNumber].FinalValue - startValue) / ProgramItems[programItemNumber].Duration * ((DateTime.Now - programItemsStartTime[programItemNumber]).TotalSeconds);
                System.Threading.Thread.Sleep(2); // decrease CPU load 
            }
            this.cancel = false;
        }

        // program execution
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            currentLoop = 0;
            while ((currentLoop < TotalLoops) || (TotalLoops == 0)) // loop while current number of loop is less than the total number of loops, or there is an infinite loop (TotalLoops = 0)
            {
                programItemsStartTime = new List<DateTime>(); // creates fresh list with start times

                for (currentItemNumber = 0; currentItemNumber < ProgramItems.Count; currentItemNumber++)
                {
                    if (worker.CancellationPending)
                    {
                        break;
                    }

                    if (ProgramItems[currentItemNumber].ProgramMode == ProgramModes.Constant)
                    {
                        Constant(currentItemNumber);
                    }
                    else if (ProgramItems[currentItemNumber].ProgramMode == ProgramModes.Ramp)
                    {
                        Ramp(currentItemNumber);
                    }
                }

                if (worker.CancellationPending)
                {
                    break;
                }

                currentLoop++;
            }
        }

        // final cleanup after program is finished or stopped
        private void worker_Finished(object sender, RunWorkerCompletedEventArgs e)
        {
            device.Set(Modes.Current, 0); // stop load
            this.isManual = true;
            if (ProgramStoppedEvent != null)
            {
                ProgramStoppedEvent(); // raise program started event
            }
        }
        
        // returns time elapsed since the start of a single program item
        private double elapsed(int programItemNumber)
        {
            if (programItemsStartTime != null)
            {
                if (programItemsStartTime.Count > programItemNumber)
                {
                    return (DateTime.Now - programItemsStartTime[programItemNumber]).TotalSeconds;
                }                
            }            
            return 0;
        }

        // checks for errors from the load
        private void checkError()
        {
            if (this.device.ErrorList != null)
            {
                this.Error(this.device.ErrorList); // raise error event in case the error list is not empty
            }
        }

        // can disable load based on values in the watchdog group box
        private void watchdog()
        {
            if (this.WatchdogEnabled)
            {
                double value;
                if (double.TryParse(this.WatchdogValue, out value))
                {
                    if (valueComparer(value, this.WatchdogMode, this.WatchdogCompare))
                    {
                        watchdogStop();
                    }                    
                }
            }
        }

        // compares the current measured quantity to a given value
        // returns true if the measured quantity is less than / more than [comparator] the given value
        private bool valueComparer(double value, Modes mode, Comparison comparator)
        {
            switch (mode)
            {
                case Modes.Current:
                    {
                        return (comparator == Comparison.LessThan) == (Current < value);
                    }
                case Modes.MPPT:
                case Modes.Power:
                    {
                        return (comparator == Comparison.LessThan) == (Power < value);
                    }
                case Modes.Resistance:
                    {
                        return (comparator == Comparison.LessThan) == (Resistance < value);
                    }
                case Modes.Voltage:
                case Modes.VoltageInvertedPhase:
                    {
                        return (comparator == Comparison.LessThan) == (Voltage < value);
                    }
            }
            return false;
        }

        // stops the load and raises WatchdogStop event
        private void watchdogStop()
        {            
            if (this.worker.IsBusy)
            {
                this.cancel = true;
                this.worker.CancelAsync();
            }
            ImmediateStop(); // immediately stop the load            
            if (WatchdogStop != null)
            {
                WatchdogStop();
            }
        }

        // creates a new file for logging
        public void NewFile(string filePath) 
        {
            CloseFile();
            try
            {
                file = new File(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // closes current file
        public void CloseFile() 
        {
            if (file != null)
            {
                file.Close();
            }
        }

        // manages data logging        
        private void log()
        {
            if (file != null)
            {
                if (file.FilePath != null)
                {
                    // manual control logging
                    if (IsLoggingManual && isManual)
                    {
                        if ((DateTime.Now - lastManualLog).TotalSeconds >= LoggingPeriod)
                        {
                            file.WriteData(Current, Voltage, Temperature, Remote);
                            lastManualLog = DateTime.Now;
                        }
                    }
                    // program control logging
                    else if (IsLoggingProgram && !isManual)
                    {
                        if ((DateTime.Now - lastProgramLog).TotalSeconds >= LoggingPeriod)
                        {
                            file.WriteData(Current, Voltage, Temperature, Remote);
                            lastProgramLog = DateTime.Now;
                        }
                    }
                }
            }
        }     
        
        // periodically raise gui update event
        private void updateGui()
        {
            if ((DateTime.Now - this.lastGuiUpdate).TotalSeconds >= guiUpdatePeriod)
            {
                this.lastGuiUpdate = DateTime.Now;
                if (this.GuiUpdateEvent != null)
                {
                    this.GuiUpdateEvent();
                }
            }
        }

        // window with load capabilities
        public void ShowDeviceInfo()
        {
            if (this.IsConnected == true)
            {
                DeviceInfo deviceInfo = new DeviceInfo(this.device); 
                deviceInfo.ShowDialog();
            }
        }
        
        public string PortName
        {
            get
            {
                return this.device.PortName;
            }
        }

        public double Voltage
        {
            get
            {
                return this.device.GetValue(Modes.Voltage);
            }
        }

        public double Current
        {
            get
            {
                return this.device.GetValue(Modes.Current);
            }
        }

        public double Power
        {
            get
            {
                return this.device.GetValue(Modes.Power);
            }
        }

        public double Resistance
        {
            get
            {
                return this.device.GetValue(Modes.Resistance);
            }
        }

        public double Temperature
        {
            get
            {
                return this.device.Temperature;
            }
        }

        public double SeriesResistance
        {
            get
            {
                return this.device.SeriesResistance;
            }
            set
            {                
                this.device.SeriesResistance = value;
            }
        }

        public bool IsConnected
        {
            get
            {
                return this.isConnected;
            }
        }

        public bool Local
        {
            get
            {
                return !this.device.Remote;
            }
        }

        public bool Remote
        {
            get
            {
                return this.device.Remote;
            }
        }

        public bool IsManual
        {
            get
            {
                return this.isManual;
            }
        }

        public int CurrentItemNumber
        {
            get
            {
                return this.currentItemNumber;
            }
        }

        public double TotalRemainingTime
        {
            get
            {
                double time = 0;
                for (int i = currentItemNumber; i < ProgramItems.Count; i++)
                {
                    time += ProgramItems[i].Duration;
                }
                time -= elapsed(currentItemNumber);
                return time;
            }
        }

        public double ItemRemainingTime
        {
            get
            {
                if (ProgramItems.Count > 0)
                {
                    return ProgramItems[currentItemNumber].Duration - elapsed(currentItemNumber);
                }
                else
                {
                    return 0;
                }
            }
        }

        public int CurrentLoop
        {
            get
            {
                return this.currentLoop;
            }
        }

        public string FilePath
        {
            get
            {
                if (this.file != null)
                {
                    if (this.file.FilePath != null)
                    {
                        return this.file.FilePath;
                    }
                }
                return "No file";
            }
        }     

        public bool IsLoggingManual
        {
            get
            {
                return this.isLoggingManual;
            }
            set
            {
                this.isLoggingManual = value;
            }
        }

        public bool IsLoggingProgram
        {
            get
            {
                return this.isLoggingProgram;
            }
            set
            {
                this.isLoggingProgram = value;
            }
        }

        public double LoggingPeriod // logging period in seconds
        {
            get
            {
                return this.loggingPeriod;
            }
            set
            {                
                if (value >= 0) // value must be non-negative
                {
                    loggingPeriod = value;
                }
                else
                {
                    loggingPeriod = 0;
                }
            }
        }

        public static string MinimumFirmwareVersion
        {
            get
            {
                return MinimumFWVersion[0].ToString() + "." + MinimumFWVersion[1].ToString() + "." + MinimumFWVersion[2].ToString();
            }
        }
    }
}
