using System;
using Microsoft.Maui.Controls; // ou Xamarin.Forms, se estiver usando

namespace GerenciadorAulas02.Converters
{
    public class AulaFinalizadaTextConverter : IValueConverter
    {
        // Converte bool (Finalizada) para o texto do botão
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool finalizada)
            {
                return finalizada ? "Aula Finalizada" : "Finalizar Aula";
            }
            return "Finalizar Aula"; // valor padrão caso não seja bool
        }

        // ConvertBack não será usado, então lança exceção
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
