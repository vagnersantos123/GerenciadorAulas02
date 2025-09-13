using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GerenciadorAulas02.Models;

public class Aula : INotifyPropertyChanged
{
    private int id;
    private string titulo = string.Empty;
    private string descricao = string.Empty;
    private DateTime data;
    private TimeSpan duracao;
    private string tipo = "Teórica";

    private int? salaDeAulaId;
    private int? materiaId;
    private Materia? materia; // 🔹 backing field da matéria

    [PrimaryKey, AutoIncrement]
    public int Id
    {
        get => id;
        set { id = value; OnPropertyChanged(nameof(Id)); }
    }

    public string Titulo
    {
        get => titulo;
        set { titulo = value; OnPropertyChanged(nameof(Titulo)); }
    }

    public string Descricao
    {
        get => descricao;
        set { descricao = value; OnPropertyChanged(nameof(Descricao)); }
    }

    public DateTime Data
    {
        get => data;
        set { data = value; OnPropertyChanged(nameof(Data)); }
    }

    public TimeSpan Duracao
    {
        get => duracao;
        set { duracao = value; OnPropertyChanged(nameof(Duracao)); }
    }

    public string Tipo
    {
        get => tipo;
        set { tipo = value; OnPropertyChanged(nameof(Tipo)); }
    }

    // 🔹 Chave estrangeira para Sala
    public int? SalaDeAulaId
    {
        get => salaDeAulaId ?? 0;
        set { salaDeAulaId = value; OnPropertyChanged(nameof(SalaDeAulaId)); }
    }

    // 🔹 Chave estrangeira para Matéria
    public int? MateriaId
    {
        get => materiaId ?? 0;
        set { materiaId = value; OnPropertyChanged(nameof(MateriaId)); }
    }

    // 🔹 Propriedade de navegação (não mapeada no SQLite)
    [Ignore]
    public Materia? Materia
    {
        get => materia;
        set
        {
            materia = value;
            OnPropertyChanged(nameof(Materia));

            // ✅ Se a Aula ainda não tem duração definida, pega a da Matéria
            if (materia != null && duracao == default)
            {
                Duracao = TimeSpan.FromMinutes(materia.Duracao);
            }
        }
    }

    [Ignore]
    public List<string> AlunosPresentes { get; set; } = new List<string>();

    [Ignore]
    public string Resumo
    {
        get
        {
            string materiaNome = Materia != null ? Materia.Nome : "Sem matéria";
            return $"{Titulo} - {materiaNome} ({Tipo}) - {Data:dd/MM/yyyy HH:mm}, {Duracao.TotalMinutes} min, {AlunosPresentes.Count} alunos";
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string nome)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}
