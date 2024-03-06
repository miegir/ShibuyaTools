using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;
using ShibuyaTools.Games;

namespace ShibuyaTools;

[Command("create")]
internal class CreateCommand(ILogger<CreateCommand> logger)
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

    [Option("-b|--backup-directory")]
    public string BackupDirectory { get; }

    [Required]
    [LegalFilePath]
    [Option("-a|--archive-path")]
    public string ArchivePath { get; }

    [Option("-f|--force")]
    public bool Force { get; }

    [Option("--force-objects")]
    public bool ForceObjects { get; }

    [Option("--force-pack")]
    public bool ForcePack { get; }
#nullable restore

    public void OnExecute()
    {
        logger.LogInformation("executing...");

        var sink = new MusterSink(logger);

        var game = new ShibuyaGame(
            logger: logger,
            gamePath: GamePath,
            backupDirectory: BackupDirectory);

        game.Muster(new MusterArguments(
            Sink: sink,
            SourceDirectory: SourceDirectory,
            ObjectDirectory: ObjectDirectory,
            ForceObjects: Force || ForceObjects));

        sink.Pack(new PackArguments(
            ArchivePath: ArchivePath,
            Force: Force || ForcePack));

        logger.LogInformation("executed.");
    }
}
