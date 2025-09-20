using System;
using GerenciadorAulas02.Services;

namespace GerenciadorAulas02.ViewModels;

public class BaseViewModelPreferencias : BaseViewModel
{
    protected BaseViewModelPreferencias()
    {
        // Inicializa valores das preferências globais
        AtualizarPreferenciasGlobais();

        // Escuta alterações globais
        PreferenciasGlobais.PreferenciasAlteradas += (_, _) => AtualizarPreferenciasGlobais();
    }

    // Propriedades vinculadas às preferências
    private int quantidadeAulas;
    public int QuantidadeAulas
    {
        get => quantidadeAulas;
        set => SetProperty(ref quantidadeAulas, value);
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

    private DateTime dataInicioAno;
    public DateTime DataInicioAno
    {
        get => dataInicioAno;
        set
        {
            if (SetProperty(ref dataInicioAno, value))
            {
                // Atualiza PreferenciasGlobais corretamente
                PreferenciasGlobais.AtualizarDatasAno(value);

                // Atualiza a propriedade local dataFimAno
                dataFimAno = PreferenciasGlobais.DataFimAno;
                OnPropertyChanged(nameof(DataFimAno));
            }
        }
    }

    private DateTime dataFimAno;
    public DateTime DataFimAno => dataFimAno;

    // Atualiza os valores do formulário com os valores atuais do PreferenciasGlobais
    protected void AtualizarPreferenciasGlobais()
    {
        QuantidadeAulas = PreferenciasGlobais.QuantidadeAulasPadrao;
        IntervaloDias = PreferenciasGlobais.IntervaloDiasPadrao;
        DuracaoMinutos = PreferenciasGlobais.DuracaoPadrao;

        dataInicioAno = PreferenciasGlobais.DataInicioAno;
        dataFimAno = PreferenciasGlobais.DataFimAno;

        OnPropertyChanged(nameof(DataInicioAno));
        OnPropertyChanged(nameof(DataFimAno));
    }
}
