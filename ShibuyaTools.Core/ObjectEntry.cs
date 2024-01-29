using System.IO.Compression;

namespace ShibuyaTools.Core;

public class ObjectEntry(ZipArchiveEntry entry)
{
    public DateTime LastWriteTimeUtc => entry?.LastWriteTime.UtcDateTime ?? DateTime.MinValue;

    public IObjectSource<T> AsObjectSource<T>() => new ObjectSource<T>(entry);

    public IStreamSource AsStreamSource() => new StreamSource(entry);

    public byte[] AsBytes()
    {
        using var source = entry.Open();
        using var target = new MemoryStream();
        source.CopyTo(target);
        return target.ToArray();
    }

    private class ObjectSource<T> : IObjectSource<T>
    {
        private readonly ZipArchiveEntry entry;

        public ObjectSource(ZipArchiveEntry entry) => this.entry = entry;

        public T Deserialize()
        {
            using var stream = entry.Open();
            return ObjectSerializer.Deserialize<T>(stream);
        }
    }

    private class StreamSource : IStreamSource
    {
        private readonly ZipArchiveEntry entry;

        public StreamSource(ZipArchiveEntry entry) => this.entry = entry;

        public Stream OpenRead() => entry.Open();
    }
}
