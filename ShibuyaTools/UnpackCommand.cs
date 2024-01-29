using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;
using ShibuyaTools.Games;
using System.ComponentModel.DataAnnotations;

namespace ShibuyaTools;

[Command("unpack")]
internal class UnpackCommand(ILogger<UnpackCommand> logger)
{
#nullable disable
    [Required]
    [FileExists]
    [Option("-g|--game-path")]
    public string GamePath { get; }

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
        var game = new ShibuyaGame(logger, GamePath);

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
