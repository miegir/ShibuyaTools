using System.Text;

namespace ShibuyaTools.Core;

public static class BinaryReaderExtensions
{
    public static byte[] ReadMagick(this BinaryReader reader, ReadOnlySpan<byte> magick)
    {
        var bytes = reader.ReadBytes(magick.Length);

        if (!magick.SequenceEqual(bytes))
        {
            throw new InvalidMagickException();
        }

        return bytes;
    }

    public static byte[] ReadPrefixedBytes(this BinaryReader reader)
    {
        return reader.ReadBytes(reader.ReadInt32());
    }

    public static T[] ReadArray<T>(this BinaryReader reader) where T : IBinaryAsset<T>
    {
        var length = reader.ReadInt32();
        var result = new T[length];

        for (var i = 0; i < length; i++)
        {
            result[i] = T.Read(reader);
        }

        return result;
    }

    public static string ReadPrefixedString(this BinaryReader reader)
    {
        return Encoding.ASCII.GetString(reader.ReadPrefixedBytes());
    }

    public static string ReadString(this BinaryReader reader, int length)
    {
        return Encoding.ASCII.GetString(reader.ReadBytes(length)).TrimEnd('\0');
    }

    public static string ReadNullTerminatedString(this BinaryReader reader)
    {
        var bytes = new List<byte>();

        while (true)
        {
            switch (reader.Read())
            {
                case 0:
                    return Encoding.ASCII.GetString(bytes.ToArray());

                case -1:
                    throw new EndOfStreamException();

                case var ch:
                    bytes.Add((byte)ch);
                    break;
            }
        }
    }

    public static byte[] ReadBytesToOffset(this BinaryReader reader, long offset)
    {
        var len = offset - reader.BaseStream.Position;
        return reader.ReadBytes((int)len);
    }

    public static byte[] ReadBytesToEnd(this BinaryReader reader)
    {
        return reader.ReadBytesToOffset(reader.BaseStream.Length);
    }
}
