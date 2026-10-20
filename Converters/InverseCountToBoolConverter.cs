using System.Collections;
using System.Globalization;

namespace AiCoder.Converters;

public sealed class InverseCountToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i == 0,
            ICollection collection => collection.Count == 0,
            IEnumerable enumerable => !enumerable.Cast<object>().Any(),
            _ => true
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
