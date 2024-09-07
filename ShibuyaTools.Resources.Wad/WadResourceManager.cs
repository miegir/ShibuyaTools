using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal class WadResourceManager(ILogger logger, FileSource source)
{
    private static bool HasExtension(WadFile file, string extension)
    {
        return Path.GetExtension(file.Name).Equals(extension, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSns(WadFile file, [NotNullWhen(true)] out Xml? xml)
    {
        if (HasExtension(file, ".sns"))
        {
            xml = Xml.Sns;
            return true;
        }

        if (HasExtension(file, ".sns64"))
        {
            xml = Xml.Sns64;
            return true;
        }

        xml = null;
        return false;
    }

    private static bool IsIvf(WadFile file) => HasExtension(file, ".ivf");

    public void Export(ExportArguments arguments, CancellationToken cancellationToken)
    {
        using var archive = new WadArchive(source);
        Enumerate().Scoped(logger, "file").Run(cancellationToken);

        IEnumerable<Action> Enumerate()
        {
            foreach (var file in archive.Files)
            {
                var path = Path.Combine(arguments.ExportDirectory, file.Name);

                if (IsSns(file, out var xml))
                {
                    logger.LogInformation("exporting sns {name}...", file.Name);
                    using (logger.BeginScope("sns {name}", file.Name))
                    {
                        var bytes = archive.Read(file);
                        SnsEncoder.DecodeBuffer(bytes);
                        var manager = new SnsResourceManager(logger, xml, bytes);
                        manager.Export(arguments with { ExportDirectory = path }, cancellationToken);
                    }
                }
                else
                {
                    if (!arguments.Force && File.Exists(path))
                    {
                        continue;
                    }

                    yield return () =>
                    {
                        logger.LogInformation("exporting file {name}...", file.Name);
                        archive.Export(file, path);
                    };
                }
            }
        }
    }

    public bool Import(ImportArguments arguments, SourceChangeTracker sourceChangeTracker, CancellationToken cancellationToken)
    {
        var hasChanges = arguments.ForceTargets || sourceChangeTracker.HasChanges();

        if (!hasChanges)
        {
            return false;
        }

        using var archive = new WadArchive(source);
        Enumerate().Scoped(logger, "file").Run(cancellationToken);
        archive.Save(logger, cancellationToken);
        return true;

        IEnumerable<Action> Enumerate()
        {
            foreach (var file in archive.Files)
            {
                var sourcePath = ResolvePath(arguments.SourceDirectory, file.Name);

                if (IsSns(file, out var xml))
                {
                    if (Directory.Exists(sourcePath))
                    {
                        yield return () =>
                        {
                            logger.LogInformation("importing sns {name}...", file.Name);
                            using (logger.BeginScope("sns {name}", file.Name))
                            {
                                var objectPath = Path.Combine(arguments.ObjectDirectory, file.Name);
                                var bytes = archive.Read(file);
                                SnsEncoder.DecodeBuffer(bytes);
                                var manager = new SnsResourceManager(logger, xml, bytes);

                                bytes = manager.Import(
                                    arguments with
                                    {
                                        SourceDirectory = sourcePath,
                                        ObjectDirectory = objectPath,
                                    },
                                    sourceChangeTracker,
                                    cancellationToken);

                                SnsEncoder.EncodeBuffer(bytes);
                                archive.AddFile(file.Name, bytes);
                            }
                        };
                    }
                    else
                    {
                        archive.AddFile(file);
                    }
                }
                else
                {
                    if (IsIvf(file))
                    {
                        var jsubtName = Path.ChangeExtension(file.Name, ".jsubt");
                        if (!archive.Exists(jsubtName))
                        {
                            var jsubtPath = Path.Combine(arguments.SourceDirectory, jsubtName);
                            var jsubtInfo = new FileInfo(jsubtPath);
                            sourceChangeTracker.RegisterSource(jsubtPath);
                            if (jsubtInfo.Exists)
                            {
                                yield return () =>
                                {
                                    logger.LogInformation("importing file {name}...", jsubtName);
                                    archive.AddFile(jsubtName, jsubtInfo);
                                };
                            }
                        }

                        sourcePath = Path.ChangeExtension(sourcePath, ".ru.ivf");
                    }

                    var sourceInfo = new FileInfo(sourcePath);

                    sourceChangeTracker.RegisterSource(sourcePath);

                    if (sourceInfo.Exists)
                    {
                        yield return () =>
                        {
                            logger.LogInformation("importing file {name}...", file.Name);
                            archive.AddFile(file.Name, sourceInfo);
                        };
                    }
                    else
                    {
                        archive.AddFile(file);
                    }
                }
            }
        }
    }

    public bool Muster(ObjectPath root, MusterArguments arguments, CancellationToken cancellationToken)
    {
        using var archive = new WadArchive(source);
        return Enumerate().Scoped(logger, "file").Run(cancellationToken);

        IEnumerable<Action> Enumerate()
        {
            foreach (var file in archive.Files)
            {
                var sourcePath = ResolvePath(arguments.SourceDirectory, file.Name);
                var musterPath = root.Append(file.Name);

                if (IsSns(file, out var xml))
                {
                    if (Directory.Exists(sourcePath))
                    {
                        yield return () =>
                        {
                            logger.LogInformation("mustering sns {name}...", file.Name);
                            using (logger.BeginScope("sns {name}", file.Name))
                            {
                                var objectPath = Path.Combine(arguments.ObjectDirectory, file.Name);
                                var bytes = archive.Read(file);
                                SnsEncoder.DecodeBuffer(bytes);
                                var manager = new SnsResourceManager(logger, xml, bytes);

                                if (manager.Muster(
                                    musterPath,
                                    arguments with
                                    {
                                        SourceDirectory = sourcePath,
                                        ObjectDirectory = objectPath,
                                    },
                                    cancellationToken))
                                {
                                    arguments.Sink.ReportDirectory(musterPath);
                                }
                            }
                        };
                    }
                }
                else
                {
                    if (IsIvf(file))
                    {
                        var jsubtName = Path.ChangeExtension(file.Name, ".jsubt");
                        if (!archive.Exists(jsubtName))
                        {
                            var jsubtPath = Path.Combine(arguments.SourceDirectory, jsubtName);
                            if (File.Exists(jsubtPath))
                            {
                                yield return () =>
                                {
                                    logger.LogInformation("mustering file {name}...", jsubtName);
                                    arguments.Sink.ReportObject(root.Append(jsubtName), jsubtPath);
                                };
                            }
                        }

                        sourcePath = Path.ChangeExtension(sourcePath, ".ru.ivf");
                    }

                    if (File.Exists(sourcePath))
                    {
                        yield return () =>
                        {
                            logger.LogInformation("mustering file {name}...", file.Name);
                            arguments.Sink.ReportObject(musterPath, sourcePath);
                        };
                    }
                }
            }
        }
    }

    public void Unpack(UnpackArguments arguments, ObjectPath root, CancellationToken cancellationToken)
    {
        var changeTracker = new WadChangeTracker(source.Destination);
        using var archive = new WadArchive(source);
        var actions = Enumerate().ToList();

        if (arguments.ForceTargets || changeTracker.HasChanges())
        {
            actions.Scoped(logger, "file").Run(cancellationToken);
            archive.Save(logger, cancellationToken);
        }
        else
        {
            logger.LogInformation("skipping unchanged file.");
        }

        IEnumerable<Action> Enumerate()
        {
            foreach (var file in archive.Files)
            {
                var musterPath = root.Append(file.Name);

                if (IsSns(file, out var xml))
                {
                    if (arguments.Container.HasDirectory(musterPath))
                    {
                        logger.LogInformation("unpacking sns {name}...", file.Name);
                        using (logger.BeginScope("sns {name}", file.Name))
                        {
                            var bytes = archive.Read(file);
                            SnsEncoder.DecodeBuffer(bytes);
                            var manager = new SnsResourceManager(logger, xml, bytes);
                            manager.UnpackTest(root, arguments, changeTracker);

                            yield return () =>
                            {
                                logger.LogInformation("unpacking file {name}...", file.Name);
                                var bytes = manager.Unpack(musterPath, arguments, cancellationToken);
                                SnsEncoder.EncodeBuffer(bytes);
                                archive.AddFile(file.Name, bytes);
                            };
                        }
                    }
                    else
                    {
                        archive.AddFile(file);
                    }
                }
                else
                {
                    if (arguments.Container.TryGetEntry(musterPath, out var entry))
                    {
                        changeTracker.Register(entry);

                        yield return () =>
                        {
                            logger.LogInformation("unpacking file {name}...", file.Name);
                            archive.AddFile(file.Name, entry.AsBytes());
                        };
                    }
                    else
                    {
                        archive.AddFile(file);
                    }
                }

                if (IsIvf(file))
                {
                    var jsubtName = Path.ChangeExtension(file.Name, ".jsubt");
                    if (!archive.Exists(jsubtName))
                    {
                        if (arguments.Container.TryGetEntry(root.Append(jsubtName), out var entry))
                        {
                            changeTracker.Register(entry);

                            yield return () =>
                            {
                                logger.LogInformation("unpacking file {name}...", jsubtName);
                                archive.AddFile(jsubtName, entry.AsBytes());
                            };
                        }
                    }
                }
            }
        }
    }

    private static string ResolvePath(string path1, string path2)
    {
        return ResolvePath(Path.Combine(path1, path2));
    }

    private static string ResolvePath(string path)
    {
        var symlinkPath = path + ".symlink";

        if (File.Exists(symlinkPath))
        {
            path = Path.Combine(path, File.ReadAllText(symlinkPath));
        }

        return Path.GetFullPath(path);
    }
}
