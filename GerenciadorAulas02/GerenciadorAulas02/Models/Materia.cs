using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GerenciadorAulas02.Services;
using GerenciadorAulas02;

public class Materia : INotifyPropertyChanged
{
    private int duracao = PreferenciasGlobais.DuracaoPadrao;

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    private string nome = string.Empty;
    public string Nome
    {
        get => nome;
        set { nome = value; OnPropertyChanged(); }
    }

    public int Duracao
    {
        get => duracao;
        set
        {
            if (duracao != value)
            {
                duracao = value;
                OnPropertyChanged();

                // salva automaticamente no banco
                _ = App.Database.SaveMateriaAsync(this);
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string nome = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
}
