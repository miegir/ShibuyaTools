using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;
using ShibuyaTools.Games;

namespace ShibuyaTools;

[Command("import", Description = "Imports source files into the game assets.")]
internal class ImportCommand(ILogger<ImportCommand> logger)
{
#nullable disable
    [Required]
    [FileExists]
    [Option("-g|--game-path", Description = "Game executable path.")]
    public string GamePath { get; }

    [Required]
    [DirectoryExists]
    [Option("-s|--source-directory", Description = "Directory containing source assets.")]
    public string SourceDirectory { get; }

    [Required]
    [LegalFilePath]
    [Option("-j|--object-directory", Description = "Intermediate output directory.")]
    public string ObjectDirectory { get; }

    [Option("-b|--backup-directory", Description = "Game backup directory. Defaults to the game directory.")]
    public string BackupDirectory { get; }

    [Option("-f|--force", Description = "Overwrite unchanged files.")]
    public bool Force { get; }

    [Option("--force-objects", Description = "Overwrite unchanged intermediate files.")]
    public bool ForceObjects { get; }

    [Option("--force-targets", Description = "Overwrite unchanged game files.")]
    public bool ForceTargets { get; }

    [Option("-d|--debug", Description = "Include debug information.")]
    public bool Debug { get; }

    [Option("-l|--launch", Description = "Launch the game after import.")]
    public bool Launch { get; }
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
            game.Import(
                new ImportArguments(
                    SourceDirectory: SourceDirectory,
                    ObjectDirectory: ObjectDirectory,
                    ForceObjects: Force || ForceObjects,
                    ForceTargets: Force || ForceTargets,
                    Debug: Debug),
                cancellationToken);

            logger.LogInformation("executed.");

            if (Launch)
            {
                game.Launch();
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("canceled.");
        }
    }
}
