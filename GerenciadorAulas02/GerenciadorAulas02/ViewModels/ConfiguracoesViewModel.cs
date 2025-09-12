using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;

namespace GerenciadorAulas02.ViewModels;

public class ConfiguracoesViewModel : BaseViewModel
{
    public ObservableCollection<Materia> Materias { get; } = new();

    public ICommand AdicionarMateriaCommand { get; }
    public ICommand ExcluirMateriaCommand { get; }

    // ================= FUTURAS CONFIGURAÇÕES =================
    private int duracaoPadrao = PreferenciasGlobais.DuracaoPadrao;
    public int DuracaoPadrao
    {
        get => duracaoPadrao;
        set
        {
            if (SetProperty(ref duracaoPadrao, value))
                PreferenciasGlobais.DuracaoPadrao = value; // 🔹 salva nas preferências
        }
    }

    private bool temaEscuro = PreferenciasGlobais.TemaEscuro;
    public bool TemaEscuro
    {
        get => temaEscuro;
        set
        {
            if (SetProperty(ref temaEscuro, value))
            {
                PreferenciasGlobais.TemaEscuro = value;

                // 🔹 aplica o tema em tempo real
                Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
            }
        }
    }


    public ConfiguracoesViewModel()
    {
        AdicionarMateriaCommand = new Command(async () => await AdicionarMateria());
        ExcluirMateriaCommand = new Command<Materia>(async (m) => await ExcluirMateria(m));

        _ = LoadMaterias();
    }

    private async Task LoadMaterias()
    {
        var materias = await App.Database.GetMateriasAsync();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Materias.Clear();
            foreach (var m in materias) Materias.Add(m);
        });
    }

    private async Task AdicionarMateria()
    {
        string nome = await Application.Current.MainPage.DisplayPromptAsync("Nova Matéria", "Nome da matéria:");
        if (string.IsNullOrWhiteSpace(nome)) return;

        var materia = new Materia { Nome = nome };
        await App.Database.SaveMateriaAsync(materia);

        await LoadMaterias();
    }

    private async Task ExcluirMateria(Materia materia)
    {
        if (materia == null) return;

        await App.Database.DeleteMateriaAsync(materia);
        await LoadMaterias();
    }
}
