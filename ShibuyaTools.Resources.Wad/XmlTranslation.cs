using MessagePack;

namespace ShibuyaTools.Resources.Wad;

[MessagePackObject]
public record XmlTranslation(
    [property: Key(0)] string Key,
    [property: Key(1)] string Val);
