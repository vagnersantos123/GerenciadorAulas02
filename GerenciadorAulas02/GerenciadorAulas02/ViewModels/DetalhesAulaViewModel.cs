using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.ViewModels;

public class DetalhesAulaViewModel : BaseViewModel
{
    public Aula Aula { get; }
    public ObservableCollection<Aluno> Alunos { get; } = new();

    public ICommand SelecionarAlunoCommand { get; }

    public DetalhesAulaViewModel(Aula aula)
    {
        Aula = aula;
        SelecionarAlunoCommand = new Command<Aluno>(async (a) => await AbrirAluno(a));

        _ = LoadAlunos();
    }

    private async Task LoadAlunos()
    {
        var alunos = await App.Database.GetAlunosByAulaAsync(Aula.Id);
        Alunos.Clear();
        foreach (var a in alunos) Alunos.Add(a);
    }

    private async Task AbrirAluno(Aluno aluno)
    {
        if (aluno == null) return;

        // ⚡ por enquanto apenas abre uma tela de placeholder
        await Application.Current.MainPage.DisplayAlert("Aluno Selecionado", $"Você clicou em {aluno.Nome}", "OK");

        // depois trocamos isso por uma página DetalhesAlunoNaAulaPage
    }
}
