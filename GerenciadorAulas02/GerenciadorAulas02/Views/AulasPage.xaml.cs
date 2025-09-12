using Microsoft.Maui.Controls;
using GerenciadorAulas02.ViewModels;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.Views;

public partial class AulasPage : ContentPage
{
    public AulasPage()
    {
        InitializeComponent();
        BindingContext = new AulasViewModel();
    }

    // ?? Construtor que recebe a SalaDeAula
    public AulasPage(SalaDeAula sala)
    {
        InitializeComponent();
        BindingContext = new AulasViewModel(sala);
    }
}
