using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;

namespace Haruka.Arcade.SegaAMFileLib.Debugging {
    class Logging {
        
        public static ILoggerFactory Factory { get; private set; }

        public static ILogger Main { get; private set; }

        public static void Initialize(IConfigurationRoot config, bool enableFile = false) {
            ArgumentNullException.ThrowIfNull(config, nameof(config));

            IConfigurationSection loggingConfig = config.GetSection("Logging");

            Factory = LoggerFactory.Create(builder => builder
                .AddConfiguration(loggingConfig)
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                })
                .AddDebug()
            );
            if (enableFile) {
                Factory.AddFile(loggingConfig.GetSection("File"));
            }
            Main = Factory.CreateLogger("Main");

            Main.LogInformation("Logging started.");
        }

    }
}
