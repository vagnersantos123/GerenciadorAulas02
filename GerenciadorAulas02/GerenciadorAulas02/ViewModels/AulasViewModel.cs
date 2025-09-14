using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;

namespace GerenciadorAulas02.ViewModels;

public class AulasViewModel : BaseViewModel
{
    // ================= LISTAS =================
    public ObservableCollection<Aula> Aulas { get; } = new();
    public ObservableCollection<Materia> Materias { get; } = new();

    private readonly SalaDeAula? sala;

    // ================= PROPRIEDADES DO FORMULÁRIO =================
    private Materia? materiaSelecionada;
    public Materia? MateriaSelecionada
    {
        get => materiaSelecionada;
        set
        {
            if (SetProperty(ref materiaSelecionada, value) && value != null)
            {
                // Atualiza duração conforme a matéria ou padrão global
                DuracaoMinutos = value.Duracao > 0 ? value.Duracao : PreferenciasGlobais.DuracaoPadrao;
            }
        }
    }

    private int quantidadeAulas;
    public int QuantidadeAulas
    {
        get => quantidadeAulas;
        set => SetProperty(ref quantidadeAulas, value);
    }

    private DateTime dataInicial;
    public DateTime DataInicial
    {
        get => dataInicial;
        set => SetProperty(ref dataInicial, value);
    }

    private int intervaloDias;
    public int IntervaloDias
    {
        get => intervaloDias;
        set => SetProperty(ref intervaloDias, value);
    }

    private int duracaoMinutos;
    public int DuracaoMinutos
    {
        get => duracaoMinutos;
        set => SetProperty(ref duracaoMinutos, value);
    }

    // ================= COMANDOS =================
    public ICommand AdicionarAulaCommand { get; }
    public ICommand ExcluirAulaCommand { get; }
    public ICommand EditarAulaCommand { get; }
    public ICommand GerenciarAlunosCommand { get; }
    public ICommand GerarAulasCommand { get; }

    // ================= CONSTRUTOR =================
    public AulasViewModel()
    {
        // Inicializa com valores do PreferenciasGlobais
        QuantidadeAulas = PreferenciasGlobais.QuantidadeAulasPadrao;
        IntervaloDias = PreferenciasGlobais.IntervaloDiasPadrao;
        DuracaoMinutos = PreferenciasGlobais.DuracaoPadrao;
        DataInicial = PreferenciasGlobais.DataInicioAno;

        // Inicializa comandos
        AdicionarAulaCommand = new Command(async () => await AdicionarAula());
        ExcluirAulaCommand = new Command<Aula>(async (a) => await ExcluirAula(a));
        EditarAulaCommand = new Command<Aula>(async (a) => await EditarAula(a));
        GerenciarAlunosCommand = new Command<Aula>(async (a) => await GerenciarAlunos(a));
        GerarAulasCommand = new Command(async () => await GerarAulasParaMateria());

        // Carrega dados
        _ = LoadAulas();
        _ = LoadMaterias();
    }

    public AulasViewModel(SalaDeAula salaSelecionada) : this()
    {
        sala = salaSelecionada ?? throw new ArgumentNullException(nameof(salaSelecionada));
        _ = LoadAulasForSala();
    }

    // ================= MÉTODOS DE CARREGAMENTO =================
    private async Task LoadAulas()
    {
        var aulas = await App.Database.GetAulasComMateriasAsync();
        Aulas.Clear();
        foreach (var a in aulas) Aulas.Add(a);
    }

    private async Task LoadAulasForSala()
    {
        if (sala == null)
        {
            await LoadAulas();
            return;
        }

        var aulas = await App.Database.GetAulasBySalaComMateriasAsync(sala.Id);
        Aulas.Clear();
        foreach (var a in aulas) Aulas.Add(a);
    }

    private async Task LoadMaterias()
    {
        var materias = await App.Database.GetMateriasAsync();
        Materias.Clear();
        foreach (var m in materias) Materias.Add(m);
    }

    // ================= MÉTODOS CRUD =================
    private async Task AdicionarAula()
    {
        if (MateriaSelecionada == null)
        {
            await Application.Current.MainPage.DisplayAlert("Erro", "Selecione uma matéria", "OK");
            return;
        }

        var aula = new Aula
        {
            Titulo = $"{MateriaSelecionada.Nome} - Aula Avulsa",
            Descricao = $"Aula de {MateriaSelecionada.Nome}",
            Data = DateTime.Now,
            Tipo = "Teórica",
            Duracao = TimeSpan.FromMinutes(DuracaoMinutos),
            SalaDeAulaId = sala?.Id,
            MateriaId = MateriaSelecionada.Id
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

    // ================= GERAR AULAS EM LOTE =================
    private async Task GerarAulasParaMateria()
    {
        if (MateriaSelecionada == null)
        {
            await Application.Current.MainPage.DisplayAlert("Erro", "Selecione uma matéria", "OK");
            return;
        }

        if (QuantidadeAulas <= 0)
        {
            await Application.Current.MainPage.DisplayAlert("Erro", "Informe a quantidade de aulas", "OK");
            return;
        }

        for (int i = 0; i < QuantidadeAulas; i++)
        {
            var aula = new Aula
            {
                Titulo = $"{MateriaSelecionada.Nome} - Aula {i + 1}",
                Descricao = $"Aula de {MateriaSelecionada.Nome}",
                Data = DataInicial.AddDays(i * IntervaloDias),
                Tipo = "Teórica",
                Duracao = TimeSpan.FromMinutes(DuracaoMinutos),
                SalaDeAulaId = sala?.Id,
                MateriaId = MateriaSelecionada.Id
            };

            await App.Database.SaveAulaAsync(aula);
        }

        // Atualiza lista
        if (sala != null) await LoadAulasForSala(); else await LoadAulas();

        // Reseta para os valores padrão novamente
        QuantidadeAulas = PreferenciasGlobais.QuantidadeAulasPadrao;
        IntervaloDias = PreferenciasGlobais.IntervaloDiasPadrao;
        DuracaoMinutos = PreferenciasGlobais.DuracaoPadrao;
        DataInicial = PreferenciasGlobais.DataInicioAno;
    }
}
