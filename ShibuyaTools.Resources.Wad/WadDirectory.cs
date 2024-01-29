using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal record WadDirectory(string Name, WadEntry[] Entries) : IBinaryAsset<WadDirectory>
{
    public static WadDirectory Read(BinaryReader reader) => new(
        Name: reader.ReadPrefixedString(),
        Entries: reader.ReadArray<WadEntry>());

    public void WriteTo(BinaryWriter writer)
    {
        writer.WritePrefixed(Name);
        writer.Write(Entries);
    }
}
