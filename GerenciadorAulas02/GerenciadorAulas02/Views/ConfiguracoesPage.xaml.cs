using Microsoft.Maui.Controls;
using GerenciadorAulas02.ViewModels;

namespace GerenciadorAulas02.Views;

public partial class ConfiguracoesPage : ContentPage
{
    public ConfiguracoesPage()
    {
        InitializeComponent();
        BindingContext = new ConfiguracoesViewModel();
    }
}
