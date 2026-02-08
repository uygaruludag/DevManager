using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DevManager.Core.Models;

namespace DevManager.App.Resources.Converters;

public class LogEntryTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LogEntryType type)
            return Brushes.White;

        return type switch
        {
            LogEntryType.StdOut => new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC)),   // Light gray
            LogEntryType.StdErr => new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)),    // Red
            LogEntryType.System => new SolidColorBrush(Color.FromRgb(0x64, 0xB5, 0xF6)),    // Light blue
            _ => Brushes.White
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
