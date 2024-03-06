using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;
using ShibuyaTools.Games;

namespace ShibuyaTools;

[Command("unpack")]
internal class UnpackCommand(ILogger<UnpackCommand> logger)
{
#nullable disable
    [Required]
    [FileExists]
    [Option("-g|--game-path")]
    public string GamePath { get; }

    [Option("-b|--backup-directory")]
    public string BackupDirectory { get; }

    [Required]
    [FileExists]
    [Option("-a|--archive-path")]
    public string ArchivePath { get; }

    [Option("-d|--debug")]
    public bool Debug { get; }

    [Option("-l|--launch")]
    public bool Launch { get; }
#nullable restore

    public void OnExecute()
    {
        logger.LogInformation("executing...");

        using var stream = File.OpenRead(ArchivePath);
        using var container = new ObjectContainer(stream);

        var game = new ShibuyaGame(
            logger: logger,
            gamePath: GamePath,
            backupDirectory: BackupDirectory);

        game.Unpack(new UnpackArguments(
            Container: container,
            Debug: Debug));

        logger.LogInformation("executed.");

        if (Launch)
        {
            game.Launch();
        }
    }
}
