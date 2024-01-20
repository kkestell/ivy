using System.Globalization;
using Avalonia.Data.Converters;
using Ivy.Common;

namespace Ivy.Converters;

public class EnumValueToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum enumValue && parameter is Enum parameterValue)
        {
            return enumValue.Equals(parameterValue);
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EnumValueToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum enumValue && parameter is Enum parameterValue)
        {
            return enumValue.Equals(parameterValue) ? Icons.RadioButtonChecked : Icons.RadioButtonUnchecked;
        }
        return Icons.RadioButtonUnchecked;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
