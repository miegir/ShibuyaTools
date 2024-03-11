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

    public void Export(ExportArguments arguments, CancellationToken cancellationToken = default) => EnumerateResources()
        .SelectMany(r => r.BeginExport(arguments, cancellationToken))
        .Scoped(logger, "resource")
        .Run(cancellationToken);

    public void Import(ImportArguments arguments, CancellationToken cancellationToken = default) => EnumerateResources()
        .SelectMany(r => r.BeginImport(arguments, cancellationToken))
        .Scoped(logger, "resource")
        .Run(cancellationToken);

    public void Muster(MusterArguments arguments, CancellationToken cancellationToken = default) => EnumerateResources()
        .SelectMany(r => r.BeginMuster(arguments, cancellationToken))
        .Scoped(logger, "resource")
        .Run(cancellationToken);

    public void Unpack(UnpackArguments arguments, CancellationToken cancellationToken = default) => EnumerateResources()
        .SelectMany(r => r.BeginUnpack(arguments, cancellationToken))
        .Scoped(logger, "resource")
        .Run(cancellationToken);

    public void Unroll(CancellationToken cancellationToken = default) => EnumerateResources()
        .SelectMany(r => r.BeginUnroll(cancellationToken))
        .Scoped(logger, "resource")
        .Run(cancellationToken);
}
