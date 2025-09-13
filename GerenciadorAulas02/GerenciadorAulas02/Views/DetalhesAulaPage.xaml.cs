using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.ViewModels;


namespace GerenciadorAulas02.Views;

public partial class DetalhesAulaPage : ContentPage
{
    public DetalhesAulaPage(Aula aula)
    {
        InitializeComponent();
        BindingContext = new DetalhesAulaViewModel(aula);
    }
}
