namespace Fallout.Tools.Core.AAF;

public sealed class AafException : Exception
{
    public AafException(string message) : base(message)
    {
    }

    public AafException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
