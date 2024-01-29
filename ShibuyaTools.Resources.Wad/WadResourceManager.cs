using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal class WadResourceManager(ILogger logger, FileSource source)
{
    private static bool HasExtension(WadFile file, string extension)
    {
        return Path.GetExtension(file.Name).Equals(extension, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSns(WadFile file) => HasExtension(file, ".sns");
    private static bool IsIvf(WadFile file) => HasExtension(file, ".ivf");

    public void Export(ExportArguments arguments)
    {
        using var archive = new WadArchive(source);
        Enumerate().Scoped(logger, "file").Run();

        IEnumerable<Action> Enumerate()
        {
            foreach (var file in archive.Files)
            {
                var path = Path.Combine(arguments.ExportDirectory, file.Name);

                if (IsSns(file))
                {
                    logger.LogInformation("exporting sns {name}...", file.Name);
                    using (logger.BeginScope("sns {name}", file.Name))
                    {
                        var bytes = archive.Read(file);
                        SnsEncoder.DecodeBuffer(bytes);
                        var manager = new SnsResourceManager(logger, bytes);
                        manager.Export(arguments with { ExportDirectory = path });
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

    public bool Import(ImportArguments arguments, SourceChangeTracker sourceChangeTracker)
    {
        var hasChanges = arguments.ForceTargets || sourceChangeTracker.HasChanges();

        if (!hasChanges)
        {
            return false;
        }

        using var archive = new WadArchive(source);
        Enumerate().Scoped(logger, "file").Run();
        archive.Save(logger);
        return true;

        IEnumerable<Action> Enumerate()
        {
            foreach (var file in archive.Files)
            {
                var sourcePath = Path.Combine(arguments.SourceDirectory, file.Name);

                if (IsSns(file))
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
                                var manager = new SnsResourceManager(logger, bytes);

                                bytes = manager.Import(
                                    arguments with
                                    {
                                        SourceDirectory = sourcePath,
                                        ObjectDirectory = objectPath,
                                    },
                                    sourceChangeTracker);

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

    public bool Muster(ObjectPath root, MusterArguments arguments)
    {
        using var archive = new WadArchive(source);
        return Enumerate().Scoped(logger, "file").Run();

        IEnumerable<Action> Enumerate()
        {
            foreach (var file in archive.Files)
            {
                var sourcePath = Path.Combine(arguments.SourceDirectory, file.Name);
                var musterPath = root.Append(file.Name);

                if (IsSns(file))
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
                                var manager = new SnsResourceManager(logger, bytes);

                                if (manager.Muster(
                                    musterPath,
                                    arguments with
                                    {
                                        SourceDirectory = sourcePath,
                                        ObjectDirectory = objectPath,
                                    }))
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

    public void Unpack(UnpackArguments arguments, ObjectPath root)
    {
        using var archive = new WadArchive(source);
        Enumerate().Scoped(logger, "file").Run();
        archive.Save(logger);

        IEnumerable<Action> Enumerate()
        {
            foreach (var file in archive.Files)
            {
                var musterPath = root.Append(file.Name);

                if (IsSns(file))
                {
                    if (arguments.Container.HasDirectory(musterPath))
                    {
                        logger.LogInformation("unpacking sns {name}...", file.Name);
                        using (logger.BeginScope("sns {name}", file.Name))
                        {
                            var bytes = archive.Read(file);
                            SnsEncoder.DecodeBuffer(bytes);
                            var manager = new SnsResourceManager(logger, bytes);
                            bytes = manager.Unpack(musterPath, arguments);
                            SnsEncoder.EncodeBuffer(bytes);
                            archive.AddFile(file.Name, bytes);
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
}
