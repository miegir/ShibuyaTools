using MessagePack;

namespace ShibuyaTools.Core;

[MessagePackObject]
public class GameVersion
{
    [Key(0)]
    public string? FileVersionString { get; set; }
    [Key(1)]
    public string? ProductVersionString { get; set; }
}
