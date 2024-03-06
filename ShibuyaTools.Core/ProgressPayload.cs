namespace ShibuyaTools.Core;

public readonly record struct ProgressPayload<T>(T Total, T Position);
