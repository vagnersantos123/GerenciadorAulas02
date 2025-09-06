using SQLite;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.Services;

public class AulaDatabase
{
    // 🔹 Aqui está a variável que faltava
    private readonly SQLiteAsyncConnection _database;

    public AulaDatabase(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);

        // Cria as tabelas no banco, se ainda não existirem
        _database.CreateTableAsync<Aula>().Wait();
        _database.CreateTableAsync<Aluno>().Wait();
        _database.CreateTableAsync<AulaAluno>().Wait();
    }

    // ====================
    // CRUD AULA
    // ====================
    public Task<List<Aula>> GetAulasAsync()
    {
        return _database.Table<Aula>().ToListAsync();
    }

    public Task<int> SaveAulaAsync(Aula aula)
    {
        if (aula.Id != 0)
            return _database.UpdateAsync(aula);
        else
            return _database.InsertAsync(aula);
    }

    public Task<int> DeleteAulaAsync(Aula aula)
    {
        return _database.DeleteAsync(aula);
    }

    // ====================
    // CRUD ALUNO (GLOBAL)
    // ====================
    public Task<List<Aluno>> GetTodosAlunosAsync()
    {
        return _database.Table<Aluno>().ToListAsync();
    }

    public Task<int> SaveAlunoAsync(Aluno aluno)
    {
        if (aluno.Id != 0)
            return _database.UpdateAsync(aluno);
        else
            return _database.InsertAsync(aluno);
    }

    public Task<int> DeleteAlunoAsync(Aluno aluno)
    {
        return _database.DeleteAsync(aluno);
    }

    // ====================
    // RELAÇÃO AULA x ALUNO (N:N)
    // ====================
    public Task<int> AddAlunoToAulaAsync(int aulaId, int alunoId)
    {
        var relacao = new AulaAluno { AulaId = aulaId, AlunoId = alunoId };
        return _database.InsertAsync(relacao);
    }

    public Task<int> RemoveAlunoFromAulaAsync(int aulaId, int alunoId)
    {
        return _database.Table<AulaAluno>()
            .Where(x => x.AulaId == aulaId && x.AlunoId == alunoId)
            .DeleteAsync();
    }

    public async Task<List<Aluno>> GetAlunosByAulaAsync(int aulaId)
    {
        var relacoes = await _database.Table<AulaAluno>()
                                      .Where(x => x.AulaId == aulaId)
                                      .ToListAsync();

        var alunos = await _database.Table<Aluno>().ToListAsync();

        return (from rel in relacoes
                join aluno in alunos on rel.AlunoId equals aluno.Id
                select aluno).ToList();
    }

    // ====================
    // Métodos compatíveis (para não quebrar código antigo)
    // ====================
    public Task<List<Aluno>> GetAlunosAsync()
    {
        return GetTodosAlunosAsync();
    }

    public Task<List<Aluno>> GetAlunosAsync(int aulaId)
    {
        return GetAlunosByAulaAsync(aulaId);
    }
}
