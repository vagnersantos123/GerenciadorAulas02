using SQLite;

namespace GerenciadorAulas02.Models;

public class AulaAluno
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int AulaId { get; set; }
    public int AlunoId { get; set; }
}
