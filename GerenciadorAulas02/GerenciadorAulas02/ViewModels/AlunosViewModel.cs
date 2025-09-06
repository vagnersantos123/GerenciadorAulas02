using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.ViewModels;

public class AlunosViewModel
{
    public ObservableCollection<Aluno> Alunos { get; } = new();

    public ICommand AdicionarAlunoCommand { get; }
    public ICommand ExcluirAlunoCommand { get; }

    public AlunosViewModel()
    {
        AdicionarAlunoCommand = new Command(AdicionarAluno);
        ExcluirAlunoCommand = new Command<Aluno>(ExcluirAluno);

        LoadAlunos();
    }

    private async void LoadAlunos()
    {
        var alunos = await App.Database.GetTodosAlunosAsync(); // ✅ usa o database global
        Alunos.Clear();
        foreach (var aluno in alunos)
            Alunos.Add(aluno);
    }

    private async void AdicionarAluno()
    {
        string nome = await Application.Current.MainPage.DisplayPromptAsync("Novo Aluno", "Nome do aluno:");
        if (string.IsNullOrWhiteSpace(nome)) return;

        string email = await Application.Current.MainPage.DisplayPromptAsync("Novo Aluno", "Email do aluno:");

        var aluno = new Aluno { Nome = nome, Email = email ?? "" };

        await App.Database.SaveAlunoAsync(aluno);
        LoadAlunos();
    }

    private async void ExcluirAluno(Aluno aluno)
    {
        if (aluno == null) return;

        await App.Database.DeleteAlunoAsync(aluno);
        LoadAlunos();
    }
}
