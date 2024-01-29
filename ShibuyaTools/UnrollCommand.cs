using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Games;

namespace ShibuyaTools;

[Command("unroll")]
internal class UnrollCommand(ILogger<UnrollCommand> logger)
{
#nullable disable
    [Required]
    [FileExists]
    [Option("-g|--game-path")]
    public string GamePath { get; }
#nullable restore

    public void OnExecute()
    {
        logger.LogInformation("executing...");

        new ShibuyaGame(logger, GamePath)
            .Unroll();

        logger.LogInformation("executed.");
    }
}
