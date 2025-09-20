using Microsoft.Maui.Storage;
using System;

namespace GerenciadorAulas02.Services;

public static class PreferenciasGlobais
{
    // ===================== CHAVES =====================
    private const string DuracaoKey = "DuracaoPadrao";
    private const string QuantidadeKey = "QuantidadeAulasPadrao";
    private const string IntervaloDiasKey = "IntervaloDiasPadrao";
    private const string DataInicioAnoKey = "DataInicioAno";
    private const string DataFimAnoKey = "DataFimAno";
    private const string TemaEscuroKey = "TemaEscuro";

    // ===================== VALORES PADRÃO =====================
    private const int DuracaoDefault = 60; // minutos
    private const int QuantidadeDefault = 10; // aulas
    private const int IntervaloDiasDefault = 1;
    private static readonly DateTime DataInicioDefault = new(DateTime.Now.Year, 2, 2);
    private static readonly DateTime DataFimDefault = DataInicioDefault.AddMonths(10);
    private const bool TemaEscuroDefault = false;

    // ===================== EVENTO =====================
    public static event EventHandler? PreferenciasAlteradas;
    private static void NotificarAlteracao() => PreferenciasAlteradas?.Invoke(null, EventArgs.Empty);

    // ===================== PROPRIEDADES =====================
    public static int DuracaoPadrao
    {
        get => Preferences.Get(DuracaoKey, DuracaoDefault);
        set { Preferences.Set(DuracaoKey, value); NotificarAlteracao(); }
    }

    public static int QuantidadeAulasPadrao
    {
        get => Preferences.Get(QuantidadeKey, QuantidadeDefault);
        set { Preferences.Set(QuantidadeKey, value); NotificarAlteracao(); }
    }

    public static int IntervaloDiasPadrao
    {
        get => Preferences.Get(IntervaloDiasKey, IntervaloDiasDefault);
        set { Preferences.Set(IntervaloDiasKey, value); NotificarAlteracao(); }
    }

    public static DateTime DataInicioAno
    {
        get
        {
            long ticks = Preferences.Get(DataInicioAnoKey, DataInicioDefault.Ticks);
            return new DateTime(ticks);
        }
        set
        {
            Preferences.Set(DataInicioAnoKey, value.Ticks);
            // Atualiza automaticamente DataFimAno
            DataFimAno = value.AddMonths(10);
            NotificarAlteracao();
        }
    }

    public static DateTime DataFimAno
    {
        get
        {
            long ticks = Preferences.Get(DataFimAnoKey, DataFimDefault.Ticks);
            return new DateTime(ticks);
        }
        private set { Preferences.Set(DataFimAnoKey, value.Ticks); }
    }

    public static bool TemaEscuro
    {
        get => Preferences.Get(TemaEscuroKey, TemaEscuroDefault);
        set
        {
            Preferences.Set(TemaEscuroKey, value);
            NotificarAlteracao();
        }
    }

    // ===================== MÉTODOS AUXILIARES =====================
    public static void ResetDatasAntigas()
    {
        try { _ = new DateTime(Preferences.Get(DataInicioAnoKey, DataInicioDefault.Ticks)); }
        catch { Preferences.Remove(DataInicioAnoKey); }

        try { _ = new DateTime(Preferences.Get(DataFimAnoKey, DataFimDefault.Ticks)); }
        catch { Preferences.Remove(DataFimAnoKey); }
    }

    // ===================== NOVO MÉTODO =====================
    public static void AtualizarDatasAno(DateTime inicio)
    {
        var fim = inicio.AddMonths(10);

        Preferences.Set(DataInicioAnoKey, inicio.Ticks);
        Preferences.Set(DataFimAnoKey, fim.Ticks);

        NotificarAlteracao();
    }
}
