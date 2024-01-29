using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal record WadHeader(
    byte[] Magick,
    int Major,
    int Minor,
    byte[] Extra,
    WadFile[] Files,
    WadDirectory[] Directories) : IBinaryAsset<WadHeader>
{
    public static WadHeader Read(BinaryReader reader) => new(
        Magick: reader.ReadMagick("AGAR"u8),
        Major: reader.ReadInt32(),
        Minor: reader.ReadInt32(),
        Extra: reader.ReadPrefixedBytes(),
        Files: reader.ReadArray<WadFile>(),
        Directories: reader.ReadArray<WadDirectory>());

    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(Magick);
        writer.Write(Major);
        writer.Write(Minor);
        writer.WritePrefixed(Extra);
        writer.Write(Files);
        writer.Write(Directories);
    }
}
