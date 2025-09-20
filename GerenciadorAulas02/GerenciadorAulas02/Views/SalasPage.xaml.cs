using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.ViewModels;

namespace GerenciadorAulas02.Views;

public partial class SalasPage : ContentPage
{
    public SalasPage()
    {
        InitializeComponent();
        BindingContext = new SalasViewModel();
    }

    private async void OnVerAulasClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is SalaDeAula sala)
        {
            await Navigation.PushAsync(new AulasPage(sala));
        }
    }

    // ?? Sempre que a tela aparecer, recarrega valores das preferências
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is SalasViewModel vm)
        {
            vm.DataInicioAno = Services.PreferenciasGlobais.DataInicioAno;
            vm.DataFimAno = Services.PreferenciasGlobais.DataFimAno;
        }
    }
}
