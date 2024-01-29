namespace ShibuyaTools.Core;

public record MusterArguments(
    MusterSink Sink,
    string SourceDirectory,
    string ObjectDirectory,
    bool ForceObjects = false);
