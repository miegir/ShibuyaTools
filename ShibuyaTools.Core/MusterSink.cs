using System.IO.Compression;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace ShibuyaTools.Core;

public class MusterSink(ILogger logger)
{
    private static readonly MessagePackSerializerOptions MessageOptions =
        MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

    private record ObjectEntry(string Name, string Path);
    private record StreamEntry(string Name, IObjectStreamSource Source);

    private readonly HashSet<string> directories = [];
    private readonly List<ObjectEntry> objects = [];
    private readonly List<StreamEntry> streams = [];
    private DateTime lastWriteTimeUtc;

    public void ReportDirectory(ObjectPath name)
    {
        directories.Add(name.Name);
    }

    public void ReportObject(ObjectPath name, string path)
    {
        var info = new FileInfo(path);

        if (!info.Exists)
        {
            return;
        }

        objects.Add(new ObjectEntry(name.Name, path));

        if (lastWriteTimeUtc < info.LastWriteTimeUtc)
        {
            lastWriteTimeUtc = info.LastWriteTimeUtc;
        }
    }

    public void ReportObject(ObjectPath name, IObjectStreamSource source)
    {
        if (!source.Exists)
        {
            return;
        }

        streams.Add(new StreamEntry(name.Name, source));

        if (lastWriteTimeUtc < source.LastWriteTimeUtc)
        {
            lastWriteTimeUtc = source.LastWriteTimeUtc;
        }
    }

    public void Pack(PackArguments arguments)
    {
        var archiveInfo = new FileInfo(arguments.ArchivePath);

        if (objects.Count == 0)
        {
            if (archiveInfo.Exists)
            {
                logger.LogInformation("removing archive...");
                archiveInfo.Delete();
            }

            return;
        }

        if (!arguments.Force && archiveInfo.LastWriteTimeUtc >= lastWriteTimeUtc)
        {
            return;
        }

        logger.LogInformation("writing archive...");

        using var target = new FileTarget(arguments.ArchivePath);
        WriteArchive();
        target.Commit();

        void WriteArchive()
        {
            using var archive = new ZipArchive(target.Stream, ZipArchiveMode.Create);

            if (directories.Count > 0)
            {
                var entry = archive.CreateEntry(".dir");
                using var stream = entry.Open();
                MessagePackSerializer.Serialize(stream, directories, MessageOptions);
            }

            foreach (var (name, path) in objects)
            {
                archive.CreateEntryFromFile(path, name);
            }

            foreach (var (name, source) in streams)
            {
                var entry = archive.CreateEntry(name);
                using var targetStream = entry.Open();
                using var sourceStream = source.OpenRead();
                sourceStream.CopyTo(targetStream);
            }
        }
    }
}
