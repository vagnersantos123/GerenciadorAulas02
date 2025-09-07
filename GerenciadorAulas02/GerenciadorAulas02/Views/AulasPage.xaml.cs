using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.ViewModels;

namespace GerenciadorAulas02.Views;

public partial class AulasPage : ContentPage
{
    // construtor que recebe a SalaDeAula (usado pela SalasPage)
    public AulasPage(SalaDeAula sala)
    {
        InitializeComponent();
        BindingContext = new AulasViewModel(sala);
    }

    // construtor sem parâmetro (mantém compatibilidade / designer)
    public AulasPage()
    {
        InitializeComponent();
        BindingContext = new AulasViewModel();
    }
}
