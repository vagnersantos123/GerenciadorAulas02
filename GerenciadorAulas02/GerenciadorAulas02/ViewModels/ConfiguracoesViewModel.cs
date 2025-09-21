using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading.Tasks;


namespace GerenciadorAulas02.ViewModels;

public class ConfiguracoesViewModel : INotifyPropertyChanged
{
    // ================= DATAS =================
    #region Datas
    public DateTime DataInicioAno
    {
        get => PreferenciasGlobais.DataInicioAno;
        set
        {
            if (PreferenciasGlobais.DataInicioAno != value)
            {
                PreferenciasGlobais.DataInicioAno = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DataFimAno));
            }
        }
    }

    public DateTime DataFimAno => PreferenciasGlobais.DataFimAno;
    #endregion


    // ================= MATERIAS =================
    #region Materias
    public ObservableCollection<Materia> Materias { get; } = new();
    public ICommand AdicionarMateriaCommand { get; }
    public ICommand ExcluirMateriaCommand { get; }

    public ConfiguracoesViewModel()
    {
        AdicionarMateriaCommand = new Command(async () => await AdicionarMateria());
        ExcluirMateriaCommand = new Command<Materia>(async (materia) => await ExcluirMateria(materia));

        _ = CarregarMaterias();
    }
    private async Task CarregarMaterias()
    {
        var lista = await App.Database.GetMateriasAsync();
        Materias.Clear();
        foreach (var m in lista) Materias.Add(m);
    }

    private async Task AdicionarMateria()
    {
        // Abre popup para digitar o nome
        string nome = await Application.Current.MainPage.DisplayPromptAsync(
            "Nova Matéria",
            "Digite o nome da matéria:");

        // Se cancelou ou deixou em branco, não faz nada
        if (string.IsNullOrWhiteSpace(nome))
            return;

        // Cria nova matéria e salva no banco
        var nova = new Materia { Nome = nome };
        await App.Database.SaveMateriaAsync(nova);

        // Adiciona na coleção para atualizar a UI
        Materias.Add(nova);
    }


    private async Task ExcluirMateria(Materia materia)
    {
        if (materia == null) return;
        await App.Database.DeleteMateriaAsync(materia);
        Materias.Remove(materia);
    }


    #endregion


    // ================= PREFERÊNCIAS =================
    #region Preferências
    public int DuracaoPadrao
    {
        get => PreferenciasGlobais.DuracaoPadrao;
        set => PreferenciasGlobais.DuracaoPadrao = value;
    }

    public int QuantidadeAulasPadrao
    {
        get => PreferenciasGlobais.QuantidadeAulasPadrao;
        set => PreferenciasGlobais.QuantidadeAulasPadrao = value;
    }

    public int IntervaloDiasPadrao
    {
        get => PreferenciasGlobais.IntervaloDiasPadrao;
        set => PreferenciasGlobais.IntervaloDiasPadrao = value;
    }

    public bool TemaEscuro
    {
        get => PreferenciasGlobais.TemaEscuro;
        set
        {
            PreferenciasGlobais.TemaEscuro = value;
            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        }
    }
    #endregion

    // ================= EVENTO =================
    #region Eventos
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string nome = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));

    #endregion
}
