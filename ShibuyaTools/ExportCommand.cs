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

    [Option("-f|--force")]
    public bool Force { get; }

    [Option("--force-export")]
    public bool ForceExport { get; }
#nullable restore

    public void OnExecute()
    {
        logger.LogInformation("executing...");

        new ShibuyaGame(logger, GamePath)
            .Export(new ExportArguments(ExportDirectory, Force: Force || ForceExport));

        logger.LogInformation("executed.");
    }
}
