using TwelveDaily.Core.Domains.Interfaces;

namespace TwelveDaily.Core.Domains.Validation;

public abstract class ValidatableEntity : IValidatable
{
    private readonly List<string> _errors = [];
    
    public IEnumerable<string> Errors => _errors;
    
    public bool IsValid => _errors.Count == 0;

    public void RegisterError(string message)
    {
        _errors.Add(message);
    }

    public abstract void Validate();
}