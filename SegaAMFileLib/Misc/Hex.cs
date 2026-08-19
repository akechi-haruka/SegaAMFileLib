using System.Text;

namespace Haruka.Arcade.SegaAMFileLib.Misc;

static class Hex {
    public static byte[] From(string str) {
        return Enumerable.Range(0, str.Length / 2)
            .Select(x => Convert.ToByte(str.Substring(x * 2, 2), 16))
            .ToArray();
    }

    public static string To(byte[] bytes) {
        return BitConverter.ToString(bytes).Replace("-", "");
    }

    // https://stackoverflow.com/a/26206519
    public static string Dump(byte[] bytes, int length = Int32.MaxValue, int offset = 0) {
        if (bytes == null) return "<null>";
        const int bytesPerLine = 16;
        int bytesLength = bytes.Length;

        char[] hexChars = "0123456789ABCDEF".ToCharArray();

        int firstHexColumn =
            8 // 8 characters for the address
            + 3; // 3 spaces

        int firstCharColumn = firstHexColumn
                              + bytesPerLine * 3 // - 2 digit for the hexadecimal value and 1 space
                              + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                              + 2; // 2 spaces 

        int lineLength = firstCharColumn
                         + bytesPerLine // - characters to show the ascii value
                         + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

        char[] line = (new String(' ', lineLength - 2) + Environment.NewLine).ToCharArray();
        int expectedLines = ((length < Int32.MaxValue ? length : bytesLength) + bytesPerLine - 1) / bytesPerLine;
        StringBuilder result = new StringBuilder(expectedLines * lineLength);

        for (int i = offset; i < Math.Min(bytesLength, offset + length); i += bytesPerLine) {
            line[0] = hexChars[(i >> 28) & 0xF];
            line[1] = hexChars[(i >> 24) & 0xF];
            line[2] = hexChars[(i >> 20) & 0xF];
            line[3] = hexChars[(i >> 16) & 0xF];
            line[4] = hexChars[(i >> 12) & 0xF];
            line[5] = hexChars[(i >> 8) & 0xF];
            line[6] = hexChars[(i >> 4) & 0xF];
            line[7] = hexChars[(i >> 0) & 0xF];

            int hexColumn = firstHexColumn;
            int charColumn = firstCharColumn;

            for (int j = 0; j < bytesPerLine; j++) {
                if (j > 0 && (j & 7) == 0) hexColumn++;
                if (i + j >= bytesLength) {
                    line[hexColumn] = ' ';
                    line[hexColumn + 1] = ' ';
                    line[charColumn] = ' ';
                } else {
                    byte b = bytes[i + j];
                    line[hexColumn] = hexChars[(b >> 4) & 0xF];
                    line[hexColumn + 1] = hexChars[b & 0xF];
                    line[charColumn] = AsciiSymbol(b);
                }

                hexColumn += 3;
                charColumn++;
            }

            result.Append(line).Append('\n');
        }

        return result.ToString();
    }

    private static char AsciiSymbol(byte val) {
        if (val < 32) return '.'; // Non-printable ASCII
        if (val < 127) return (char)val; // Normal ASCII
        // Handle the hole in Latin-1
        if (val == 127) return '.';
        if (val < 0x90) return "€.‚ƒ„…†‡ˆ‰Š‹Œ.Ž."[val & 0xF];
        if (val < 0xA0) return ".‘’“”•–—˜™š›œ.žŸ"[val & 0xF];
        if (val == 0xAD) return '.'; // Soft hyphen: this symbol is zero-width even in monospace fonts
        return (char)val; // Normal Latin-1
    }
}