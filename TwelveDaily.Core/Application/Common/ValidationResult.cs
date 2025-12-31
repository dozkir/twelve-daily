namespace TwelveDaily.Core.Application.Common;

public record ValidationResult(IReadOnlyCollection<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}