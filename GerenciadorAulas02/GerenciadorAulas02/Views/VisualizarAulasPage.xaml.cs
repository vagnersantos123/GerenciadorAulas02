using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;
using GerenciadorAulas02.ViewModels;
using Microsoft.Maui.Controls;

namespace GerenciadorAulas02.Views;

public partial class VisualizarAulasPage : ContentPage
{
    // Declara o viewModel como campo da classe
    private readonly VisualizarAulasViewModel viewModel;

    public VisualizarAulasPage(AulaDatabase database, SalaDeAula sala)
    {
        InitializeComponent();

        // Inicializa o viewModel e define o BindingContext
        viewModel = new VisualizarAulasViewModel(database, sala);
        BindingContext = viewModel;
    }

    private void OnAulaSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Aula aula)
        {
            if (BindingContext is VisualizarAulasViewModel viewModel)
            {
                viewModel.AulaSelecionadaCommand.Execute(aula);
            }
            ((CollectionView)sender).SelectedItem = null; // limpa a seleção
        }
    }



}
