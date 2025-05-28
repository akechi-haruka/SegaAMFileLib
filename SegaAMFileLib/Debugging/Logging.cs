using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;

namespace Haruka.Arcade.SegaAMFileLib.Debugging {
    
    /// <summary>
    /// Main class used to interact with the Microsoft logging system.
    /// </summary>
    public static class Logging {
        
        /// <summary>
        /// The logger factory (to be used for creating loggers)
        /// </summary>
        public static ILoggerFactory Factory { get; private set; }

        /// <summary>
        /// The current "main" logger, used for any non-specific log messages.
        /// </summary>
        public static ILogger Main { get; private set; }

        /// <summary>
        /// Initializes the logging system
        /// </summary>
        /// <param name="config">The configuration to use</param>
        /// <param name="silent">If true, logging to console will be disabled.</param>
        /// <param name="enableFile">If true, logging to file will be enabled.</param>
        /// <seealso cref="Configuration"/>
        public static void Initialize(IConfigurationRoot config, bool silent = false, bool enableFile = false) {
            ArgumentNullException.ThrowIfNull(config, nameof(config));

            IConfigurationSection loggingConfig = config.GetSection("Logging");

            Factory = LoggerFactory.Create(builder => {
                builder.AddConfiguration(loggingConfig)
                    .AddDebug();
                if (!silent) {
                    builder.AddSimpleConsole(options => {
                        options.SingleLine = true;
                    });
                }
            });
            if (enableFile) {
                Factory.AddFile(loggingConfig.GetSection("File"));
            }

            Main = Factory.CreateLogger("Main");

            Main.LogInformation("Logging started.");
        }
    }
}