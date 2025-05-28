using Microsoft.Extensions.Configuration;

namespace Haruka.Arcade.SegaAMFileLib {
    
    /// <summary>
    /// Main class handling library configuration (for now, mainly logging)
    /// </summary>
    public static class Configuration {

        internal static IConfigurationRoot Current { get; private set; }

        /// <summary>
        /// Initializes the configuration from the default files.
        /// </summary>
        /// <returns>An accessor to configuration data.</returns>
        public static IConfigurationRoot Initialize() {
            Current = new ConfigurationBuilder()
                .AddJsonFile("segaamfilelib.json", false, true)
                .AddJsonFile("segaamfilelib.debug.json", true)
                .Build();
            return Current;
        }

        static string Get(string section, string value) {
            return Current.GetSection(section)?.GetSection(value)?.Value;
        }

        static string Get(string section, string subsection, string value) {
            return Current.GetSection(section)?.GetSection(subsection)?.GetSection(value)?.Value;
        }

        static int GetInt(string section, string value) {
            return (Current.GetSection(section)?.GetValue<int>(value)).Value;
        }

        static bool GetBool(string section, string value) {
            return (Current.GetSection(section)?.GetValue<bool>(value)).Value;
        }
    }
}
