using System.Collections.ObjectModel;
using System.Windows.Input;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Views;

namespace GerenciadorAulas02.ViewModels;

public class VisualizarAulasViewModel : BaseViewModel
{
    private readonly AulaDatabase database;
    private readonly SalaDeAula sala;

    public ObservableCollection<Aula> Aulas { get; } = new();
    public string Title { get; private set; }

    // Comando para abrir detalhes da aula
    public ICommand AulaSelecionadaCommand { get; }

    public VisualizarAulasViewModel(AulaDatabase database, SalaDeAula sala)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.sala = sala ?? throw new ArgumentNullException(nameof(sala));

        Title = $"Aulas - {sala.Nome}";

        // Inicializa o comando
        AulaSelecionadaCommand = new Command<Aula>(async (aula) =>
        {
            if (aula == null) return;
            await Application.Current.MainPage.Navigation.PushAsync(new VisualizarAulaDetalhePage(aula));
        });

        _ = CarregarAulasAsync();
    }

    private async Task CarregarAulasAsync()
    {
        var lista = await database.GetAulasBySalaComMateriasAsync(sala.Id);
        Aulas.Clear();
        foreach (var aula in lista)
            Aulas.Add(aula);
    }
}
