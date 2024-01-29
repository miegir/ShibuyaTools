namespace ShibuyaTools.Core;

internal class InvalidMagickException : Exception
{
    public InvalidMagickException()
    {
    }

    public InvalidMagickException(string? message) : base(message)
    {
    }

    public InvalidMagickException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
