using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.ViewModels;

public class AulasViewModel
{
    public ObservableCollection<Aula> Aulas { get; private set; } = new ObservableCollection<Aula>();

    public ICommand AdicionarAulaCommand { get; }
    public ICommand ExcluirAulaCommand { get; }
    public ICommand EditarAulaCommand { get; }
    public ICommand AbrirAlunosCommand { get; }
    public ICommand GerenciarAlunosCommand { get; }

    public AulasViewModel()
    {
        AdicionarAulaCommand = new Command(AdicionarAula);
        ExcluirAulaCommand = new Command<Aula>(ExcluirAula);
        EditarAulaCommand = new Command<Aula>(EditarAula);
        AbrirAlunosCommand = new Command<Aula>(AbrirAlunos);
        GerenciarAlunosCommand = new Command<Aula>(GerenciarAlunos);


        // ⚡ garante que as aulas existentes sejam carregadas
        LoadAulas();
    }

    private async void LoadAulas()
    {


        var aulas = await App.Database.GetAulasAsync();

        Aulas.Clear(); // limpa a coleção atual sem recriar
        foreach (var aula in aulas)
        {
            Aulas.Add(aula); // adiciona as aulas salvas do banco
        }
    }

    private async void AdicionarAula()
    {
        string titulo = await Application.Current.MainPage.DisplayPromptAsync("Nova Aula", "Título da aula:");
        if (string.IsNullOrWhiteSpace(titulo))
            return;

        string tipo = await Application.Current.MainPage.DisplayPromptAsync("Nova Aula", "Tipo (Teórica/Prática):", initialValue: "Teórica");
        string duracaoStr = await Application.Current.MainPage.DisplayPromptAsync("Nova Aula", "Duração em minutos:", initialValue: "60");

        if (!double.TryParse(duracaoStr, out double minutos))
            minutos = 60;

        Aula novaAula = new Aula
        {
            Titulo = titulo,
            Descricao = "Descrição da aula",
            Data = DateTime.Now,
            Tipo = tipo,
            Duracao = TimeSpan.FromMinutes(minutos)
        };

        await App.Database.SaveAulaAsync(novaAula); // salva no banco
        LoadAulas(); // recarrega a lista inteira (inclui a nova)
    }

    private async void ExcluirAula(Aula aula)
    {
        if (aula != null)
        {
            await App.Database.DeleteAulaAsync(aula); // remove do banco
            LoadAulas(); // recarrega a lista do banco
        }
    }

    private async void EditarAula(Aula aula)
    {
        if (aula != null)
        {
            string novoTitulo = await Application.Current.MainPage.DisplayPromptAsync(
                "Editar Aula",
                "Novo título:",
                initialValue: aula.Titulo);

            if (!string.IsNullOrWhiteSpace(novoTitulo))
            {
                aula.Titulo = novoTitulo;
                await App.Database.SaveAulaAsync(aula); // atualiza no banco
                LoadAulas(); // recarrega a lista
            }
        }
    }
    private async void AbrirAlunos(Aula aula)
    {
        if (aula != null)
        {
            await Application.Current.MainPage.Navigation.PushAsync(new Views.AlunosPage());
        }
    }
    private async void GerenciarAlunos(Aula aula)
    {
        if (aula != null)
        {
            await Application.Current.MainPage.Navigation.PushAsync(
                new Views.GerenciarAlunosDaAulaPage(aula)
            );
        }

    }
}