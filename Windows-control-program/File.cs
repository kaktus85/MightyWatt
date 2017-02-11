using System;
using System.Text;
using System.IO;

namespace MightyWatt
{
    public class File
    {
        private StreamWriter file;
        private string filePath;
        private DateTime startTime; // time at creation of file
        private const string NUMBER_FORMAT = "f3"; // default number format (mV, mA resolution)
        private const string TEMPERATURE_NUMBER_FORMAT = "f0"; // default number format for temperature (°C)
        public const int columnCount = 6; // number of columns
        public const char delimiter = '\t';

        // creates a new file with header and notes the starting time
        public File(string filePath)
        {
            file = new StreamWriter(filePath, true, new UTF8Encoding());
            this.filePath = filePath;
            startTime = DateTime.Now;
            file.AutoFlush = true;
            file.WriteLine("# MightyWatt Log File");
            file.WriteLine("# Started on\t{0}\t{1}", startTime.ToShortDateString(), startTime.ToLongTimeString());
            file.WriteLine("# Current [A]\tVoltage [V]\tTime since start [s]\tTemperature [deg C]\tLocal[l]/Remote[r]");
         }

        // closes the file
        public void Close()
        {
            file.Close();
            this.filePath = null;
        }

        // writes a single line of load data to the file
        public void WriteData(double current, double voltage, double temperature, bool remote)
        {
            string lr;
            if (remote)
            {
                lr = "r";
            }
            else
            {
                lr = "l";
            }
            file.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", current.ToString(NUMBER_FORMAT), voltage.ToString(NUMBER_FORMAT), elapsedSeconds(), temperature.ToString(TEMPERATURE_NUMBER_FORMAT), lr);
        }

        public void WriteLine(string line)
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                file.WriteLine(line);
            }
        }

        // returns number of elapsed seconds since file creation
        private string elapsedSeconds()
        {
            if (startTime != null)
            {
                return (DateTime.Now - startTime).TotalSeconds.ToString(NUMBER_FORMAT);
            }
            else
            {
                return "0";
            }
        }        

        // returns file path
        public string FilePath
        {
            get
            {
                return this.filePath;
            }
        }
    }
}
