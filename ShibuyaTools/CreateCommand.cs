using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;
using ShibuyaTools.Games;

namespace ShibuyaTools;

[Command("create", Description = "Creates asset .zip bundle.")]
internal class CreateCommand(ILogger<CreateCommand> logger)
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

    [Required]
    [LegalFilePath]
    [Option("-a|--archive-path", Description = "Path to the created asset bundle.")]
    public string ArchivePath { get; }

    [Option("-f|--force", Description = "Overwrite unchanged files.")]
    public bool Force { get; }

    [Option("--force-objects", Description = "Overwrite unchanged intermediate files.")]
    public bool ForceObjects { get; }

    [Option("--force-pack", Description = "Overwrite unchanged bundle.")]
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
