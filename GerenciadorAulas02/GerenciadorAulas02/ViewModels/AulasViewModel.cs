using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.ViewModels;

public class AulasViewModel
{
    public ObservableCollection<Aula> Aulas { get; set; }

    public ICommand AdicionarAulaCommand { get; }

    public AulasViewModel()
    {
        Aulas = new ObservableCollection<Aula>();

        // Comando para adicionar aula
        AdicionarAulaCommand = new Command(AdicionarAula);
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
}
