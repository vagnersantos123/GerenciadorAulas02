using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.ViewModels;

public class SalasViewModel : BaseViewModel
{
    public ObservableCollection<SalaDeAula> Salas { get; } = new();

    private string nomeSala;
    public string NomeSala
    {
        get => nomeSala;
        set => SetProperty(ref nomeSala, value);
    }

    public ICommand AdicionarSalaCommand { get; }
    public ICommand ExcluirSalaCommand { get; }

    public SalasViewModel()
    {
        AdicionarSalaCommand = new Command(async () => await AdicionarSala());
        ExcluirSalaCommand = new Command<SalaDeAula>(async (s) => await ExcluirSala(s));

        _ = LoadSalas();
    }

    private async Task LoadSalas()
    {
        var salas = await App.Database.GetSalasAsync();
        Salas.Clear();
        foreach (var s in salas) Salas.Add(s);
    }

    private async Task AdicionarSala()
    {
        if (string.IsNullOrWhiteSpace(NomeSala))
        {
            await Application.Current.MainPage.DisplayAlert("Erro", "Digite um nome para a sala", "OK");
            return;
        }

        var novaSala = new SalaDeAula
        {
            Nome = NomeSala
        };

        await App.Database.SaveSalaAsync(novaSala);

        NomeSala = string.Empty;
        OnPropertyChanged(nameof(NomeSala));

        await LoadSalas();
    }

    private async Task ExcluirSala(SalaDeAula sala)
    {
        if (sala == null) return;

        bool confirmar = await Application.Current.MainPage.DisplayAlert(
            "Confirmar",
            $"Deseja excluir a sala {sala.Nome}?",
            "Sim", "Não");

        if (!confirmar) return;

        await App.Database.DeleteSalaAsync(sala);
        await LoadSalas();
    }
}
