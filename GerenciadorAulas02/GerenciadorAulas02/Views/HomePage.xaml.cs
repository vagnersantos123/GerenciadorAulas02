using Microsoft.Maui.Controls;

namespace GerenciadorAulas02.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnSairClicked(object sender, EventArgs e)
    {
        // Voltar para a LoginPage
        await Navigation.PopToRootAsync();
    }

    private async void OnGerenciarAulasClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AulasPage());
    }

    private async void OnGerenciarSalasClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SalasPage());
    }

    private async void OnGerenciarAlunosClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AlunosPage());
    }

    private async void OnConfiguracoesClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ConfiguracoesPage());
    }
}
