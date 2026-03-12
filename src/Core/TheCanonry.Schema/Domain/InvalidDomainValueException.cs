namespace TheCanonry.Schema.Domain;

public class InvalidDomainValueException : Exception
{
    public InvalidDomainValueException(string message) : base(message) { }
}
