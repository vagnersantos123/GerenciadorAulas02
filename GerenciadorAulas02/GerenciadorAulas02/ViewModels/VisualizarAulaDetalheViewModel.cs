using GerenciadorAulas02.Models;
using System.Windows.Input;

namespace GerenciadorAulas02.ViewModels;

public class VisualizarAulaDetalheViewModel : BaseViewModel
{
    private readonly Aula aula; // referência da aula

    public string Titulo => aula.Titulo;
    public DateTime DiaAula => aula.DiaAula;
    public TimeSpan Duracao => aula.Duracao;
    public string Descricao => aula.Descricao;

    public ICommand FinalizarAulaCommand { get; } // comando para finalizar a aula



    public VisualizarAulaDetalheViewModel(Aula aula)
    {
       
        if (aula == null) throw new ArgumentNullException(nameof(aula));

        this.aula = aula; // guarda a referência

        FinalizarAulaCommand = new Command(FinalizarAula);
    }

    public bool Finalizada
    {
        get => aula.Finalizada;
        set
        {
            if (aula.Finalizada != value)
            {
                aula.Finalizada = value;
                OnPropertyChanged(nameof(Finalizada));
            }
        }
    }
    private void FinalizarAula()
    {
        Finalizada = true;
    }


    public string DuracaoFormatada
    {
        get
        {
            if (aula.Duracao.TotalHours >= 1)
                return $"{(int)aula.Duracao.TotalHours}h {aula.Duracao.Minutes}min";
            else
                return $"{aula.Duracao.Minutes}min";
        }
    }

}
