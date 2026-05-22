namespace NetCid;

/// <summary>
/// Thrown when a JSON value cannot be canonicalized per RFC 8785
/// (JSON Canonicalization Scheme).
/// </summary>
public sealed class JcsFormatException : FormatException
{
    public JcsFormatException(string message)
        : base(message)
    {
    }

    public JcsFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
