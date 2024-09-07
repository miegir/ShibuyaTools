using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.CompilerServices;
using DotNext.IO.MemoryMappedFiles;

namespace ShibuyaTools;

// see https://github.com/dotnet/runtime/tree/main/src/installer/managed/Microsoft.NET.HostModel
internal static class BundleHelper
{
    private static readonly byte[] BundleSignature =
    {
        // 32 bytes represent the bundle signature: SHA-256 for ".net core bundle"
        0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
        0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
        0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
        0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae,
    };

    public static Stream? OpenBundle(string bundlePath, string filePath, string manifestResourceName)
    {
        using var memoryMappedFile = MemoryMappedFile.CreateFromFile(bundlePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);

        long headerOffset;
        using (var accessor = memoryMappedFile.CreateMemoryAccessor(0, 0, MemoryMappedFileAccess.Read))
        {
            var index = accessor.Bytes.IndexOf(BundleSignature) - sizeof(long);
            if (index < 0) return null;
            headerOffset = Unsafe.ReadUnaligned<long>(ref accessor[index]);
        }

        using var stream = memoryMappedFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
        using var reader = new BinaryReader(stream);

        stream.Position = headerOffset;

        var major = reader.ReadInt32();

        if (major > 6)
        {
            throw new NotSupportedException($"Bundle version not supported: {major}.");
        }

        var minor = reader.ReadInt32();
        var count = reader.ReadInt32();

        _ = reader.ReadString();

        if (major >= 2)
        {
            _ = reader.ReadInt64();
            _ = reader.ReadInt64();
            _ = reader.ReadInt64();
            _ = reader.ReadInt64();
            _ = reader.ReadInt64();
        }

        for (var i = 0; i < count; i++)
        {
            var offset = reader.ReadInt64();
            var size = reader.ReadInt64();

            var compressedSize = major >= 6
                ? reader.ReadInt64()
                : 0;

            var fileType = reader.ReadByte();
            if (fileType != 1) // Assembly
            {
                continue;
            }

            var relativePath = reader.ReadString();

            if (relativePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            {
                return LoadAssembly(offset, size, compressedSize);
            }
        }

        return null;

        Stream? LoadAssembly(long offset, long size, long compressedSize)
        {
            stream.Position = offset;

            var comressed = compressedSize > 0;

            if (!comressed)
            {
                compressedSize = size;
            }

            if (compressedSize > int.MaxValue)
            {
                throw new NotSupportedException("Assembly too large.");
            }

            var bytes = reader.ReadBytes((int)compressedSize);

            if (comressed)
            {
                using var assemblyStream = new MemoryStream(bytes);
                using var compressedStream = new DeflateStream(assemblyStream, CompressionMode.Decompress);
                using var uncompressedStream = new MemoryStream();

                compressedStream.CopyTo(uncompressedStream);

                bytes = uncompressedStream.ToArray();
            }

            var assembly = Assembly.Load(bytes);

            var source = assembly.GetManifestResourceStream(manifestResourceName)
                ?? throw new FileNotFoundException($"Manifest resource '{manifestResourceName}' not found in the assembly '{filePath}'.");

            var target = new MemoryStream();
            source.CopyTo(target);
            target.Position = 0;

            return target;
        }
    }
}
