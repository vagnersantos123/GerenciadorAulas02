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
}
