using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal record WadFile(string Name, long RawSize, long RawOffset) : IBinaryAsset<WadFile>
{
    public long Size => RawSize & 0x7FFFFFFFFFFFFFFFL;
    public long Offset => RawOffset & 0x7FFFFFFFFFFFFFFFL;

    public static WadFile Read(BinaryReader reader) => new(
        Name: reader.ReadPrefixedString(),
        RawSize: reader.ReadInt64(),
        RawOffset: reader.ReadInt64());

    public void WriteTo(BinaryWriter writer)
    {
        writer.WritePrefixed(Name);
        writer.Write(RawSize);
        writer.Write(RawOffset);
    }
}
