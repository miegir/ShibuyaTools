namespace ShibuyaTools.Resources.Wad;

internal static class SnsEncoder
{
    private static readonly byte[] Key = "CSD-CSCNV:01.82\0"u8.ToArray();
    private static readonly int KeyLength = Key.Length;

    public static void EncodeBuffer(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] += Key[i % KeyLength];
        }
    }

    public static void DecodeBuffer(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] -= Key[i % KeyLength];
        }
    }
}
