using System.Collections.Frozen;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal sealed class WadArchive : IDisposable
{
    private readonly FileSource source;
    private readonly WadHeader header;
    private readonly long dataOffset;
    private readonly FrozenSet<string> existingFileNames;
    private readonly List<TargetFile> targetFileSourceList = [];
    private Stream? stream;

    public WadArchive(FileSource source)
    {
        this.source = source;
        var stream = EnsureStream();
        using var reader = CreateReader(stream);
        header = WadHeader.Read(reader);
        dataOffset = stream.Position;
        existingFileNames = header.Files
            .Select(file => file.Name)
            .ToFrozenSet();
    }

    public IReadOnlyList<WadFile> Files => header.Files;

    public void Dispose()
    {
        Close();
    }

    public bool Exists(string name) => existingFileNames.Contains(name);

    public byte[] Read(WadFile file)
    {
        var stream = EnsureStream();
        using var reader = CreateReader(stream);
        stream.Position = dataOffset + file.Offset;
        return reader.ReadBytes((int)file.Size);
    }

    public void Export(WadFile file, string path)
    {
        var stream = EnsureStream();
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

        Close();

        logger.LogInformation("creating target...");
        var scope = logger.BeginScope("creating target");
        var progressReporter = new ProgressReporter(logger);
        using var target = source.CreateTarget(progressReporter.ReportProgress);
        scope?.Dispose();
        logger.LogInformation("writing header...");

        using var writer = new BinaryWriter(target.Stream);
        targetHeader.WriteTo(writer);
        writer.Flush();

        var totalLength = target.Stream.Position + targetFileSourceList.Sum(file => file.Body.GetLength());

        logger.LogInformation("writing content...");
        scope = logger.BeginScope("writing content");
        progressReporter.Restart();

        foreach (var file in targetFileSourceList)
        {
            switch (file.Body)
            {
                case TargetBody.Internal(var offset, var length):
                    var stream = EnsureStream();
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

            progressReporter.ReportProgress(
                progress: new ProgressPayload<long>(
                    Total: totalLength,
                    Position: target.Stream.Position));
        }

        scope?.Dispose();
        logger.LogDebug("writing wad done.");
        Close();
        target.Commit();
    }

    private Stream EnsureStream() => stream ??= source.OpenRead();

    private static BinaryReader CreateReader(Stream stream)
    {
        return new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
    }

    private void Close()
    {
        var disposable = stream;
        if (disposable is not null)
        {
            stream = null;
            disposable?.Dispose();
        }
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
