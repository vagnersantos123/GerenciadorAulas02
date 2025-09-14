using System;
using System.Collections.Generic;
using System.Linq;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.Config;

namespace GerenciadorAulas02.Services
{
    public class CalendarioLetivoService
    {
        private readonly ConfiguracoesLetivo _config;

        public CalendarioLetivoService(ConfiguracoesLetivo config)
        {
            _config = config;
        }

        /// <summary>
        /// Gera todas as datas de aula válidas dentro do período do ano letivo,
        /// respeitando dias da semana permitidos e feriados.
        /// </summary>
        public List<DateTime> GerarDatasLetivas()
        {
            var datas = new List<DateTime>();

            if (_config == null || _config.DataInicio == default || _config.DataFim == default)
                return datas;

            for (var data = _config.DataInicio; data <= _config.DataFim; data = data.AddDays(1))
            {
                // 1. Verifica se o dia da semana está marcado como "letivo"
                if (!_config.DiasLetivos.Contains(data.DayOfWeek))
                    continue;

                // 2. Pula feriados
                if (_config.Feriados.Any(f => f.Date == data.Date))
                    continue;

                datas.Add(data);
            }

            return datas;
        }
    }
}
