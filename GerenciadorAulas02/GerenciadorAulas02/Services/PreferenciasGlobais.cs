using Microsoft.Maui.Storage;

namespace GerenciadorAulas02.Services;

public static class PreferenciasGlobais
{
    private const string DuracaoKey = "DuracaoPadrao";
    private const int DuracaoDefault = 60;

    private const string TemaEscuroKey = "TemaEscuro";
    private const bool TemaEscuroDefault = false;

    /// <summary>
    /// Duração padrão das aulas em minutos.
    /// </summary>
    public static int DuracaoPadrao
    {
        get => Preferences.Get(DuracaoKey, DuracaoDefault);
        set => Preferences.Set(DuracaoKey, value);
    }

    /// <summary>
    /// Define se o app deve usar tema escuro.
    /// </summary>
    public static bool TemaEscuro
    {
        get => Preferences.Get(TemaEscuroKey, TemaEscuroDefault);
        set => Preferences.Set(TemaEscuroKey, value);
    }
}
