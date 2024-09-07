using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal abstract class Xml
{
    public static readonly Xml Sns = new XmlSns();
    public static readonly Xml Sns64 = new XmlSns64();

    private record Bookmark(int Location, long LocationOffset);

    public XmlTranslation[] Parse(byte[] body)
    {
        using var stream = new MemoryStream(body);
        using var reader = new BinaryReader(stream);

        var scriptOffset = (int)ReadScriptOffset(reader);
        var bookmarkList = ReadBookmarkList(reader, scriptOffset);

        var sourcePtr = scriptOffset;
        var translations = new List<XmlTranslation>();

        foreach (var bookmark in bookmarkList)
        {
            var offset = scriptOffset + bookmark.Location;
            RegisterBlock(body[sourcePtr..offset]);
        }

        RegisterBlock(body[sourcePtr..]);

        return [.. translations];

        void RegisterBlock(byte[] body)
        {
            var ptr = 0;

            foreach (var (_, b, key) in Phrases.Detect(body))
            {
                translations.Add(new XmlTranslation(Key: key, Val: key));
                ptr = b;
            }

            sourcePtr += body.Length;
        }
    }

    public ReadOnlySpan<byte> Translate(ILogger logger, XmlTranslation[] existing, ReadOnlySpan<byte> body)
    {
        var index = 0;
        var showError = true;

        string popval(string key)
        {
            if (index >= existing.Length)
            {
                logger.LogError("translation eos at key '{key}'", key);
                return key;
            }

            var i = index++;
            var e = existing[i];
            if (e.Key == key)
            {
                return XmlText.Compact(e.Val);
            }

            if (showError)
            {
                logger.LogError("wrong key [{index}] '{actualKey}'; expected '{expectedKey}'", i, e.Key, key);
                showError = false;
            }

            return key;
        }

        using var stream = new MemoryStream(body.Length);
        using var reader = new BinaryReader(stream);

        stream.Write(body);

        var scriptOffset = (int)ReadScriptOffset(reader);
        var bookmarkList = ReadBookmarkList(reader, scriptOffset);

        var sourcePtr = scriptOffset;
        var targetPtr = scriptOffset;

        var blocks = new List<byte[]>();
        var bookmarkBlock = body[..scriptOffset].ToArray();

        using var bookmarkStream = new MemoryStream(bookmarkBlock);
        using var bookmarkWriter = new BinaryWriter(bookmarkStream);

        blocks.Add(bookmarkBlock);

        void RegisterBlock(ReadOnlySpan<byte> body)
        {
            var block = new List<byte>(body.Length);
            var ptr = 0;

            foreach (var (a, b, phrase) in Phrases.Detect(body.ToArray()))
            {
                var translation = popval(phrase);
                block.AddRange(body[ptr..a]);
                block.AddRange(Phrases.Encode(translation));
                ptr = b;
            }

            block.AddRange(body[ptr..]);
            blocks.Add([.. block]);
            sourcePtr += body.Length;
            targetPtr += block.Count;
        }

        foreach (var bookmark in bookmarkList)
        {
            var offset = scriptOffset + bookmark.Location;
            RegisterBlock(body[sourcePtr..offset]);
            bookmarkStream.Position = bookmark.LocationOffset;
            bookmarkWriter.Write(targetPtr - scriptOffset);
            bookmarkWriter.Flush();
        }

        RegisterBlock(body[sourcePtr..].TrimEnd((byte)0));

        var totalLength = blocks.Sum(b => b.Length);
        var alignment = totalLength % 4;

        if (alignment != 0)
        {
            totalLength += 4 - alignment;
        }

        var result = new byte[totalLength];
        var p = 0;

        foreach (var block in blocks)
        {
            block.CopyTo(result, p);
            p += block.Length;
        }

        return result;
    }

    protected abstract long ReadScriptOffset(BinaryReader reader);

    private static List<Bookmark> ReadBookmarkList(BinaryReader reader, long scriptOffset)
    {
        var stream = reader.BaseStream;
        var bookmarkList = new List<Bookmark>();

        while (stream.Position < scriptOffset)
        {
            _ = reader.ReadInt32(); // kind
            var locationOffset = stream.Position;
            var location = reader.ReadInt32();
            _ = reader.ReadNullTerminatedString(); // name

            stream.Align();

            bookmarkList.Add(
                new Bookmark(
                    Location: location,
                    LocationOffset: locationOffset));
        }

        bookmarkList.Sort((a, b) => a.Location - b.Location);

        return bookmarkList;
    }

    private class XmlSns : Xml
    {
        protected override long ReadScriptOffset(BinaryReader reader)
        {
            reader.BaseStream.Position = 0x430;

            return reader.ReadInt32();
        }
    }

    private class XmlSns64 : Xml
    {
        protected override long ReadScriptOffset(BinaryReader reader)
        {
            reader.BaseStream.Position = 0x830;

            return reader.ReadInt64();
        }
    }
}
