namespace ShibuyaTools.Core;

public record ImportArguments(
    string SourceDirectory,
    string ObjectDirectory,
    bool ForceObjects = false,
    bool ForceTargets = false,
    bool Debug = false);
