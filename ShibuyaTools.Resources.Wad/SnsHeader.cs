using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal record SnsHeader(
    string Zero1,
    int Dummy1,
    int Zero2,
    int Dummy2,
    string Zero3,
    int Offset1,
    int Offset2,
    string Zero4,
    int Offset3,
    string Zero5,
    int Offset4,
    string Zero6,
    string Zero7,
    int Dummy3,
    string Zero8,
    string Name,
    SnsFile[] Files) : IBinaryAsset<SnsHeader>
{
    public static SnsHeader Read(BinaryReader reader) => new(
        Zero1: reader.ReadString(16),
        Dummy1: reader.ReadInt32(),
        Zero2: reader.ReadInt32(),
        Dummy2: reader.ReadInt32(),
        Zero3: reader.ReadString(12),
        Offset1: reader.ReadInt32(),
        Offset2: reader.ReadInt32(),
        Zero4: reader.ReadString(0x30),
        Offset3: reader.ReadInt32(),
        Zero5: reader.ReadString(12),
        Offset4: reader.ReadInt32(),
        Zero6: reader.ReadString(12),
        Zero7: reader.ReadString(0x50),
        Dummy3: reader.ReadInt32(),
        Zero8: reader.ReadString(12),
        Name: reader.ReadString(0x20),
        Files: reader.ReadArray<SnsFile>());

    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(Zero1, 16);
        writer.Write(Dummy1);
        writer.Write(Zero2);
        writer.Write(Dummy2);
        writer.Write(Zero3, 12);
        writer.Write(Offset1);
        writer.Write(Offset2);
        writer.Write(Zero4, 0x30);
        writer.Write(Offset3);
        writer.Write(Zero5, 12);
        writer.Write(Offset4);
        writer.Write(Zero6, 12);
        writer.Write(Zero7, 0x50);
        writer.Write(Dummy3);
        writer.Write(Zero8, 12);
        writer.Write(Name, 0x20);
        writer.Write(Files);
    }
}
