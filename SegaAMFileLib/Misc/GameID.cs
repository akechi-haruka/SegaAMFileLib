using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.Misc;

public static class GameID {
    public const string APM_APP_ID = "SDEM";
    public const string SYSTEM_APP_ID = "----";

    public static bool IsValid(string gameId) {
        ArgumentNullException.ThrowIfNull(gameId);
        if (gameId.Length != 4) {
            Log.Main.LogError("GameID could not be validated: length is invalid: " + gameId);
            return false;
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

    public static bool IsApm(string appId) {
        return appId == APM_APP_ID;
    }
}