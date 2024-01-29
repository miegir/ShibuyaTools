namespace ShibuyaTools.Core;

public static class ObjectContainerExtensions
{
    public static GameVersion? FindGameVersion(this ObjectContainer container)
    {
        return container.TryGetEntry(GameVersionStatics.Path, out var entry)
            ? entry.AsObjectSource<GameVersion>().Deserialize()
            : null;
    }
}
