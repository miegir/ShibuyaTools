namespace ShibuyaTools.Resources.Wad;

internal static class XmlText
{
    public static string Compact(string s) => string.Create(s.Length, s, static (span, s) =>
    {
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = s[i] switch
            {
                'А' => 'A',
                'В' => 'B',
                'С' => 'C',
                'Е' => 'E',
                'Н' => 'H',
                'К' => 'K',
                'М' => 'M',
                'О' => 'O',
                'Р' => 'P',
                'Т' => 'T',
                'Х' => 'X',
                'а' => 'a',
                'с' => 'c',
                'е' => 'e',
                'о' => 'o',
                'р' => 'p',
                'х' => 'x',
                'у' => 'y',
                var c => c,
            };
        }
    });
}
