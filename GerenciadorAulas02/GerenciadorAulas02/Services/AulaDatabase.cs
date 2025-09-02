using SQLite;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.Services;

public class AulaDatabase
{
    private readonly SQLiteAsyncConnection _database;

    public AulaDatabase(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
        _database.CreateTableAsync<Aula>().Wait();
    }

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
}
