using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace GerenciadorAulas02.Helpers
{
    public static class AppHelper
    {
        // Exibe um alerta simples
        public static async Task ShowAlert(string title, string message, string cancel = "OK")
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        // Navega para uma página qualquer
        public static async Task NavigateTo(Page page)
        {
            if (Application.Current?.MainPage != null && page != null)
                await Application.Current.MainPage.Navigation.PushAsync(page);
        }
    }
}
