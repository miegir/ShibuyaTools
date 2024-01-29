using System.Text;

namespace ShibuyaTools.Resources.Wad;

internal static class Phrases
{
    private static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static byte[] Encode(string s) => Encoding.GetBytes(s);

    public static IEnumerable<(int Start, int End, string Phrase)> Detect(byte[] bytes)
    {
        var f = false;
        var s = 0;

        for (var i = 0; i < bytes.Length; i++)
        {
            switch (bytes[i])
            {
                case 1 when !f:
                    f = true;
                    s = i + 1;
                    break;

                case 1 when f:
                    s = i + 1;
                    break;

                case 2 when f:
                    f = false;
                    var phrase = Encoding.GetString(bytes[s..i]);
                    if (IsValid(phrase)) yield return (s, i, phrase);
                    break;
            }
        }
    }

    private static bool IsValid(string phrase)
    {
        return phrase.Length > 0 && !phrase.Any(c => c < 20 || c > 65532);
    }
}
