using System;
using System.Globalization;
using System.Windows.Data;
using InstallerUI.Properties;

namespace InstallerUI.Convertors
{
    internal class BooleanToButtonContentConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isInstalled && isInstalled ? Resources.CancelBtn : Resources.ExitBtn;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is string s) || !s.Equals(Resources.ExitBtn);
        }
    }
}