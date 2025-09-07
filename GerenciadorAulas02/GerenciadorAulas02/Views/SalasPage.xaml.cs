using Microsoft.Maui.Controls;
using GerenciadorAulas02.ViewModels;

namespace GerenciadorAulas02.Views;

public partial class SalasPage : ContentPage
{
    public SalasPage()
    {
        InitializeComponent();
        BindingContext = new SalasViewModel();
    }
}
