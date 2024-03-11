using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

public class WadResource(ILogger logger, FileSource source) : IResource
{
    private readonly string name = source.FileName;

    public IEnumerable<Action> BeginExport(ExportArguments arguments, CancellationToken cancellationToken)
    {
        var path = Path.Combine(arguments.ExportDirectory, name);

        yield return () =>
        {
            logger.LogInformation("exporting {name}...", name);
            using (logger.BeginScope("{name}", name))
            {
                var manager = new WadResourceManager(logger, source);
                manager.Export(arguments with { ExportDirectory = path }, cancellationToken);
            }
        };
    }

    public IEnumerable<Action> BeginImport(ImportArguments arguments, CancellationToken cancellationToken)
    {
        var sourceDirectory = Path.Combine(arguments.SourceDirectory, name);

        return Directory.Exists(sourceDirectory) ? Enumerate() : BeginUnroll(cancellationToken);

        IEnumerable<Action> Enumerate()
        {
            yield return () =>
            {
                logger.LogInformation("importing {name}...", name);
                using (logger.BeginScope("{name}", name))
                {
                    var objectDirectory = Path.Combine(arguments.ObjectDirectory, name);
                    var statePath = objectDirectory + ".importstate";
                    var sourceChangeTracker = new SourceChangeTracker(source.Destination, statePath);
                    var manager = new WadResourceManager(logger, source);

                    var shouldCommit = manager.Import(
                        arguments with
                        {
                            SourceDirectory = sourceDirectory,
                            ObjectDirectory = objectDirectory,
                        },
                        sourceChangeTracker,
                        cancellationToken);

                    if (shouldCommit)
                    {
                        sourceChangeTracker.Commit();
                    }
                }
            };
        }
    }

    public IEnumerable<Action> BeginMuster(MusterArguments arguments, CancellationToken cancellationToken)
    {
        var sourceDirectory = Path.Combine(arguments.SourceDirectory, name);

        if (!Directory.Exists(sourceDirectory))
        {
            yield break;
        }

        yield return () =>
        {
            logger.LogInformation("mustering {name}...", name);
            using (logger.BeginScope("{name}", name))
            {
                var objectDirectory = Path.Combine(arguments.ObjectDirectory, name);
                var directoryName = ObjectPath.Root.Append(name);
                var manager = new WadResourceManager(logger, source);

                if (manager.Muster(
                    directoryName,
                    arguments with
                    {
                        SourceDirectory = sourceDirectory,
                        ObjectDirectory = objectDirectory,
                    },
                    cancellationToken))
                {
                    arguments.Sink.ReportDirectory(directoryName);
                }
            }
        };
    }

    public IEnumerable<Action> BeginUnpack(UnpackArguments arguments, CancellationToken cancellationToken)
    {
        var directory = ObjectPath.Root.Append(name);

        return arguments.Container.HasDirectory(directory) ? Enumerate() : BeginUnroll(cancellationToken);

        IEnumerable<Action> Enumerate()
        {
            yield return () =>
            {
                logger.LogInformation("unpacking {name}...", name);
                using (logger.BeginScope("{name}", name))
                {
                    var manager = new WadResourceManager(logger, source);
                    manager.Unpack(arguments, directory, cancellationToken);
                }
            };
        }
    }

    public IEnumerable<Action> BeginUnroll(CancellationToken cancellationToken)
    {
        if (source.CanUnroll())
        {
            yield return () =>
            {
                logger.LogInformation("unrolling {name}...", name);
                using (logger.BeginScope("unrolling {name}", name))
                {
                    var progressReporter = new ProgressReporter(logger);
                    source.Unroll(progressReporter.ReportProgress, cancellationToken);
                }
            };
        }
    }
}
