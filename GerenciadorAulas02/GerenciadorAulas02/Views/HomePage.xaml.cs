using Microsoft.Maui.Controls;

namespace GerenciadorAulas02.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private void OnSairClicked(object sender, EventArgs e)
    {
        // Limpa a preferência de manter conectado
        Preferences.Set("ManterConectado", false);
        Preferences.Remove("UsuarioLogado");

        // Reinicia a MainPage com a LoginPage
        Application.Current.MainPage = new NavigationPage(new Views.LoginPage());
    }

    private async void OnGerenciarSalasClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SalasPage());
    }

    //private async void OnGerenciarAulasClicked(object sender, EventArgs e)
    //{
    //    await Navigation.PushAsync(new AulasPage());
    //}

    private async void OnConfiguracoesClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ConfiguracoesPage());
    }
}
