using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;
using ShibuyaTools.Games;

namespace ShibuyaTools;

[Command("import")]
internal class ImportCommand(ILogger<ImportCommand> logger)
{
#nullable disable
    [Required]
    [FileExists]
    [Option("-g|--game-path")]
    public string GamePath { get; }

    [Required]
    [DirectoryExists]
    [Option("-s|--source-directory")]
    public string SourceDirectory { get; }

    [Required]
    [LegalFilePath]
    [Option("-j|--object-directory")]
    public string ObjectDirectory { get; }

    [Option("-f|--force")]
    public bool Force { get; }

    [Option("--force-objects")]
    public bool ForceObjects { get; }

    [Option("--force-targets")]
    public bool ForceTargets { get; }

    [Option("-d|--debug")]
    public bool Debug { get; }

    [Option("-l|--launch")]
    public bool Launch { get; }
#nullable restore

    public void OnExecute()
    {
        logger.LogInformation("executing...");

        var game = new ShibuyaGame(logger, GamePath);

        game.Import(new ImportArguments(
            SourceDirectory: SourceDirectory,
            ObjectDirectory: ObjectDirectory,
            ForceObjects: Force || ForceObjects,
            ForceTargets: Force || ForceTargets,
            Debug: Debug));

        logger.LogInformation("executed.");

        if (Launch)
        {
            game.Launch();
        }
    }
}
