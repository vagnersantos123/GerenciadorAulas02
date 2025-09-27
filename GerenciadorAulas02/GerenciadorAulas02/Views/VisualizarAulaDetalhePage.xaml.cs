using GerenciadorAulas02.Models;
using GerenciadorAulas02.ViewModels;

namespace GerenciadorAulas02.Views;

public partial class VisualizarAulaDetalhePage : ContentPage
{
    public VisualizarAulaDetalhePage(Aula aula)
    {
        InitializeComponent();
        BindingContext = new VisualizarAulaDetalheViewModel(aula);
    }
}
