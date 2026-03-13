namespace TheCanonry.Schema.Domain;

public sealed class InvalidDomainValueException : Exception
{
    public InvalidDomainValueException() { }
    public InvalidDomainValueException(string message) : base(message) { }
    public InvalidDomainValueException(string message, Exception innerException) : base(message, innerException) { }
}
