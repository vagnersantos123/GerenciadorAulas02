using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;
using GerenciadorAulas02.ViewModels;

namespace GerenciadorAulas02.Views;

public partial class VisualizarSalasPage : ContentPage
{
    private readonly VisualizarSalasViewModel viewModel;

    public VisualizarSalasPage(AulaDatabase database)
    {
        InitializeComponent();
        viewModel = new VisualizarSalasViewModel(database);
        BindingContext = viewModel;
    }

    private void OnSalaSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SalaDeAula sala)
        {
            viewModel.SalaSelecionadaCommand.Execute(sala);
            ((CollectionView)sender).SelectedItem = null; // limpa a seleção
        }
    }
}
