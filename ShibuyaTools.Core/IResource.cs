namespace ShibuyaTools.Core;

public interface IResource
{
    IEnumerable<Action> BeginExport(ExportArguments arguments, CancellationToken cancellationToken);
    IEnumerable<Action> BeginImport(ImportArguments arguments, CancellationToken cancellationToken);
    IEnumerable<Action> BeginMuster(MusterArguments arguments, CancellationToken cancellationToken);
    IEnumerable<Action> BeginUnpack(UnpackArguments arguments, CancellationToken cancellationToken);
    IEnumerable<Action> BeginUnroll(CancellationToken cancellationToken);
}
