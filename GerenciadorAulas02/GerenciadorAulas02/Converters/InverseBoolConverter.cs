using System;
using Microsoft.Maui.Controls; // ou Xamarin.Forms, se estiver usando

namespace GerenciadorAulas02.Converters
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // aqui você inverte o bool

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // normalmente só devolve o inverso também
            return !(bool)value;
        }
    }
}
