using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal record WadEntry(string Name, WadEntryType Type) : IBinaryAsset<WadEntry>
{
    public static WadEntry Read(BinaryReader reader) => new(
        Name: reader.ReadPrefixedString(),
        Type: (WadEntryType)reader.ReadByte());

    public void WriteTo(BinaryWriter writer)
    {
        writer.WritePrefixed(Name);
        writer.Write((byte)Type);
    }
}
