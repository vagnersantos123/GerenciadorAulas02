using System.Collections.ObjectModel;
using System.Windows.Input;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;
using Microsoft.Maui.Controls;

namespace GerenciadorAulas02.ViewModels;

public class VisualizarSalasViewModel : BaseViewModel
{
    private readonly AulaDatabase database;

    public ObservableCollection<SalaDeAulaView> Salas { get; } = new();
    public ICommand SalaSelecionadaCommand { get; }

    public VisualizarSalasViewModel(AulaDatabase database)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        SalaSelecionadaCommand = new Command<SalaDeAulaView>(async (salaView) =>
        {
            if (salaView == null) return;
            // busca a SalaDeAula original antes de navegar (caso precise campos completos)
            var salaOriginal = await database.GetSalaByIdAsync(salaView.Id);
            if (salaOriginal == null) return;
            await Application.Current.MainPage.Navigation.PushAsync(new Views.VisualizarAulasPage(database, salaOriginal));
        });

        _ = CarregarSalasAsync();
    }

    private async Task CarregarSalasAsync()
    {
        var lista = await database.GetSalasAsync();
        Salas.Clear();

        foreach (var sala in lista)
        {
            var count = await database.GetAulasCountBySalaAsync(sala.Id);
            Salas.Add(new SalaDeAulaView
            {
                Id = sala.Id,
                Nome = sala.Nome,
                Descricao = sala.Descricao,
                QuantidadeAulas = count
            });
        }
    }
}
