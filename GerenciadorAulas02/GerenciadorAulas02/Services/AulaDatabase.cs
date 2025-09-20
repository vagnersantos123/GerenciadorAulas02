using SQLite;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.Services;

public class AulaDatabase
{
    private readonly SQLiteAsyncConnection _database;

    public AulaDatabase(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);

        // 🔹 Criação das tabelas no banco
        _database.CreateTableAsync<Aula>().Wait();
        _database.CreateTableAsync<Aluno>().Wait();
        _database.CreateTableAsync<AulaAluno>().Wait();
        _database.CreateTableAsync<SalaDeAula>().Wait();
        _database.CreateTableAsync<Materia>().Wait();
    }

    // ====================
    // CRUD AULA
    // ====================
    #region CRUD AULA
    public Task<List<Aula>> GetAulasAsync()
    {
        return _database.Table<Aula>().ToListAsync();
    }

    public Task<List<Aula>> GetAulasBySalaAsync(int salaId)
    {
        return _database.Table<Aula>()
                        .Where(a => a.SalaDeAulaId == salaId)
                        .ToListAsync();
    }

    public async Task<List<Aula>> GetAulasComMateriasAsync()
    {
        var aulas = await _database.Table<Aula>().ToListAsync();
        var materias = await _database.Table<Materia>().ToListAsync();

        foreach (var aula in aulas)
        {
            // Vincula a matéria
            aula.Materia = materias.FirstOrDefault(m => m.Id == aula.MateriaId);

            // Se a aula não tem duração, define pela matéria ou preferências
            if (aula.Duracao == default && aula.Materia != null)
            {
                aula.Duracao = TimeSpan.FromMinutes(aula.Materia.Duracao > 0
                    ? aula.Materia.Duracao
                    : PreferenciasGlobais.DuracaoPadrao);
            }

            // Se Fim não estiver definido, calcula a partir do Inicio + Duracao
            if (aula.Fim == default && aula.Inicio != default)
            {
                aula.Fim = aula.Inicio.Add(aula.Duracao);
            }
        }

        return aulas;
    }


    public async Task<List<Aula>> GetAulasBySalaComMateriasAsync(int salaId)
    {
        var aulas = await _database.Table<Aula>()
                                   .Where(a => a.SalaDeAulaId == salaId)
                                   .ToListAsync();
        var materias = await _database.Table<Materia>().ToListAsync();

        foreach (var aula in aulas)
        {
            aula.Materia = materias.FirstOrDefault(m => m.Id == aula.MateriaId);

            if (aula.Duracao == default && aula.Materia != null)
            {
                aula.Duracao = TimeSpan.FromMinutes(aula.Materia.Duracao > 0
                    ? aula.Materia.Duracao
                    : PreferenciasGlobais.DuracaoPadrao);
            }

            if (aula.Fim == default && aula.Inicio != default)
            {
                aula.Fim = aula.Inicio.Add(aula.Duracao);
            }
        }

        return aulas;
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
    #endregion

    // ====================
    // CRUD ALUNO (GLOBAL)
    // ====================
    #region CRUD ALUNO (GLOBAL)
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
    #endregion

    // ====================
    // RELAÇÃO AULA x ALUNO (N:N)
    // ====================
    #region RELAÇÃO AULA x ALUNO (N:N)
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
    #endregion

    // ====================
    // CRUD SALA
    // ====================
    #region CRUD SALA
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
    #endregion

    // ====================
    // CRUD MATERIA
    // ====================
    #region CRUD MATERIA
    public async Task<List<Materia>> GetMateriasAsync()
    {
        var materias = await _database.Table<Materia>().ToListAsync();

        foreach (var m in materias)
        {
            if (m.Duracao <= 0)
                m.Duracao = PreferenciasGlobais.DuracaoPadrao;
        }

        return materias;
    }


    public Task<int> SaveMateriaAsync(Materia materia)
    {
        // ✅ Se não foi definido, usa o valor padrão das Preferências
        if (materia.Duracao <= 0)
        {
            materia.Duracao = PreferenciasGlobais.DuracaoPadrao;
        }

        if (materia.Id != 0)
            return _database.UpdateAsync(materia);
        else
            return _database.InsertAsync(materia);
    }


    public Task<int> DeleteMateriaAsync(Materia materia)
    {
        return _database.DeleteAsync(materia);
    }
    #endregion
}
