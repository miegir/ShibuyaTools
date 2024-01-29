using Microsoft.Extensions.Logging;

namespace ShibuyaTools.Core;

public abstract class Game(ILogger logger)
{
    protected readonly ILogger logger = logger;

    public virtual GameVersionInfo? FindVersionInfo() => null;

    public virtual void Launch()
    {
    }

    protected abstract IEnumerable<IResource> EnumerateResources();

    public void Export(ExportArguments arguments) => EnumerateResources()
        .SelectMany(r => r.BeginExport(arguments))
        .Scoped(logger, "resource")
        .Run();

    public void Import(ImportArguments arguments) => EnumerateResources()
        .SelectMany(r => r.BeginImport(arguments))
        .Scoped(logger, "resource")
        .Run();

    public void Muster(MusterArguments arguments) => EnumerateResources()
        .SelectMany(r => r.BeginMuster(arguments))
        .Scoped(logger, "resource")
        .Run();

    public void Unpack(UnpackArguments arguments) => EnumerateResources()
        .SelectMany(r => r.BeginUnpack(arguments))
        .Scoped(logger, "resource")
        .Run();

    public void Unroll() => EnumerateResources()
        .SelectMany(r => r.BeginUnroll())
        .Scoped(logger, "resource")
        .Run();
}
