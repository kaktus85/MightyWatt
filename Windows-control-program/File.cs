﻿using System;
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
            file.WriteLine("# Started on" + delimiter + "{0}" + delimiter + "{1}", startTime.ToShortDateString(), startTime.ToLongTimeString());
            file.WriteLine("# Current [A]" + delimiter + "Voltage [V]" + delimiter + "Temperature [deg C]" + delimiter + "Local[l]/Remote[r]" + delimiter + "Time since start [s]" + delimiter + "System timestamp");
        }

        // closes the file
        public void Close()
        {
            file.Close();
            filePath = null;
        }

        // writes a single line of load data to the file
        public void WriteData(double current, double voltage, double temperature, bool remote)
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                StringBuilder sb = new StringBuilder();
                string lr;
                DateTime now = DateTime.Now;
                if (remote)
                {
                    lr = "r";
                }
                else
                {
                    lr = "l";
                }
                sb.Append(current.ToString(NUMBER_FORMAT));
                sb.Append(delimiter);
                sb.Append(voltage.ToString(NUMBER_FORMAT));
                sb.Append(delimiter);
                sb.Append(temperature.ToString(TEMPERATURE_NUMBER_FORMAT));
                sb.Append(delimiter);
                sb.Append(lr);
                sb.Append(delimiter);
                sb.Append(elapsedSeconds());
                sb.Append(delimiter);
                sb.Append(" ");
                sb.Append(now.ToLongTimeString());
                sb.Append(":");
                sb.Append(now.Millisecond);
                file.WriteLine(sb.ToString());
            }
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
                return filePath;
            }
        }
    }
}
