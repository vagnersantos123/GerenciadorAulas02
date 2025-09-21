using System.Globalization;

namespace GerenciadorAulas02.Converters;
public class BoolParaAnguloConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return 90; // expandido
        return 0;     // recolhido
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value == 90;
    }
}
