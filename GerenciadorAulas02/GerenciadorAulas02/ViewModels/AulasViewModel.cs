using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.ViewModels;

public class AulasViewModel
{
    public ObservableCollection<Aula> Aulas { get; set; }

    public ICommand AdicionarAulaCommand { get; }
    public ICommand ExcluirAulaCommand { get; }
    public ICommand EditarAulaCommand { get; }

    public AulasViewModel()
    {
        Aulas = new ObservableCollection<Aula>();

        // Comando para adicionar aula
        AdicionarAulaCommand = new Command(AdicionarAula);
        ExcluirAulaCommand = new Command<Aula>(ExcluirAula);
        EditarAulaCommand = new Command<Aula>(EditarAula);
    }

    private void AdicionarAula()
    {
        // Para fins de exemplo, vamos adicionar uma aula fixa
        Aulas.Add(new Aula
        {
            Id = Aulas.Count + 1,
            Titulo = "Nova Aula",
            Descricao = "Descrição da aula",
            Data = DateTime.Now
        });
    }
    private void ExcluirAula(Aula aula)
    {
        if (aula != null)
            Aulas.Remove(aula);
    }
    private async void EditarAula(Aula aula)
    {
        if (aula != null)
        {
            // Exemplo simples: alterar título
            string novoTitulo = await Application.Current.MainPage.DisplayPromptAsync(
                "Editar Aula",
                "Novo título:",
                initialValue: aula.Titulo);

            if (!string.IsNullOrWhiteSpace(novoTitulo))
                aula.Titulo = novoTitulo;
        }
    }
}
