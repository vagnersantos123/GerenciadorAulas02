using SQLite;

namespace GerenciadorAulas02.Models;

public class Aluno
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Relação com a aula
    public int AulaId { get; set; }
}
