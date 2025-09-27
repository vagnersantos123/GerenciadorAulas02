using System.Collections.ObjectModel;
using System.Windows.Input;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Views;

namespace GerenciadorAulas02.ViewModels;

public class VisualizarSalasViewModel : BaseViewModel
{
    private readonly AulaDatabase database;

    public ObservableCollection<SalaDeAula> Salas { get; } = new();
    public ICommand SalaSelecionadaCommand { get; }

    public VisualizarSalasViewModel(AulaDatabase database)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));

        SalaSelecionadaCommand = new Command<SalaDeAula>(async (sala) =>
        {
            if (sala == null) return;
            await Application.Current.MainPage.Navigation.PushAsync(
                new VisualizarAulasPage(database, sala));
        });

        _ = CarregarSalasAsync();
    }


    private async Task CarregarSalasAsync()
    {
        var lista = await database.GetSalasAsync();
        Salas.Clear();
        foreach (var sala in lista)
            Salas.Add(sala);
    }
}
