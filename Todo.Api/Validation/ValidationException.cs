namespace Todo.Api.Validation;

public class ValidationException : Exception
{
    public ValidationException(string field, string message)
        : this(new Dictionary<string, string[]>
        {
            [field] = new[] { message }
        })
    {
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

