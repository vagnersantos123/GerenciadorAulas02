using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.ViewModels;

namespace GerenciadorAulas02.Views;

public partial class GerenciarAlunosDaAulaPage : ContentPage
{
    public GerenciarAlunosDaAulaPage(Aula aula)
    {
        InitializeComponent();
        BindingContext = new GerenciarAlunosDaAulaViewModel(aula);
    }
}
