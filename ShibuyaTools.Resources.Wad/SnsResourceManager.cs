using System.Text.Json;
using Microsoft.Extensions.Logging;
using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal class SnsResourceManager(ILogger logger, byte[] bytes)
{
    public void Export(ExportArguments arguments)
    {
        Enumerate().Scoped(logger, "file").Run();

        IEnumerable<Action> Enumerate()
        {
            using var archive = new SnsArchive(bytes);

            foreach (var file in archive.Files)
            {
                var (name, bytes) = archive.Read(file);
                var xmlPath = Path.Combine(arguments.ExportDirectory, name);

                var binPath = xmlPath + ".bin";
                if (arguments.Force || !File.Exists(binPath))
                {
                    yield return () =>
                    {
                        logger.LogInformation("exporting bin {name}...", name);
                        using var target = new FileTarget(binPath);
                        target.Stream.Write(bytes);
                        target.Commit();
                    };
                }

                var txtPath = xmlPath + ".txt";
                if (arguments.Force || !File.Exists(txtPath))
                {
                    yield return () =>
                    {
                        logger.LogInformation("exporting txt {name}...", name);
                        var translations = Xml.Parse(bytes);
                        using var target = new FileTarget(txtPath);
                        JsonSerializer.Serialize(target.Stream, translations, XmlContext.Relaxed.XmlTranslationArray);
                        target.Commit();
                    };
                }
            }
        }
    }

    public byte[] Import(ImportArguments arguments, SourceChangeTracker sourceChangeTracker)
    {
        using var archive = new SnsArchive(bytes);
        Enumerate().Scoped(logger, "xml").Run();
        return archive.Save();

        IEnumerable<Action> Enumerate()
        {
            foreach (var file in archive.Files)
            {
                var (name, bytes) = archive.Read(file);
                var jsonName = name + ".txt";
                var jsonPath = Path.Combine(arguments.SourceDirectory, jsonName);

                sourceChangeTracker.RegisterSource(jsonPath);

                if (File.Exists(jsonPath))
                {
                    yield return () =>
                    {
                        logger.LogInformation("translating {name}...", name);
                        using (logger.BeginScope("{name}", name))
                        {
                            using var stream = File.OpenRead(jsonPath);

                            var translations = JsonSerializer.Deserialize(stream, XmlContext.Relaxed.XmlTranslationArray);
                            if (translations != null && translations.Length > 0)
                            {
                                bytes = Xml.Translate(logger, translations, bytes).ToArray();
                            }

                            archive.AddFile(name, bytes);
                        }
                    };
                }
                else
                {
                    yield return () =>
                    {
                        logger.LogInformation("emitting {name}...", name);
                        archive.AddFile(name, bytes);
                    };
                }
            }
        }
    }

    public bool Muster(ObjectPath root, MusterArguments arguments)
    {
        using var archive = new SnsArchive(bytes);
        return Enumerate().Scoped(logger, "xml").Run();

        IEnumerable<Action> Enumerate()
        {
            foreach (var file in archive.Files)
            {
                var (name, bytes) = archive.Read(file);
                var jsonName = name + ".txt";
                var jsonPath = Path.Combine(arguments.SourceDirectory, jsonName);
                var jsonInfo = new FileInfo(jsonPath);

                yield return () =>
                {
                    logger.LogInformation("mustering {name}...", name);
                    arguments.Sink.ReportObject(root.Append(name), new XmlTranslationSource(jsonInfo));
                };
            }
        }
    }

    internal byte[] Unpack(ObjectPath root, UnpackArguments arguments)
    {
        using var archive = new SnsArchive(bytes);
        Enumerate().Scoped(logger, "xml").Run();
        return archive.Save();

        IEnumerable<Action> Enumerate()
        {
            foreach (var file in archive.Files)
            {
                var (name, bytes) = archive.Read(file);
                if (arguments.Container.TryGetEntry(root.Append(name), out var entry))
                {
                    yield return () =>
                    {
                        logger.LogInformation("translating {name}...", name);
                        using (logger.BeginScope("{name}", name))
                        {
                            var translations = entry.AsObjectSource<XmlTranslation[]>().Deserialize();

                            if (translations.Length > 0)
                            {
                                bytes = Xml.Translate(logger, translations, bytes).ToArray();
                            }

                            archive.AddFile(name, bytes);
                        }
                    };
                }
                else
                {
                    yield return () =>
                    {
                        logger.LogInformation("emitting {name}...", name);
                        archive.AddFile(name, bytes);
                    };
                }
            }
        }
    }
}
