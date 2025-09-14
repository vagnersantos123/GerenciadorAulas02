using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;

namespace GerenciadorAulas02.ViewModels;

public class ConfiguracoesViewModel : BaseViewModel
{
    // ================= MATÉRIAS =================
    public ObservableCollection<Materia> Materias { get; } = new();

    public ICommand AdicionarMateriaCommand { get; }
    public ICommand ExcluirMateriaCommand { get; }

    // ================= PREFERÊNCIAS =================
    private int duracaoPadrao;
    public int DuracaoPadrao
    {
        get => duracaoPadrao;
        set
        {
            if (SetProperty(ref duracaoPadrao, value))
                PreferenciasGlobais.DuracaoPadrao = value;
        }
    }

    private int quantidadeAulasPadrao;
    public int QuantidadeAulasPadrao
    {
        get => quantidadeAulasPadrao;
        set
        {
            if (SetProperty(ref quantidadeAulasPadrao, value))
                PreferenciasGlobais.QuantidadeAulasPadrao = value;
        }
    }

    private DateTime dataInicioAno;
    public DateTime DataInicioAno
    {
        get => dataInicioAno;
        set
        {
            if (SetProperty(ref dataInicioAno, value))
                PreferenciasGlobais.DataInicioAno = value;
        }
    }

    private DateTime dataFimAno;
    public DateTime DataFimAno
    {
        get => dataFimAno;
        set
        {
            if (SetProperty(ref dataFimAno, value))
                PreferenciasGlobais.DataFimAno = value;
        }
    }

    private bool temaEscuro;
    public bool TemaEscuro
    {
        get => temaEscuro;
        set
        {
            if (SetProperty(ref temaEscuro, value))
            {
                PreferenciasGlobais.TemaEscuro = value;
                Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
            }
        }
    }

    // ================= CONSTRUTOR =================
    public ConfiguracoesViewModel()
    {
        // Inicializa com valores do PreferenciasGlobais
        duracaoPadrao = PreferenciasGlobais.DuracaoPadrao;
        quantidadeAulasPadrao = PreferenciasGlobais.QuantidadeAulasPadrao;
        dataInicioAno = PreferenciasGlobais.DataInicioAno;
        dataFimAno = PreferenciasGlobais.DataFimAno;
        temaEscuro = PreferenciasGlobais.TemaEscuro;

        // Comandos de matérias
        AdicionarMateriaCommand = new Command(async () => await AdicionarMateria());
        ExcluirMateriaCommand = new Command<Materia>(async (m) => await ExcluirMateria(m));

        // Carrega matérias
        _ = LoadMaterias();
    }

    // ================= MÉTODOS =================
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
