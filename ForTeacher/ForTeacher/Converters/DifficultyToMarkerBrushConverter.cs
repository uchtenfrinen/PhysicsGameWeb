using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ForTeacher.Converters;

public sealed class DifficultyToMarkerBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var d = value is int i ? i : value is short sh ? sh : 0;
        return d switch
        {
            0 => new SolidColorBrush(Color.FromRgb(0x2A, 0x33, 0x48)),
            1 or 2 => new SolidColorBrush(Color.FromRgb(0x4A, 0x8C, 0xC4)),
            3 => new SolidColorBrush(Color.FromRgb(0xD4, 0xB0, 0x3A)),
            _ => new SolidColorBrush(Color.FromRgb(0xE0, 0x55, 0x2E))
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
