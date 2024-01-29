using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal sealed class WadArchive : IDisposable
{
    private readonly FileSource source;
    private readonly Stream stream;
    private readonly BinaryReader reader;
    private readonly WadHeader header;
    private readonly long dataOffset;
    private readonly FrozenSet<string> existingFileNames;
    private readonly List<TargetFile> targetFileSourceList = [];

    public WadArchive(FileSource source)
    {
        this.source = source;
        stream = source.OpenRead();
        reader = new BinaryReader(stream);
        header = WadHeader.Read(reader);
        dataOffset = stream.Position;
        existingFileNames = header.Files
            .Select(file => file.Name)
            .ToFrozenSet();
    }

    public IReadOnlyList<WadFile> Files => header.Files;

    public void Dispose()
    {
        reader.Dispose();
        stream.Dispose();
    }

    public bool Exists(string name) => existingFileNames.Contains(name);

    public byte[] Read(WadFile file)
    {
        stream.Position = dataOffset + file.Offset;
        return reader.ReadBytes((int)file.Size);
    }

    public void Export(WadFile file, string path)
    {
        using var target = new FileTarget(path);
        stream.Position = dataOffset + file.Offset;
        stream.CopyBytesTo(target.Stream, file.Size);
        target.Commit();
    }

    public void AddFile(WadFile file)
    {
        var body = new TargetBody.Internal(dataOffset + file.Offset, file.Size);
        targetFileSourceList.Add(new TargetFile(Name: file.Name, Body: body));
    }

    public void AddFile(string name, FileInfo sourceInfo)
    {
        var body = new TargetBody.External(sourceInfo);
        targetFileSourceList.Add(new TargetFile(Name: name, Body: body));
    }

    public void AddFile(string name, byte[] bytes)
    {
        var body = new TargetBody.Buffer(bytes);
        targetFileSourceList.Add(new TargetFile(Name: name, Body: body));
    }

    public void Save(ILogger logger)
    {
        var targetFileOffset = 0L;
        var targetFiles = new WadFile[targetFileSourceList.Count];
        for (var i = 0; i < targetFiles.Length; i++)
        {
            var file = targetFileSourceList[i];
            var pos = targetFileOffset;
            var length = file.Body.GetLength();
            targetFileOffset += length;
            targetFiles[i] = new WadFile(
                Name: file.Name,
                RawOffset: pos,
            RawSize: length);
        }

        var targetHeader = header with { Files = targetFiles };
        using var target = source.CreateTarget();
        using var writer = new BinaryWriter(target.Stream);

        targetHeader.WriteTo(writer);
        writer.Flush();

        var totalLength = FormatLength(target.Stream.Position + targetFileSourceList.Sum(file => file.Body.GetLength()));
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("writing wad...");

        foreach (var file in targetFileSourceList)
        {
            switch (file.Body)
            {
                case TargetBody.Internal(var offset, var length):
                    stream.Position = offset;
                    stream.CopyBytesTo(target.Stream, length);
                    break;

                case TargetBody.External(var info):
                    using (var infoStream = info.OpenRead())
                    {
                        infoStream.CopyTo(target.Stream);
                        break;
                    }

                case TargetBody.Buffer(var buffer):
                    target.Stream.Write(buffer);
                    break;
            }

            if (stopwatch.Elapsed.TotalSeconds > 1)
            {
                logger.LogDebug("written {count} of {total}", FormatLength(target.Stream.Position), totalLength);
                stopwatch.Restart();
            }
        }

        logger.LogDebug("writing wad done.");
        stream.Close();
        target.Commit();
    }

    private static string FormatLength(float length)
    {
        if (length < 1024) return $"{length:0.00}B";
        length /= 1024;
        if (length < 1024) return $"{length:0.00}KB";
        length /= 1024;
        if (length < 1024) return $"{length:0.00}MB";
        return $"{length:0.00}GB";
    }

    private record TargetFile(string Name, TargetBody Body);

    private abstract record TargetBody
    {
        public abstract long GetLength();

        public record Internal(long Offset, long Length) : TargetBody
        {
            public override long GetLength() => Length;
        }

        public record External(FileInfo Info) : TargetBody
        {
            public override long GetLength() => Info.Length;
        }

        public record Buffer(byte[] Bytes) : TargetBody
        {
            public override long GetLength() => Bytes.LongLength;
        }
    }
}
