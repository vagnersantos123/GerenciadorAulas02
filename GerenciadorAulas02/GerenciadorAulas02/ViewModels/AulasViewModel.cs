using GerenciadorAulas02.Config;
using GerenciadorAulas02.Helpers;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;


namespace GerenciadorAulas02.ViewModels;

public class AulasViewModel : BaseViewModelPreferencias
{
    public List<Aula> TodasAulas { get; set; } = new();
    public ObservableCollection<Aula> Aulas { get; } = new();
    public ObservableCollection<Materia> Materias { get; } = new();

    private readonly SalaDeAula? sala;
    private ConfiguracaoLetivo config;

    private string pesquisa = string.Empty;
    public string Pesquisa
    {
        get => pesquisa;
        set
        {
            // SetProperty vem do BaseViewModelPreferencias; dispara OnPropertyChanged
            if (SetProperty(ref pesquisa, value))
            {
                // chama filtro sempre que o texto muda
                FiltrarAulas(pesquisa);
            }
        }
    }


    private Materia? materiaSelecionada;
    public Materia? MateriaSelecionada
    {
        get => materiaSelecionada;
        set
        {
            if (SetProperty(ref materiaSelecionada, value) && value != null)
            {
                DuracaoMinutos = value.Duracao > 0 ? value.Duracao : PreferenciasGlobais.DuracaoPadrao;
            }
        }
    }

    private int quantidadeAulas = PreferenciasGlobais.QuantidadeAulasPadrao;
    public int QuantidadeAulas
    {
        get => quantidadeAulas;
        set => SetProperty(ref quantidadeAulas, value);
    }

    private int intervaloDias = PreferenciasGlobais.IntervaloDiasPadrao;
    public int IntervaloDias
    {
        get => intervaloDias;
        set => SetProperty(ref intervaloDias, value);
    }

    private int duracaoMinutos = PreferenciasGlobais.DuracaoPadrao;
    public int DuracaoMinutos
    {
        get => duracaoMinutos;
        set => SetProperty(ref duracaoMinutos, value);
    }

    private DateTime dataInicioAno;
    public DateTime DataInicioAno
    {
        get => dataInicioAno;
        set
        {
            if (SetProperty(ref dataInicioAno, value))
            {
                dataFimAno = value.AddMonths(10);
                OnPropertyChanged(nameof(DataFimAno));

                // Atualiza config e salva
                config.DataInicioAno = value;
                config.DataFimAno = dataFimAno;
                ConfigService.Salvar(config);
            }
        }
    }

    private bool mostrarCriacaoAulas = true;
    public bool MostrarCriacaoAulas
    {
        get => mostrarCriacaoAulas;
        set => SetProperty(ref mostrarCriacaoAulas, value);
    }


    private DateTime dataFimAno;
    public DateTime DataFimAno => dataFimAno;

    // ================= COMANDOS =================
    public ICommand AdicionarAulaCommand { get; }
    public ICommand ExcluirAulaCommand { get; }
    public ICommand EditarAulaCommand { get; }
    public ICommand GerenciarAlunosCommand { get; }
    public ICommand GerarAulasCommand { get; }
    public ICommand AlternarCriacaoAulasCommand { get; }

    // ================= CONSTRUTOR =================
    public AulasViewModel()
    {
        // Carrega config do ano letivo
        config = ConfigService.Carregar();
        dataInicioAno = config.DataInicioAno;
        dataFimAno = config.DataFimAno;

        AdicionarAulaCommand = new Command(async () => await AdicionarAula());
        ExcluirAulaCommand = new Command<Aula>(async (a) => await ExcluirAula(a));
        EditarAulaCommand = new Command<Aula>(async (a) => await EditarAula(a));
        GerenciarAlunosCommand = new Command<Aula>(async (a) => await GerenciarAlunos(a));
        GerarAulasCommand = new Command(async () => await GerarAulasParaMateria());

        _ = LoadMaterias();
        _ = LoadAulas();

        PreferenciasGlobais.PreferenciasAlteradas += OnPreferenciasAlteradas;

        AlternarCriacaoAulasCommand = new Command(() =>
        {
            MostrarCriacaoAulas = !MostrarCriacaoAulas;
        });
    }

    public AulasViewModel(SalaDeAula salaSelecionada) : this()
    {
        sala = salaSelecionada ?? throw new ArgumentNullException(nameof(salaSelecionada));
        _ = LoadAulasForSala();
    }

    private void OnPreferenciasAlteradas(object? sender, EventArgs e)
    {
        AtualizarPreferenciasGlobais();
    }

    private void AtualizarPreferenciasGlobais()
    {
        QuantidadeAulas = PreferenciasGlobais.QuantidadeAulasPadrao;
        IntervaloDias = PreferenciasGlobais.IntervaloDiasPadrao;
        DuracaoMinutos = PreferenciasGlobais.DuracaoPadrao;

        config = ConfigService.Carregar();
        dataInicioAno = config.DataInicioAno;
        dataFimAno = config.DataFimAno;

        OnPropertyChanged(nameof(DataInicioAno));
        OnPropertyChanged(nameof(DataFimAno));
    }

    // ================= MÉTODOS DE CARREGAMENTO =================
    private async Task LoadAulas()
    {
        var aulas = await App.Database.GetAulasComMateriasAsync();
        TodasAulas = aulas.ToList();        // guarda a lista completa
        FiltrarAulas(Pesquisa);            // popula Aulas já filtrada pelo texto atual
    }

    private async Task LoadAulasForSala()
    {
        if (sala == null)
        {
            await LoadAulas();
            return;
        }

        var aulas = await App.Database.GetAulasBySalaComMateriasAsync(sala.Id);
        TodasAulas = aulas.ToList();
        FiltrarAulas(Pesquisa);
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
            Inicio = DateTime.Now,
            Duracao = TimeSpan.FromMinutes(DuracaoMinutos),
            Fim = DateTime.Now.AddMinutes(DuracaoMinutos),
            Tipo = "Teórica",
            SalaDeAulaId = sala?.Id,
            MateriaId = MateriaSelecionada.Id,
            DiaAula = DateTime.Now
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

    private async Task GerarAulasParaMateria()
    {
        if (MateriaSelecionada == null)
        {
            await AlertHelper.Show("Erro", "Selecione uma matéria");
            return;
        }

        if (QuantidadeAulas <= 0)
        {
            await Application.Current.MainPage.DisplayAlert("Erro", "Informe a quantidade de aulas", "OK");
            return;
        }

        for (int i = 0; i < QuantidadeAulas; i++)
        {
            var inicio = config.DataInicioAno.AddDays(i * IntervaloDias);
            var fim = inicio.AddMinutes(DuracaoMinutos);

            var aula = new Aula
            {
                Titulo = $"{MateriaSelecionada.Nome} - Aula {i + 1}",
                Descricao = $"Aula de {MateriaSelecionada.Nome}",
                Inicio = inicio,
                Fim = fim,
                Duracao = TimeSpan.FromMinutes(DuracaoMinutos),
                Tipo = "Teórica",
                SalaDeAulaId = sala?.Id,
                MateriaId = MateriaSelecionada.Id
            };

            await App.Database.SaveAulaAsync(aula);
        }

        if (sala != null) await LoadAulasForSala(); else await LoadAulas();

        AtualizarPreferenciasGlobais();

    }

    

public void FiltrarAulas(string searchText)
{
    var filtradas = FilterHelper.Filter(
        TodasAulas, // lista original
        searchText,
        a => a.Titulo,
        a => a.Materia?.Nome,
        a => a.DiaAula.ToString("dd/MM/yyyy")
    );

    Aulas.Clear();
    foreach (var aula in filtradas)
        Aulas.Add(aula);
}

}
