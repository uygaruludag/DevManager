using System.Globalization;
using System.Windows.Data;
using DevManager.Core.Models;

namespace DevManager.App.Resources.Converters;

public class ProcessStateToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ProcessState state)
            return "Unknown";

        return state switch
        {
            ProcessState.Running => "Running",
            ProcessState.Stopped => "Stopped",
            ProcessState.Starting => "Starting...",
            ProcessState.Stopping => "Stopping...",
            ProcessState.Crashed => "Crashed",
            ProcessState.Restarting => "Restarting...",
            _ => "Unknown"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
