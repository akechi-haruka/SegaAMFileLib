using Microsoft.Extensions.Configuration;

namespace Haruka.Arcade.SegaAMFileLib {
    public static class Configuration {

        public static IConfigurationRoot Current { get; private set; }

        public static IConfigurationRoot Initialize() {
            Current = new ConfigurationBuilder()
                .AddJsonFile("segaamfilelib.json", false, true)
                .AddJsonFile("segaamfilelib.debug.json", true)
                .Build();
            return Current;
        }

        public static string Get(string section, string value) {
            return Current.GetSection(section)?.GetSection(value)?.Value;
        }

        public static string Get(string section, string subsection, string value) {
            return Current.GetSection(section)?.GetSection(subsection)?.GetSection(value)?.Value;
        }

        public static int GetInt(string section, string value) {
            return (Current.GetSection(section)?.GetValue<int>(value)).Value;
        }

        public static bool GetBool(string section, string value) {
            return (Current.GetSection(section)?.GetValue<bool>(value)).Value;
        }
    }
}
