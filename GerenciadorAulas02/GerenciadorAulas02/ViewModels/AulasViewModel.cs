using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.ViewModels;

public class AulasViewModel
{
    public ObservableCollection<Aula> Aulas { get; } = new();

    public ICommand AdicionarAulaCommand { get; }
    public ICommand ExcluirAulaCommand { get; }
    public ICommand EditarAulaCommand { get; }
    public ICommand GerenciarAlunosCommand { get; }

    private readonly SalaDeAula? sala;

    // ----------------------------
    // Construtor sem parâmetros
    // ----------------------------
    public AulasViewModel()
    {
        AdicionarAulaCommand = new Command(async () => await AdicionarAula());
        ExcluirAulaCommand = new Command<Aula>(async (a) => await ExcluirAula(a));
        EditarAulaCommand = new Command<Aula>(async (a) => await EditarAula(a));
        GerenciarAlunosCommand = new Command<Aula>(async (a) => await GerenciarAlunos(a));

        _ = LoadAulas(); // fire-and-forget (carrega todas as aulas)
    }

    // ----------------------------
    // Construtor que recebe a SalaDeAula
    // ----------------------------
    public AulasViewModel(SalaDeAula salaSelecionada) : this()
    {
        sala = salaSelecionada ?? throw new ArgumentNullException(nameof(salaSelecionada));
        _ = LoadAulasForSala(); // carrega aulas da sala
    }

    #region Carregamento
    private async Task LoadAulas()
    {
        try
        {
            var aulas = await App.Database.GetAulasAsync();
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
            var aulas = await App.Database.GetAulasBySalaAsync(sala.Id);
            Aulas.Clear();
            foreach (var a in aulas) Aulas.Add(a);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Erro", $"Erro ao carregar aulas da sala: {ex.Message}", "OK");
        }
    }
    #endregion

    #region CRUD
    private async Task AdicionarAula()
    {
        string titulo = await Application.Current.MainPage.DisplayPromptAsync("Nova Aula", "Título da aula:");
        if (string.IsNullOrWhiteSpace(titulo)) return;

        string tipo = await Application.Current.MainPage.DisplayPromptAsync("Nova Aula", "Tipo (Teórica/Prática):", initialValue: "Teórica");
        string duracaoStr = await Application.Current.MainPage.DisplayPromptAsync("Nova Aula", "Duração em minutos:", initialValue: "60");
        if (!double.TryParse(duracaoStr, out double minutos)) minutos = 60;

        var aula = new Aula
        {
            Titulo = titulo,
            Descricao = "Descrição da aula",
            Data = DateTime.Now,
            Tipo = tipo ?? "Teórica",
            Duracao = TimeSpan.FromMinutes(minutos),
            SalaDeAulaId = sala?.Id // 🔹 agora é opcional
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
}
