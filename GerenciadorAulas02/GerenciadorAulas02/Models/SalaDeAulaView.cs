namespace GerenciadorAulas02.Models;

public class SalaDeAulaView
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int QuantidadeAulas { get; set; }
}
