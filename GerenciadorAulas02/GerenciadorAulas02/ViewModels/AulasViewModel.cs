using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.ViewModels;

public class AulasViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Aula> Aulas { get; } = new();
    public ObservableCollection<Materia> Materias { get; } = new();

    // Commands
    public ICommand AdicionarAulaCommand { get; }
    public ICommand ExcluirAulaCommand { get; }
    public ICommand EditarAulaCommand { get; }
    public ICommand GerenciarAlunosCommand { get; }

    private SalaDeAula? sala;

    // Materia selecionada para o Picker
    private Materia? materiaSelecionada;
    public Materia? MateriaSelecionada
    {
        get => materiaSelecionada;
        set
        {
            if (materiaSelecionada != value)
            {
                materiaSelecionada = value;
                OnPropertyChanged();
            }
        }
    }

    public AulasViewModel()
    {
        AdicionarAulaCommand = new Command(async () => await AdicionarAula());
        ExcluirAulaCommand = new Command<Aula>(async (a) => await ExcluirAula(a));
        EditarAulaCommand = new Command<Aula>(async (a) => await EditarAula(a));
        GerenciarAlunosCommand = new Command<Aula>(async (a) => await GerenciarAlunos(a));

        _ = LoadAulas();
        _ = LoadMaterias();
    }

    public AulasViewModel(SalaDeAula salaSelecionada) : this()
    {
        sala = salaSelecionada ?? throw new ArgumentNullException(nameof(salaSelecionada));
        _ = LoadAulasForSala();
    }

    #region Carregamento
    private async Task LoadAulas()
    {
        try
        {
            var aulas = await App.Database.GetAulasComMateriasAsync();
            Aulas.Clear();
            foreach (var a in aulas) Aulas.Add(a);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Erro", $"Erro ao carregar aulas: {ex.Message}", "OK");
        }
    }

    private async Task LoadAulasForSala()
    {
        if (sala == null)
        {
            await LoadAulas();
            return;
        }

        try
        {
            var aulas = await App.Database.GetAulasBySalaComMateriasAsync(sala.Id);
            Aulas.Clear();
            foreach (var a in aulas) Aulas.Add(a);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Erro", $"Erro ao carregar aulas da sala: {ex.Message}", "OK");
        }
    }

    private async Task LoadMaterias()
    {
        try
        {
            var materias = await App.Database.GetMateriasAsync();
            Materias.Clear();
            foreach (var m in materias) Materias.Add(m);

            if (Materias.Count > 0 && MateriaSelecionada == null)
                MateriaSelecionada = Materias.First();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Erro", $"Erro ao carregar matérias: {ex.Message}", "OK");
        }
    }
    #endregion

    #region CRUD
    private async Task AdicionarAula()
    {
        if (MateriaSelecionada == null)
        {
            await Application.Current.MainPage.DisplayAlert("Aviso", "Selecione uma matéria antes de adicionar a aula.", "OK");
            return;
        }

        string tipo = await Application.Current.MainPage.DisplayPromptAsync("Nova Aula", "Tipo (Teórica/Prática):", initialValue: "Teórica");

        // duração padrão vem de PreferenciasGlobais
        int duracaoPadrao = Services.PreferenciasGlobais.DuracaoPadrao;

        string duracaoStr = await Application.Current.MainPage.DisplayPromptAsync(
            "Nova Aula",
            "Duração em minutos:",
            initialValue: duracaoPadrao.ToString()
        );

        if (!double.TryParse(duracaoStr, out double minutos))
            minutos = duracaoPadrao;

        var aula = new Aula
        {
            Titulo = MateriaSelecionada.Nome,
            MateriaId = MateriaSelecionada.Id,
            Materia = MateriaSelecionada,
            Descricao = "Descrição da aula",
            Data = DateTime.Now,
            Tipo = tipo ?? "Teórica",
            Duracao = TimeSpan.FromMinutes(minutos),
            SalaDeAulaId = sala?.Id
        };

        await App.Database.SaveAulaAsync(aula);

        if (sala != null) await LoadAulasForSala(); else await LoadAulas();
    }

    private async Task ExcluirAula(Aula aula)
    {
        if (aula == null) return;

        await App.Database.DeleteAulaAsync(aula);

        if (sala != null) await LoadAulasForSala(); else await LoadAulas();
    }

    private async Task EditarAula(Aula aula)
    {
        if (aula == null) return;

        string novoTitulo = await Application.Current.MainPage.DisplayPromptAsync("Editar Aula", "Novo título:", initialValue: aula.Titulo);
        if (!string.IsNullOrWhiteSpace(novoTitulo))
        {
            aula.Titulo = novoTitulo;
            await App.Database.SaveAulaAsync(aula);

            if (sala != null) await LoadAulasForSala(); else await LoadAulas();
        }
    }

    private async Task GerenciarAlunos(Aula aula)
    {
        if (aula == null) return;

        await Application.Current.MainPage.Navigation.PushAsync(new Views.GerenciarAlunosDaAulaPage(aula));
    }
    #endregion

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}
