using System;
using System.Collections.Generic;

namespace GerenciadorAulas02.Models
{
    /// <summary>
    /// Configurações globais do calendário letivo.
    /// </summary>
    public class ConfiguracoesLetivo
    {
        /// <summary>
        /// Data de início do ano letivo.
        /// </summary>
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data de término do ano letivo.
        /// </summary>
        public DateTime DataFim { get; set; }

        /// <summary>
        /// Dias da semana considerados letivos (ex: segunda a sexta).
        /// </summary>
        public List<DayOfWeek> DiasLetivos { get; set; } = new List<DayOfWeek>
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };

        /// <summary>
        /// Lista de feriados no ano letivo.
        /// </summary>
        public List<DateTime> Feriados { get; set; } = new List<DateTime>();
    }
}
