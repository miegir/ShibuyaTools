using System.ComponentModel.DataAnnotations;
using System.Runtime.Loader;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;
using ShibuyaTools.Games;

namespace ShibuyaTools;

[Command("unpack", Description = "Unpacks asset bundle into the game assets.")]
internal class UnpackCommand(ILogger<UnpackCommand> logger)
{
#nullable disable
    [Required]
    [FileExists]
    [Option("-g|--game-path", Description = "Game executable path.")]
    public string GamePath { get; }

    [Option("-b|--backup-directory", Description = "Game backup directory. Defaults to the game directory.")]
    public string BackupDirectory { get; }

    [Required]
    [FileExists]
    [Option("-a|--archive-path", Description = "Path to the asset bundle.")]
    public string ArchivePath { get; }

    [Option("-m|--manifest-resource-name", Description = "Manifest resource name inside the bundle (when the bundle is an .exe or .dll). Do not specify a value for .zip bundles.")]
    public string ManifestResourceName { get; }

    [Option("-f|--force", Description = "Overwrite unchanged files.")]
    public bool Force { get; }

    [Option("--force-targets", Description = "Overwrite unchanged game files.")]
    public bool ForceTargets { get; }

    [Option("-d|--debug", Description = "Include debug information.")]
    public bool Debug { get; }

    [Option("-l|--launch", Description = "Launch the game after unpacking.")]
    public bool Launch { get; }
#nullable restore

    public void OnExecute(CancellationToken cancellationToken)
    {
        logger.LogInformation("executing...");

        using var stream = OpenStream();
        using var container = new ObjectContainer(stream);

        var game = new ShibuyaGame(
            logger: logger,
            gamePath: GamePath,
            backupDirectory: BackupDirectory);

        try
        {
            game.Unpack(
                new UnpackArguments(
                    Container: container,
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

    private Stream OpenStream() => string.IsNullOrEmpty(ManifestResourceName)
        ? File.OpenRead(ArchivePath)
        : BundleHelper.OpenBundle(
            ArchivePath,
            "ShibuyaTools.UI.dll",
            ManifestResourceName) ?? OpenAssembly();

    private Stream OpenAssembly()
    {
        var context = new AssemblyLoadContext(ArchivePath);
        var assemblyPath = Path.GetFullPath(ArchivePath);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);
        var stream = assembly.GetManifestResourceStream(ManifestResourceName);

        return stream ?? throw new FileNotFoundException(
            message: $"Manifest resource '{ManifestResourceName}'" +
            $" not found in the assembly '{ArchivePath}'.",
            fileName: ManifestResourceName);
    }
}
