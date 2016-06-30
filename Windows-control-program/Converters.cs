using System;
using System.Windows.Data;

namespace MightyWatt
{
    class Converters
    {
        // temperature in status bar
        [ValueConversion(typeof(double), typeof(string))]
        public class TemperatureConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                // temperature to full text
                if ((double)value == 0)
                {
                    return "Temperature: N/A";
                }
                return "Temperature: " + ((double)value).ToString("N0") + " °C";
            }
            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                // not needed
                string s;
                s = ((string)value).Remove(0, 13); // remove "Temperature: "
                if (s == "N/A")
                {
                    return 0;
                }
                s = s.TrimEnd('°', ' ', 'C');
                return Double.Parse(s);
            }
        }       

        // double to string conversion
        [ValueConversion(typeof(double), typeof(string))]
        public class ValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) // parameter is number format
            {
                // double to string
                return ((double)value).ToString((string)parameter, culture);                
            }
            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                // string to double
                return Double.Parse((string)value);
            }
        }

        // int to unit (enum name) conversion
        [ValueConversion(typeof(byte), typeof(string))]
        public class UnitEnumConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                // byte to string
                try
                {
                    return Load.UnitSymbols[System.Convert.ToByte(value)];

                }
                catch (OverflowException)
                {
                    return null;
                }
            }
            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                // string to byte
                for (byte i = 0; i < Load.UnitSymbols.Length; i++)
                {
                    if ((string)value == Load.UnitSymbols[i])
                    {
                        return i;
                    }
                }
                return null;
            }
        }

        // double seconds to string representation in parameter-units
        [ValueConversion(typeof(double), typeof(string))]
        public class TimeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                double returnedValue = (double)value;
                switch ((TimeUnits)parameter)
                {
                    case TimeUnits.ms:
                    {
                        returnedValue *= 1000;
                        break;
                    }
                    case TimeUnits.min:
                    {
                        returnedValue /= 60;
                        break;
                    }
                    case TimeUnits.h:
                    {
                        returnedValue /= 3600;
                        break;
                    }
                }
                return returnedValue.ToString("g4", culture);
            }
            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                double returnedValue = Double.Parse((string)value, culture);
                switch ((TimeUnits)parameter)
                {
                    case TimeUnits.ms:
                    {
                        returnedValue /= 1000;
                        break;
                    }
                    case TimeUnits.min:
                    {
                        returnedValue *= 60;
                        break;
                    }
                    case TimeUnits.h:
                    {
                        returnedValue *= 3600;
                        break;
                    }
                }
                return returnedValue;
            }
        }
    }
}
