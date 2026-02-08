using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DevManager.Core.Models;

namespace DevManager.App.Resources.Converters;

public class ProcessStateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ProcessState state)
            return Brushes.Gray;

        return state switch
        {
            ProcessState.Running => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),   // Green
            ProcessState.Stopped => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)),   // Gray
            ProcessState.Starting => new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)),   // Yellow
            ProcessState.Stopping => new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)),   // Yellow
            ProcessState.Crashed => new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)),    // Red
            ProcessState.Restarting => new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)),  // Orange
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
