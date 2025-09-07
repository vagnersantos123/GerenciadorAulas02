using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.ViewModels;

public class SalasViewModel
{
    public ObservableCollection<SalaDeAula> Salas { get; } = new();

    public ICommand AdicionarSalaCommand { get; }
    public ICommand ExcluirSalaCommand { get; }
    public ICommand AbrirAulasCommand { get; }

    public SalasViewModel()
    {
        AdicionarSalaCommand = new Command(async () => await AdicionarSala());
        ExcluirSalaCommand = new Command<SalaDeAula>(async (sala) => await ExcluirSala(sala));
        AbrirAulasCommand = new Command<SalaDeAula>(async (sala) => await AbrirAulas(sala));

        _ = LoadSalas();
    }

    private async Task LoadSalas()
    {
        var salas = await App.Database.GetSalasAsync();
        Salas.Clear();
        foreach (var s in salas)
            Salas.Add(s);
    }

    private async Task AdicionarSala()
    {
        string nome = await Application.Current.MainPage.DisplayPromptAsync("Nova Sala", "Nome da sala:");
        if (string.IsNullOrWhiteSpace(nome)) return;

        string descricao = await Application.Current.MainPage.DisplayPromptAsync("Descrição", "Descrição da sala:");

        var sala = new SalaDeAula
        {
            Nome = nome,
            Descricao = descricao ?? ""
        };

        await App.Database.SaveSalaAsync(sala);
        await LoadSalas();
    }

    private async Task ExcluirSala(SalaDeAula sala)
    {
        if (sala == null) return;

        bool confirmar = await Application.Current.MainPage.DisplayAlert(
            "Confirmar exclusão",
            $"Deseja excluir a sala \"{sala.Nome}\"?",
            "Sim", "Não");

        if (!confirmar) return;

        await App.Database.DeleteSalaAsync(sala);
        await LoadSalas();
    }

    private async Task AbrirAulas(SalaDeAula sala)
    {
        if (sala == null) return;

        await Application.Current.MainPage.Navigation.PushAsync(new Views.AulasPage(sala));
    }
}
