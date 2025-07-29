using Haruka.Arcade.SegaAMFileLib.Debugging;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.Misc;

public static class GameID {

    public static bool IsValid(string gameId) {
        ArgumentNullException.ThrowIfNull(gameId);
        if (gameId.Length != 4) {
            Logging.Main.LogError("GameID could not be validated: length is invalid: " + gameId);
            return false;
        }

        foreach (char c in gameId) {
            if (!Char.IsAsciiLetter(c)) {
                Logging.Main.LogError("GameID could not be validated: contains non-ASCII letters: " + gameId);
                return false;
            } else if (!Char.IsUpper(c)) {
                Logging.Main.LogError("GameID could not be validated: contains non-uppercase letters: " + gameId);
                return false;
            }
        }

        return true;
    }
    
}