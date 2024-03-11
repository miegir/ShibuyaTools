using System.Text;
using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal sealed class SnsArchive : IDisposable
{
    private readonly Stream stream;
    private readonly BinaryReader reader;
    private readonly SnsHeader header;
    private readonly int firstFileOffset;
    private readonly List<FileSource> files;

    public SnsArchive(byte[] bytes)
    {
        stream = new MemoryStream(bytes);
        reader = new BinaryReader(stream);
        header = SnsHeader.Read(reader);
        firstFileOffset = (int)stream.Position;
        files = new List<FileSource>(header.Files.Length);
    }

    public IReadOnlyList<SnsFile> Files => header.Files;

    public void Dispose()
    {
        reader.Dispose();
        stream.Dispose();
    }

    public string ReadName(SnsFile file)
    {
        stream.Position = file.Offset;
        return reader.ReadString(0x30);
    }

    public KeyValuePair<string, byte[]> Read(SnsFile file)
    {
        stream.Position = file.Offset;
        var bytes = reader.ReadBytes(file.Size);
        var name = Encoding.ASCII.GetString(bytes, 0, 0x30).TrimEnd('\0');
        return new KeyValuePair<string, byte[]>(name, bytes);
    }

    public void AddFile(string name, byte[] body)
    {
        files.Add(new FileSource(Name: name, Body: body));
    }

    public byte[] Save()
    {
        var offset = firstFileOffset;

        stream.Position = header.Offset1;

        var tail1 = reader.ReadBytesToOffset(header.Offset2);
        var tail2 = reader.ReadBytesToOffset(header.Offset3);
        var tail3 = reader.ReadBytesToOffset(header.Offset4);
        var tail4 = reader.ReadBytesToEnd();

        var targetFiles = new SnsFile[files.Count];
        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var pos = offset;
            var length = file.Body.Length;
            offset += length;
            targetFiles[i] = new SnsFile(Offset: pos, Size: length);
        }

        var tail1Offset = offset;
        var tail2Offset = tail1Offset + tail1.Length;
        var tail3Offset = tail2Offset + tail2.Length;
        var tail4Offset = tail3Offset + tail3.Length;
        var offsetDiff = tail1Offset - header.Offset1;

        var targetHeader = header with
        {
            Offset1 = tail1Offset,
            Offset2 = tail2Offset,
            Offset3 = tail3Offset,
            Offset4 = tail4Offset,
            Files = targetFiles,
        };

        using var writerStream = new MemoryStream();
        using var writer = new BinaryWriter(writerStream);

        targetHeader.WriteTo(writer);

        foreach (var file in files)
        {
            writer.Write(file.Body);
        }

        writer.Write(tail1);
        writer.Write(tail2);

        // fix offsets in the tail3
        using var offsetStream = new MemoryStream(tail3);
        using var offsetReader = new BinaryReader(offsetStream);

        var offsetCount = header.Files.Length * 2;
        for (var i = 0; i < offsetCount; i++)
        {
            var originalOffset = offsetReader.ReadInt32();
            var updatedOffset = originalOffset == 0 ? 0 : originalOffset + offsetDiff;
            writer.Write(updatedOffset);
        }

        writer.Flush();
        offsetStream.CopyTo(writerStream);
        writer.Write(tail4);
        writer.Flush();

        return writerStream.ToArray();
    }

    private record FileSource(string Name, byte[] Body);
}
