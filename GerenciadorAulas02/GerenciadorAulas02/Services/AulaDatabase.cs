using SQLite;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.Services;

public class AulaDatabase
{
    private readonly SQLiteAsyncConnection _database;

    public AulaDatabase(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);

        // 🔹 Criação das tabelas
        _database.CreateTableAsync<Aula>().Wait();
        _database.CreateTableAsync<Aluno>().Wait();
        _database.CreateTableAsync<AulaAluno>().Wait();
        _database.CreateTableAsync<SalaDeAula>().Wait(); // agora temos salas
    }

    // ====================================================
    // CRUD AULAS
    // ====================================================
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

    // 🔹 Buscar aulas por sala
    public Task<List<Aula>> GetAulasBySalaAsync(int salaId)
    {
        return _database.Table<Aula>()
                        .Where(a => a.SalaDeAulaId == salaId)
                        .ToListAsync();
    }

    // ====================================================
    // CRUD ALUNOS (GLOBAL)
    // ====================================================
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

    // ====================================================
    // RELAÇÃO AULA x ALUNO (N:N)
    // ====================================================
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

    // ====================================================
    // CRUD SALAS
    // ====================================================
    public Task<List<SalaDeAula>> GetSalasAsync()
    {
        return _database.Table<SalaDeAula>().ToListAsync();
    }

    public Task<int> SaveSalaAsync(SalaDeAula sala)
    {
        if (sala.Id != 0)
            return _database.UpdateAsync(sala);
        else
            return _database.InsertAsync(sala);
    }

    public Task<int> DeleteSalaAsync(SalaDeAula sala)
    {
        return _database.DeleteAsync(sala);
    }
}
