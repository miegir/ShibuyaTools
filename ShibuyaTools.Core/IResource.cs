namespace ShibuyaTools.Core;

public interface IResource
{
    IEnumerable<Action> BeginExport(ExportArguments arguments);
    IEnumerable<Action> BeginImport(ImportArguments arguments);
    IEnumerable<Action> BeginMuster(MusterArguments arguments);
    IEnumerable<Action> BeginUnpack(UnpackArguments arguments);
    IEnumerable<Action> BeginUnroll();
}
