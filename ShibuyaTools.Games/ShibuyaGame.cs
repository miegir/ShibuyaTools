using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;
using ShibuyaTools.Resources.Wad;

namespace ShibuyaTools.Games;

public class ShibuyaGame(ILogger logger, string gamePath, string? backupDirectory = null) : Game(logger)
{
    public override GameVersionInfo? FindVersionInfo()
    {
        if (File.Exists(gamePath))
        {
            try
            {
                return new(gamePath);
            }
            catch (IOException)
            {
            }
        }

        return null;
    }

    public override void Launch()
    {
        var startInfo = new ProcessStartInfo(gamePath)
        {
            WorkingDirectory = Path.GetDirectoryName(gamePath),
        };

        Process.Start(startInfo)?.Dispose();
    }

    protected override IEnumerable<IResource> EnumerateResources()
    {
        yield return new GameVersionResource(logger, this);

        var gameDir = Path.GetDirectoryName(gamePath);
        if (gameDir is not null)
        {
            var backupDir = backupDirectory ?? gameDir;

            foreach (var path in Directory.EnumerateFiles(gameDir, "*.wad"))
            {
                var backupName = Path.GetFileName(path) + ".bak";
                var backupPath = Path.Combine(backupDir, backupName);
                var source = new FileSource(path, backupPath);
                yield return new WadResource(logger, source);
            }
        }
    }
}
