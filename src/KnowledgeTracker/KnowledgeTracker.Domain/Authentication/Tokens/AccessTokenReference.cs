namespace KnowledgeTracker.Domain.Authentication;

public sealed record AccessTokenReference
{
    public AccessTokenReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Access token value is required.", nameof(value));

        Value = value;
    }

    public string Value { get; }
}