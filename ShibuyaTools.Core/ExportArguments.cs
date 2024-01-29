namespace ShibuyaTools.Core;

public record ExportArguments(
    string ExportDirectory,
    bool Force = false);
