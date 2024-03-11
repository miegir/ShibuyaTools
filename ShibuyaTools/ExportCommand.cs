using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;
using ShibuyaTools.Games;

namespace ShibuyaTools;

[Command("export", Description = "Exports game assets.")]
internal class ExportCommand(ILogger<ExportCommand> logger)
{
#nullable disable
    [Required]
    [FileExists]
    [Option("-g|--game-path", Description = "Game executable path.")]
    public string GamePath { get; }

    [Required]
    [LegalFilePath]
    [Option("-e|--export-directory", Description = "Directory to place exported assets into.")]
    public string ExportDirectory { get; }

    [Option("-b|--backup-directory", Description = "Game backup directory. Defaults to the game directory.")]
    public string BackupDirectory { get; }

    [Option("-f|--force", Description = "Overwrite existing files.")]
    public bool Force { get; }

    [Option("--force-export", Description = "Overwrite exported assets.")]
    public bool ForceExport { get; }
#nullable restore

    public void OnExecute(CancellationToken cancellationToken)
    {
        logger.LogInformation("executing...");

        var game = new ShibuyaGame(
            logger: logger,
            gamePath: GamePath,
            backupDirectory: BackupDirectory);

        try
        {
            game.Export(
                new ExportArguments(
                    ExportDirectory: ExportDirectory,
                    Force: Force || ForceExport),
                cancellationToken);

            logger.LogInformation("executed.");
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("canceled.");
        }
    }
}
