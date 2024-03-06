using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;
using ShibuyaTools.Games;

namespace ShibuyaTools;

[Command("export")]
internal class ExportCommand(ILogger<ExportCommand> logger)
{
#nullable disable
    [Required]
    [FileExists]
    [Option("-g|--game-path")]
    public string GamePath { get; }

    [Required]
    [LegalFilePath]
    [Option("-e|--export-directory")]
    public string ExportDirectory { get; }

    [Option("-b|--backup-directory")]
    public string BackupDirectory { get; }

    [Option("-f|--force")]
    public bool Force { get; }

    [Option("--force-export")]
    public bool ForceExport { get; }
#nullable restore

    public void OnExecute()
    {
        logger.LogInformation("executing...");

        var game = new ShibuyaGame(
            logger: logger,
            gamePath: GamePath,
            backupDirectory: BackupDirectory);

        game.Export(new ExportArguments(
            ExportDirectory: ExportDirectory,
            Force: Force || ForceExport));

        logger.LogInformation("executed.");
    }
}
