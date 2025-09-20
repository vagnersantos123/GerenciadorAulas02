using System;
using GerenciadorAulas02.Config;

namespace GerenciadorAulas02.Helpers
{
    public static class PreferenciasHelper
    {
        // ==============================
        // LEITURA
        // ==============================
        public static DateTime GetDataInicioAno()
        {
            var cfg = ConfigService.Carregar();
            return cfg.DataInicioAno;
        }

        public static DateTime GetDataFimAno()
        {
            var cfg = ConfigService.Carregar();
            return cfg.DataFimAno;
        }

        // ==============================
        // GRAVAÇÃO
        // ==============================
        /// <summary>
        /// Atualiza a data de início e automaticamente define a data de fim (+10 meses).
        /// </summary>
        public static void SalvarDatas(DateTime inicio)
        {
            var cfg = ConfigService.Carregar();
            cfg.DataInicioAno = inicio;
            cfg.DataFimAno = inicio.AddMonths(10);

            ConfigService.Salvar(cfg);
        }

        /// <summary>
        /// Atualiza início e fim com valores específicos.
        /// </summary>
        public static void SalvarDatas(DateTime inicio, DateTime fim)
        {
            var cfg = ConfigService.Carregar();
            cfg.DataInicioAno = inicio;
            cfg.DataFimAno = fim;

            ConfigService.Salvar(cfg);
        }
    }
}
