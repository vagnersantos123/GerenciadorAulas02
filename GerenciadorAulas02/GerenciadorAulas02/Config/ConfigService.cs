using Microsoft.Maui.Storage;
using System.Text.Json;

namespace GerenciadorAulas02.Config
{
    public static class ConfigService
    {
        private const string ConfigKey = "ConfiguracaoLetivo";

        public static ConfiguracaoLetivo Carregar()
        {
            var json = Preferences.Get(ConfigKey, string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                // Retorna configuração padrão
                return new ConfiguracaoLetivo
                {
                    DataInicioAno = new DateTime(DateTime.Now.Year, 2, 2),
                    DataFimAno = new DateTime(DateTime.Now.Year, 12, 2)
                };
            }

            try
            {
                var cfg = JsonSerializer.Deserialize<ConfiguracaoLetivo>(json);
                if (cfg == null)
                    throw new Exception("Configuração nula");

                // Garante que a data fim faz sentido
                if (cfg.DataFimAno <= cfg.DataInicioAno)
                    cfg.DataFimAno = cfg.DataInicioAno.AddMonths(10);

                return cfg;
            }
            catch
            {
                // Retorna padrão se JSON estiver inválido
                return new ConfiguracaoLetivo
                {
                    DataInicioAno = new DateTime(DateTime.Now.Year, 2, 2),
                    DataFimAno = new DateTime(DateTime.Now.Year, 12, 2)
                };
            }
        }


        public static void Salvar(ConfiguracaoLetivo config)
        {
            var json = JsonSerializer.Serialize(config);
            Preferences.Set(ConfigKey, json);
        }
    }
}
