using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Text;

namespace MightyWatt
{
    /// <summary>
    /// Interaction logic for Statistics.xaml
    /// </summary>
    public partial class Statistics : Window
    {
        //private Load load;
        private MeasurementValues values, lastValues;
        private double dvmInputResistance;
        private ValueWithStatistics voltage, current, power, resistance;
        private double charge, energy, seconds;
        private bool count; // true if we should add/count values
        private DateTime lastMeasurement;
        private File log;

        private string sdValueFormat = "e3";
        private string floatingValueFormat = "f4";
        private string accumulatedValueFormat = "f3";
        private string secondFormat = "f3";

        private List<BindingExpression> bindingExpressions;
        private DateTime lastUpdateTime;
        private DateTime lastLogTime;

        public Statistics(MeasurementValues values, double dvmInputResistance, File log)
        {
            InitializeComponent();

            this.dvmInputResistance = dvmInputResistance;
            this.values = values;
            this.log = log;
            lastValues = new MeasurementValues(0, 0);

            // bindings
            DataContext = this;
            bindingExpressions = new List<BindingExpression>();
            bindingExpressions.Add(textBoxCharge.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxEnergy.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxTime.GetBindingExpression(TextBox.TextProperty));

            bindingExpressions.Add(textBoxVoltage.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxCurrent.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxPower.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxResistance.GetBindingExpression(TextBox.TextProperty));

            bindingExpressions.Add(textBoxVoltageSD.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxCurrentSD.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxPowerSD.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxResistanceSD.GetBindingExpression(TextBox.TextProperty));

            bindingExpressions.Add(textBoxVoltageMinimum.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxCurrentMinimum.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxPowerMinimum.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxResistanceMinimum.GetBindingExpression(TextBox.TextProperty));

            bindingExpressions.Add(textBoxVoltageMaximum.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxCurrentMaximum.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxPowerMaximum.GetBindingExpression(TextBox.TextProperty));
            bindingExpressions.Add(textBoxResistanceMaximum.GetBindingExpression(TextBox.TextProperty));

            bindingExpressions.Add(buttonLog.GetBindingExpression(IsEnabledProperty));
            bindingExpressions.Add(labelLogged.GetBindingExpression(VisibilityProperty));

            Reset();
        }

        private void UpdateGUI()
        {
            if ((DateTime.Now - lastUpdateTime).TotalMilliseconds > 200) // limit GUI update rate
            {
                lastUpdateTime = DateTime.Now;
                Dispatcher.Invoke(() =>
                {
                    if (bindingExpressions != null)
                    {
                        foreach (BindingExpression be in bindingExpressions)
                        {
                            be.UpdateTarget();
                        }
                    }
                }
                );
            }
        }

        public void SetFile(File log)
        {
            this.log = log;
            UpdateGUI();
        }

        public void Update()
        {
            if (Count)
            {
                DateTime now = DateTime.Now;
                // add charge and energy - trapezoid integration                  
                charge += (now - lastMeasurement).TotalSeconds * (values.current + lastValues.current) / 2;
                energy += (now - lastMeasurement).TotalSeconds * (values.current * values.voltage + lastValues.current * lastValues.voltage) / 2;
                seconds += (now - lastMeasurement).TotalSeconds;
                lastMeasurement = now;
                lastValues.current = values.current;
                lastValues.voltage = values.voltage;

                // add to statistics
                current.Add(values.current);
                voltage.Add(values.voltage);
                power.Add(values.voltage * values.current);
                if (values.current == 0)
                {
                    resistance.Add(dvmInputResistance);
                }
                else
                {
                    resistance.Add(values.voltage / values.current);
                }
            }
            // update GUI
            UpdateGUI();
        }

        private void Reset()
        {
            voltage.Reset();
            current.Reset();
            resistance.Reset();
            power.Reset();
            charge = 0;
            energy = 0;
            seconds = 0;
            lastValues.current = 0;
            lastValues.voltage = 0;
            UpdateGUI();
        }

        private void Log()
        {
            if (log == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(log.FilePath))
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            string prefix = string.Empty;
            if (OffsetLog)
            {
                for (int i = 0; i < File.columnCount; i++)
                {
                    sb.Append(File.delimiter);
                }
            }
            prefix = sb.ToString();

            // user note
            if (!string.IsNullOrEmpty(textBoxUserNote.Text))
            {
                sb.Clear();
                sb.Append(prefix);
                sb.Append(textBoxUserNote.Text);
                log.WriteLine(sb.ToString());
            }

            // integrated values
            sb.Clear();
            sb.Append(prefix);
            sb.Append("Charge");
            sb.Append(File.delimiter);
            sb.Append(Charge);
            sb.Append(File.delimiter);
            sb.Append(((ComboBoxItem)comboBoxChargeUnit.SelectedItem).Content.ToString());
            log.WriteLine(sb.ToString());

            sb.Clear();
            sb.Append(prefix);
            sb.Append("Dissipated energy");
            sb.Append(File.delimiter);
            sb.Append(Energy);
            sb.Append(File.delimiter);
            sb.Append(((ComboBoxItem)comboBoxEnergyUnit.SelectedItem).Content.ToString());
            log.WriteLine(sb.ToString());

            sb.Clear();
            sb.Append(prefix);
            sb.Append("Elapsed time");
            sb.Append(File.delimiter);
            sb.Append(Time);
            sb.Append(File.delimiter);
            sb.Append(((ComboBoxItem)comboBoxTimeUnit.SelectedItem).Content.ToString());
            log.WriteLine(sb.ToString());

            // header for statistics
            sb.Clear();
            sb.Append(prefix);
            sb.Append(File.delimiter);
            sb.Append("Average");
            sb.Append(File.delimiter);
            sb.Append("Standard deviation");
            sb.Append(File.delimiter);
            sb.Append("Minimum");
            sb.Append(File.delimiter);
            sb.Append("Maximum");
            sb.Append(File.delimiter);
            sb.Append("Unit");
            log.WriteLine(sb.ToString());

            // statistics values
            sb.Clear();
            sb.Append(prefix);
            sb.Append("Current");
            sb.Append(File.delimiter);
            sb.Append(CurrentAverage);
            sb.Append(File.delimiter);
            sb.Append(CurrentSD);
            sb.Append(File.delimiter);
            sb.Append(CurrentMin);
            sb.Append(File.delimiter);
            sb.Append(CurrentMax);
            sb.Append(File.delimiter);
            sb.Append("A");
            log.WriteLine(sb.ToString());

            sb.Clear();
            sb.Append(prefix);
            sb.Append("Voltage");
            sb.Append(File.delimiter);
            sb.Append(VoltageAverage);
            sb.Append(File.delimiter);
            sb.Append(VoltageSD);
            sb.Append(File.delimiter);
            sb.Append(VoltageMin);
            sb.Append(File.delimiter);
            sb.Append(VoltageMax);
            sb.Append(File.delimiter);
            sb.Append("V");
            log.WriteLine(sb.ToString());

            sb.Clear();
            sb.Append(prefix);
            sb.Append("Power");
            sb.Append(File.delimiter);
            sb.Append(PowerAverage);
            sb.Append(File.delimiter);
            sb.Append(PowerSD);
            sb.Append(File.delimiter);
            sb.Append(PowerMin);
            sb.Append(File.delimiter);
            sb.Append(PowerMax);
            sb.Append(File.delimiter);
            sb.Append("W");
            log.WriteLine(sb.ToString());

            sb.Clear();
            sb.Append(prefix);
            sb.Append("Resistance");
            sb.Append(File.delimiter);
            sb.Append(ResistanceAverage);
            sb.Append(File.delimiter);
            sb.Append(ResistanceSD);
            sb.Append(File.delimiter);
            sb.Append(ResistanceMin);
            sb.Append(File.delimiter);
            sb.Append(ResistanceMax);
            sb.Append(File.delimiter);
            sb.Append("Ω");
            log.WriteLine(sb.ToString());

            lastLogTime = DateTime.Now;
        }

        private void buttonStartStop_Click(object sender, RoutedEventArgs e)
        {
            Count = !Count; // toggle start/stop
            buttonStartStop.GetBindingExpression(Button.ContentProperty).UpdateTarget();
            UpdateGUI();
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void buttonLog_Click(object sender, RoutedEventArgs e)
        {
            Log();
            UpdateGUI();
        }

        private void comboBoxChargeUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGUI();
        }

        private void comboBoxEnergyUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGUI();
        }

        private void comboBoxTimeUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGUI();
        }

        public bool Count
        {
            get
            {
                return count;
            }
            set
            {
                if (value)
                {
                    lastMeasurement = DateTime.Now; // start time when count is activated
                    voltage.Start();
                    current.Start();
                    resistance.Start();
                    power.Start();
                }
                else
                {
                    voltage.Stop();
                    current.Stop();
                    resistance.Stop();
                    power.Stop();
                }
                count = value;
                UpdateGUI();
            }
        }

        public string StartStop
        {
            get
            {
                if (Count)
                {
                    return "Stop";
                }
                else
                {
                    return "Start";
                }
            }
        }

        public string Charge
        {
            get
            {
                if (comboBoxChargeUnit.SelectedIndex == 0)
                {
                    return charge.ToString(accumulatedValueFormat);
                }
                else if (comboBoxChargeUnit.SelectedIndex == 1)
                {
                    return (charge / 3600).ToString(accumulatedValueFormat);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string Energy
        {
            get
            {
                if (comboBoxEnergyUnit.SelectedIndex == 0)
                {
                    return energy.ToString(accumulatedValueFormat);
                }
                else if (comboBoxEnergyUnit.SelectedIndex == 1)
                {
                    return (energy / 3600).ToString(accumulatedValueFormat);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string Time
        {
            get
            {
                if (comboBoxTimeUnit.SelectedIndex == 0)
                {
                    return seconds.ToString(secondFormat);
                }
                else if (comboBoxTimeUnit.SelectedIndex == 1)
                {
                    TimeSpan ts = TimeSpan.FromSeconds(seconds);
                    return string.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public bool OffsetLog { get; set; }

        public string CurrentAverage { get { return current.Average.ToString(floatingValueFormat); } }
        public string VoltageAverage { get { return voltage.Average.ToString(floatingValueFormat); } }
        public string PowerAverage { get { return power.Average.ToString(floatingValueFormat); } }
        public string ResistanceAverage { get { return resistance.Average.ToString(floatingValueFormat); } }

        public string CurrentSD { get { return current.SD.ToString(sdValueFormat); } }
        public string VoltageSD { get { return voltage.SD.ToString(sdValueFormat); } }
        public string PowerSD { get { return power.SD.ToString(sdValueFormat); } }
        public string ResistanceSD { get { return resistance.SD.ToString(sdValueFormat); } }

        public string CurrentMin { get { return current.Min?.ToString(floatingValueFormat); } }
        public string VoltageMin { get { return voltage.Min?.ToString(floatingValueFormat); } }

        public string PowerMin { get { return power.Min?.ToString(floatingValueFormat); } }

        public string ResistanceMin { get { return resistance.Min?.ToString(floatingValueFormat); } }

        public string CurrentMax { get { return current.Max?.ToString(floatingValueFormat); } }
        public string VoltageMax { get { return voltage.Max?.ToString(floatingValueFormat); } }
        public string PowerMax { get { return power.Max?.ToString(floatingValueFormat); } }
        public string ResistanceMax { get { return resistance.Max?.ToString(floatingValueFormat); } }

        public bool LogFileAvailable
        {
            get
            {
                if (log == null)
                {
                    return false;
                }
                else
                {
                    return !string.IsNullOrEmpty(log.FilePath);
                }
            }
        }

        public Visibility LogSavedNotification
        {
            get
            {
                if ((DateTime.Now - lastLogTime).TotalSeconds < 1)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Hidden;
                }
            }
        }
    }

    public class MeasurementValues
    {
        public double voltage, current;

        public MeasurementValues(double voltage, double current)
        {
            this.voltage = voltage;
            this.current = current;
        }
    }

    public struct ValueWithStatistics
    {
        private double sum, sum2, stdev, average;
        private double? min, max;
        private double seconds, lastx;
        private DateTime lastAddition;
        private bool started, firstRun;

        public void Add(double x)
        {
            if (!started)
            {
                return;
            }

            if (firstRun)
            {
                lastAddition = DateTime.Now;
                lastx = x;
                firstRun = false;
                return;
            }

            DateTime now = DateTime.Now;
            double dt = (now - lastAddition).TotalSeconds;
            double xav = (x + lastx) / 2;
            double xdt = dt * xav;

            seconds += dt;
            sum += xdt;
            sum2 += xdt * xav;

            average = sum / seconds;
            stdev = Math.Sqrt(sum2 / seconds - Math.Pow(average, 2));
            if (double.IsNaN(stdev))
            {
                stdev = 0;
            }

            lastAddition = now;
            lastx = x;

            if (min == null)
            {
                min = x;
            }
            else if (x < min)
            {
                min = x;
            }

            if (max == null)
            {
                max = x;
            }
            else if (x > max)
            {
                max = x;
            }
        }

        public void Start()
        {
            started = true;
            firstRun = true;
            lastAddition = DateTime.Now;
            lastx = 0;
        }

        public void Stop()
        {
            started = false;
        }

        public void Reset()
        {
            sum = 0;
            sum2 = 0;
            stdev = 0;
            average = 0;
            min = null;
            max = null;
            seconds = 0;
            lastx = 0;
            lastAddition = DateTime.Now;
            firstRun = true;
        }

        public double Average { get { return average; } }

        public double SD { get { return stdev; } }

        public double Time { get { return seconds; } }

        public double? Min { get { return min; } }

        public double? Max { get { return max; } }
    }
}
