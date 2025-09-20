using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace GerenciadorAulas02.Helpers
{
    public static class AlertHelper
    {
        public static Task Show(string titulo, string mensagem, string botaoOk = "OK")
        {
            return Application.Current.MainPage.DisplayAlert(titulo, mensagem, botaoOk);
        }
    }
}
