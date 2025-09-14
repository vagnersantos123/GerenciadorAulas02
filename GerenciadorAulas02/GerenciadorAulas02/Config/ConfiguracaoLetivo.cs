

namespace GerenciadorAulas02.Config;
public class ConfiguracaoLetivo
{
    public DateTime DataInicioAno { get; set; }
    public DateTime DataFimAno { get; set; }
    public List<DateTime> Feriados { get; set; } = new();
}
