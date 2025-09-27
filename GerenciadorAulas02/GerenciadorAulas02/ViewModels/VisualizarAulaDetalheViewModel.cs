using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.ViewModels;

public class VisualizarAulaDetalheViewModel : BaseViewModel
{
    public string Titulo { get; }
    public DateTime DiaAula { get; }
    public TimeSpan Duracao { get; }
    public string Descricao { get; }

    public VisualizarAulaDetalheViewModel(Aula aula)
    {
        if (aula == null) throw new ArgumentNullException(nameof(aula));

        Titulo = aula.Titulo;
        DiaAula = aula.DiaAula;
        Duracao = aula.Duracao;
        Descricao = aula.Descricao;
    }
}
