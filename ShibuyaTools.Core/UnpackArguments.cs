namespace ShibuyaTools.Core;

public record UnpackArguments(
    ObjectContainer Container,
    bool ForceTargets = false,
    bool Debug = false);
