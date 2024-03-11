using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal class WadChangeTracker(FileDestination destination)
{
    private readonly DateTime destinationTimeUtc = destination.FileState.LastWriteTimeUtc;
    private DateTime sourceTimeUtc;

    public bool HasChanges() => destinationTimeUtc <= sourceTimeUtc;

    public void Register(ObjectEntry entry)
    {
        var entryTime = entry.LastWriteTimeUtc;

        if (sourceTimeUtc < entryTime)
        {
            sourceTimeUtc = entryTime;
        }
    }
}
