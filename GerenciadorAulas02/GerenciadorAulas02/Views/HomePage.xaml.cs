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

    private async void OnConfiguracoesClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ConfiguracoesPage());
    }

    // a visuallizar ainda nao foi criada por isso o erro aqui.
    private async void OnVisualizarSalasClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new VisualizarSalasPage(App.Database));
    }

    private async void OnGerenciarAlunosClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AlunosPage());
    }

}
