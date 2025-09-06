using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.ViewModels;

public class AlunoSelecionavel
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsSelecionado { get; set; } // usado no CheckBox
}

public class GerenciarAlunosDaAulaViewModel
{
    public ObservableCollection<AlunoSelecionavel> TodosAlunos { get; set; } = new();

    public ICommand SalvarCommand { get; }

    private Aula aula;

    public GerenciarAlunosDaAulaViewModel(Aula aulaSelecionada)
    {
        aula = aulaSelecionada;
        SalvarCommand = new Command(Salvar);
        LoadAlunos();
    }

    private async void LoadAlunos()
    {
        var todos = await App.Database.GetTodosAlunosAsync();
        var vinculados = await App.Database.GetAlunosByAulaAsync(aula.Id);

        TodosAlunos.Clear();
        foreach (var aluno in todos)
        {
            TodosAlunos.Add(new AlunoSelecionavel
            {
                Id = aluno.Id,
                Nome = aluno.Nome,
                Email = aluno.Email,
                IsSelecionado = vinculados.Any(v => v.Id == aluno.Id)
            });
        }
    }

    private async void Salvar()
    {
        // Remove vínculos antigos
        foreach (var aluno in await App.Database.GetAlunosByAulaAsync(aula.Id))
            await App.Database.RemoveAlunoFromAulaAsync(aula.Id, aluno.Id);

        // Adiciona vínculos novos
        foreach (var aluno in TodosAlunos.Where(a => a.IsSelecionado))
            await App.Database.AddAlunoToAulaAsync(aula.Id, aluno.Id);

        await Application.Current.MainPage.DisplayAlert("Sucesso", "Alunos atualizados para a aula!", "OK");
        await Application.Current.MainPage.Navigation.PopAsync();
    }
}
