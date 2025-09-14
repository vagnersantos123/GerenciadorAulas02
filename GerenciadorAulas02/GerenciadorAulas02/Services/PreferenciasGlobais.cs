using Microsoft.Maui.Storage;

namespace GerenciadorAulas02.Services;

public static class PreferenciasGlobais
{
    // =====================
    // Chaves
    // =====================
    private const string DuracaoKey = "DuracaoPadrao";
    private const string QuantidadeKey = "QuantidadeAulasPadrao";
    private const string IntervaloDiasKey = "IntervaloDiasPadrao";
    private const string DataInicioAnoKey = "DataInicioAno";
    private const string DataFimAnoKey = "DataFimAno";
    private const string TemaEscuroKey = "TemaEscuro";

    // =====================
    // Valores Padrão
    // =====================
    private const int DuracaoDefault = 60;           // minutos
    private const int QuantidadeDefault = 10;        // aulas
    private const int IntervaloDiasDefault = 1;      // intervalo entre aulas em dias
    private static readonly DateTime DataInicioDefault = new DateTime(DateTime.Now.Year, 2, 2);
    private static readonly DateTime DataFimDefault = new DateTime(DateTime.Now.Year, 12, 10);
    private const bool TemaEscuroDefault = false;

    // =====================
    // Propriedades
    // =====================
    public static int DuracaoPadrao
    {
        get => Preferences.Get(DuracaoKey, DuracaoDefault);
        set => Preferences.Set(DuracaoKey, value);
    }

    public static int QuantidadeAulasPadrao
    {
        get => Preferences.Get(QuantidadeKey, QuantidadeDefault);
        set => Preferences.Set(QuantidadeKey, value);
    }

    public static int IntervaloDiasPadrao
    {
        get => Preferences.Get(IntervaloDiasKey, IntervaloDiasDefault);
        set => Preferences.Set(IntervaloDiasKey, value);
    }

    public static DateTime DataInicioAno
    {
        get
        {
            if (Preferences.ContainsKey(DataInicioAnoKey))
            {
                long ticks = Preferences.Get(DataInicioAnoKey, DataInicioDefault.Ticks);
                return new DateTime(ticks);
            }
            return DataInicioDefault;
        }
        set
        {
            Preferences.Set(DataInicioAnoKey, value.Ticks);
        }
    }

    public static DateTime DataFimAno
    {
        get
        {
            if (Preferences.ContainsKey(DataFimAnoKey))
            {
                long ticks = Preferences.Get(DataFimAnoKey, DataFimDefault.Ticks);
                return new DateTime(ticks);
            }
            return DataFimDefault;
        }
        set
        {
            Preferences.Set(DataFimAnoKey, value.Ticks);
        }
    }



    public static bool TemaEscuro
    {
        get => Preferences.Get(TemaEscuroKey, TemaEscuroDefault);
        set => Preferences.Set(TemaEscuroKey, value);
    }
}
