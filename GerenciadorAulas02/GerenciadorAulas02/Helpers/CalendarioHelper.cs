using GerenciadorAulas02.Services;  

public static class CalendarioHelper
{
    public static DateTime CalcularDataFimAno(DateTime dataInicio)
    {
        // Usa intervalo salvo em PreferenciasGlobais
        return dataInicio.AddDays(PreferenciasGlobais.IntervaloDiasPadrao);
        // ou, se você guarda em anos: return dataInicio.AddYears(1);
    }
}
