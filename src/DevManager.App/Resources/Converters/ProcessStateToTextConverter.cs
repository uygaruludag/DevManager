using System.Globalization;
using System.Windows.Data;
using DevManager.Core.Models;
using Loc = DevManager.App.Localization.Strings;

namespace DevManager.App.Resources.Converters;

public class ProcessStateToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ProcessState state)
            return Loc.State_Unknown;

        return state switch
        {
            ProcessState.Running => Loc.State_Running,
            ProcessState.Stopped => Loc.State_Stopped,
            ProcessState.Starting => Loc.State_Starting,
            ProcessState.Stopping => Loc.State_Stopping,
            ProcessState.Crashed => Loc.State_Crashed,
            ProcessState.Restarting => Loc.State_Restarting,
            _ => Loc.State_Unknown
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
