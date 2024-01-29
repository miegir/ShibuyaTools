using System.Buffers;
using System.Text;

namespace ShibuyaTools.Core;

public static class BinaryWriterExtensions
{
    public static void WritePrefixed(this BinaryWriter writer, byte[] value)
    {
        writer.Write(value.Length);
        writer.Write(value);
    }

    public static void WritePrefixed(this BinaryWriter writer, string value)
    {
        writer.WritePrefixed(Encoding.ASCII.GetBytes(value));
    }

    public static void Write<T>(this BinaryWriter writer, T[] value) where T : IBinaryAsset<T>
    {
        writer.Write(value.Length);

        foreach (var element in value)
        {
            element.WriteTo(writer);
        }
    }

    public static void Write(this BinaryWriter writer, string value, int length)
    {
        var bytes = new byte[length];
        Encoding.ASCII.GetBytes(value.AsSpan(), bytes);
        writer.Write(bytes);
    }
}
