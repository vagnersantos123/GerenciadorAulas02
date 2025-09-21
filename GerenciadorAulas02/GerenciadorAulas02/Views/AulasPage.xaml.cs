using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.ViewModels;
using GerenciadorAulas02.Services;


namespace GerenciadorAulas02.Views;

public partial class AulasPage : ContentPage
{
    // Construtor padrão (lista todas as aulas)
    public AulasPage()
    {
        InitializeComponent();
        BindingContext = new AulasViewModel();
    }

    // Construtor recebendo uma sala (lista aulas só da sala)
    public AulasPage(SalaDeAula sala)
    {
        InitializeComponent();
        BindingContext = new AulasViewModel(sala);
    }
    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is AulasViewModel vm)
        {
            vm.FiltrarAulas(e.NewTextValue);
        }
    }

}
