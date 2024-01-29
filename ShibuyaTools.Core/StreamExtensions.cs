using System.Buffers;

namespace ShibuyaTools.Core;

public static class StreamExtensions
{
    private const int DefaultBufferSize = 32767;

    public static void CopyBytesTo(this Stream source, Stream target, long count)
    {
        var bufferSize = DefaultBufferSize;

        if (bufferSize > count)
        {
            bufferSize = (int)count;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        try
        {
            while (count > 0)
            {
                var remaining = bufferSize;
                if (remaining > count)
                {
                    remaining = (int)count;
                }

                var read = source.Read(buffer, 0, remaining);

                if (read == 0)
                {
                    throw new EndOfStreamException();
                }

                target.Write(buffer, 0, read);
                count -= read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static void Align(this Stream stream, long alignment = 4)
    {
        var offset = stream.Position % alignment;
        if (offset != 0)
        {
            stream.Position += alignment - offset;
        }
    }
}
