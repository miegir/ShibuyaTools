using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Games;

namespace ShibuyaTools;

[Command("unroll", Description = "Restores game files from a backup.")]
internal class UnrollCommand(ILogger<UnrollCommand> logger)
{
#nullable disable
    [Required]
    [FileExists]
    [Option("-g|--game-path", Description = "Game executable path.")]
    public string GamePath { get; }

    [Option("-b|--backup-directory", Description = "Game backup directory. Defaults to the game directory.")]
    public string BackupDirectory { get; }
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
            game.Unroll(cancellationToken);

            logger.LogInformation("executed.");
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("canceled.");
        }
    }
}
