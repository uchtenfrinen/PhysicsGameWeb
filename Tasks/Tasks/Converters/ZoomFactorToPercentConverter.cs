using System.Globalization;
using System.Windows.Data;

namespace PhysicsTasks.Terminal.Converters;

/// <summary>Коэффициент масштаба (1.0 = 100%) в строку вида «142%».</summary>
public sealed class ZoomFactorToPercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d && !double.IsNaN(d) && !double.IsInfinity(d))
            return $"{Math.Round(d * 100.0):0}%";
        return "100%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
