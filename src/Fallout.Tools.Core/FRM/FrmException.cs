using System;

namespace Fallout.Tools.Core.FRM;

public sealed class FrmException : Exception
{
    public FrmException(string message) : base(message)
    {
    }

    public FrmException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
