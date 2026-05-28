using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.Misc;

/// <summary>
/// Utility functions for game/app IDs.
/// </summary>
public static class GameID {
    /// <summary>
    /// The app ID for APM v3.
    /// </summary>
    public const string APM_APP_ID = "SDEM";

    /// <summary>
    /// The app ID stored in system (.pack) containers.
    /// </summary>
    public const string SYSTEM_APP_ID = "----";

    /// <summary>
    /// Checks if the given app ID is valid.
    /// </summary>
    /// <param name="gameId">The ID to check</param>
    /// <returns>true if valid, false if not</returns>
    /// <exception cref="ArgumentNullException"><paramref name="gameId"/> is null</exception>
    public static bool IsValid(string gameId) {
        ArgumentNullException.ThrowIfNull(gameId);
        if (gameId.Length != 4) {
            Log.Main.LogError("GameID could not be validated: length is invalid: " + gameId);
            return false;
        }

        if (gameId.Equals(SYSTEM_APP_ID)) {
            return true;
        }

        foreach (char c in gameId) {
            if (!Char.IsAsciiLetter(c)) {
                Log.Main.LogError("GameID could not be validated: contains non-ASCII letters: " + gameId);
                return false;
            } else if (!Char.IsUpper(c)) {
                Log.Main.LogError("GameID could not be validated: contains non-uppercase letters: " + gameId);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if the given app ID belongs to APM v3.
    /// </summary>
    /// <param name="appId">The app ID to check</param>
    /// <returns>true if the app ID is APM v3, false if not.</returns>
    public static bool IsApm(string appId) {
        return appId == APM_APP_ID;
    }
}