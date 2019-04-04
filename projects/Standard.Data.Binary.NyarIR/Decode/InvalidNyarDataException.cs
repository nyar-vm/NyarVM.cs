namespace Std.Data.Binary.NyarIR.Decode;

public sealed class InvalidNyarDataException : Exception
{
    public InvalidNyarDataException(string message) : base(message)
    {
    }

    public InvalidNyarDataException(string message, Exception innerException) : base(message, innerException)
    {
    }
}