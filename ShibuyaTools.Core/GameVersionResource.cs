using Microsoft.Extensions.Logging;

namespace ShibuyaTools.Core;

public class GameVersionResource(ILogger logger, Game game) : IResource
{
    public IEnumerable<Action> BeginExport(ExportArguments arguments)
    {
        return Enumerable.Empty<Action>();
    }

    public IEnumerable<Action> BeginImport(ImportArguments arguments)
    {
        return Enumerable.Empty<Action>();
    }

    public IEnumerable<Action> BeginMuster(MusterArguments arguments)
    {
        yield return () =>
        {
            logger.LogInformation("mustering game version...");

            var versionInfo = game.FindVersionInfo();
            if (versionInfo is null)
            {
                logger.LogError("game version info not found.");
                return;
            }

            arguments.Sink.ReportObject(
                GameVersionStatics.Path,
                new GameVersionStreamSource(versionInfo));
        };
    }

    public IEnumerable<Action> BeginUnpack(UnpackArguments arguments)
    {
        return Enumerable.Empty<Action>();
    }

    public IEnumerable<Action> BeginUnroll()
    {
        return Enumerable.Empty<Action>();
    }

    private class GameVersionStreamSource(GameVersionInfo info) : IObjectStreamSource
    {
        public bool Exists => true;
        public DateTime LastWriteTimeUtc => info.LastWriteTimeUtc;
        public Stream OpenRead() => ObjectSerializer.SerializeToStream(info.GameVersion);
    }
}
