using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal record SnsFile(int Offset, int Size) : IBinaryAsset<SnsFile>
{
    public static SnsFile Read(BinaryReader reader) => new(
        Offset: reader.ReadInt32(),
        Size: reader.ReadInt32());

    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(Offset);
        writer.Write(Size);
    }
}
