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
    private TimeSpan duracao;
    private string tipo = "Teórica";
    private bool finalizada = false;

    private int? salaDeAulaId;
    private int? materiaId;
    private Materia? materia;

    private TimeSpan? horarioInicio;
    private TimeSpan? horarioFim;
    private DateTime diaAula;

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

    public bool Finalizada
    {
        get => finalizada;
        set { finalizada = value; OnPropertyChanged(nameof(Finalizada)); }
    }

    public int? SalaDeAulaId
    {
        get => salaDeAulaId ?? 0;
        set { salaDeAulaId = value; OnPropertyChanged(nameof(SalaDeAulaId)); }
    }

    public int? MateriaId
    {
        get => materiaId ?? 0;
        set { materiaId = value; OnPropertyChanged(nameof(MateriaId)); }
    }

    public DateTime DiaAula
    {
        get => diaAula;
        set { diaAula = value; OnPropertyChanged(nameof(DiaAula)); }
    }

    public string DiaAulaFormatado => DiaAula.ToString("dd/MM/yyyy");


    [Ignore]
    public Materia? Materia
    {
        get => materia;
        set
        {
            materia = value;
            OnPropertyChanged(nameof(Materia));

            if (materia != null && duracao == default)
            {
                Duracao = TimeSpan.FromMinutes(materia.Duracao);
                HorarioFim = HorarioInicio + Duracao; // soma dois TimeSpan
            }
        }
    }


    [Ignore]
    public List<string> AlunosPresentes { get; set; } = new List<string>();
    [Ignore]
    public string HorarioInicioFormatado
    => HorarioInicio.HasValue ? HorarioInicio.Value.ToString(@"hh\:mm") : "--:--";

    [Ignore]
    public string HorarioFimFormatado
        => HorarioFim.HasValue ? HorarioFim.Value.ToString(@"hh\:mm") : "--:--";

    [Ignore]
    public string DuracaoFormatada
    {
        get
        {
            if (Duracao.TotalHours >= 1)
                return $"{(int)Duracao.TotalHours}h {Duracao.Minutes}min";
            else
                return $"{Duracao.Minutes}min";
        }
    }


    [Ignore]
    public string DataFormatada
        => DiaAula.ToString("dd/MM/yyyy");

    



    public TimeSpan? HorarioInicio
    {
        get => horarioInicio;
        set
        {
            horarioInicio = value;
            OnPropertyChanged(nameof(HorarioInicio));
            AtualizarDuracao();
        }
    }

    public TimeSpan? HorarioFim
    {
        get => horarioFim;
        set
        {
            horarioFim = value;
            OnPropertyChanged(nameof(HorarioFim));
            AtualizarDuracao();
        }
    }

    private void AtualizarDuracao()
    {
        if (HorarioInicio.HasValue && HorarioFim.HasValue)
        {
            Duracao = HorarioFim.Value - HorarioInicio.Value;
            OnPropertyChanged(nameof(Duracao));
        }
    }


    [Ignore]
    public string Resumo
    {
        get
        {
            string materiaNome = Materia != null ? Materia.Nome : "Sem matéria";
            string inicio = HorarioInicio.HasValue ? HorarioInicio.Value.ToString(@"hh\:mm") : "--:--";
            string fim = HorarioFim.HasValue ? HorarioFim.Value.ToString(@"hh\:mm") : "--:--";
            return $"{Titulo} - {materiaNome} ({Tipo}) - {DiaAula:dd/MM/yyyy} {inicio} até {fim}, {Duracao.TotalMinutes} min, {AlunosPresentes.Count} alunos";
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string nome)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}
