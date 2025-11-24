using System.Globalization;

namespace Todo.Api.Validation;

public static class Validators
{
    public const int MaxTitleLength = 200;

    public static void ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("title", "Title is required.");

        var trimmed = title.Trim();
        if (trimmed.Length is < 1 or > MaxTitleLength)
        {
            throw new ValidationException(
                "title",
                $"Title must be between 1 and {MaxTitleLength} characters.");
        }
    }

    public static DateOnly? ParseDate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        if (DateOnly.TryParseExact(
                input.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        throw new ValidationException("dueDate", "DueDate must be in YYYY-MM-DD format.");
    }

    public static void EnsureDateRange(DateOnly? from, DateOnly? to)
    {
        if (from is not null && to is not null && from > to)
        {
            throw new ValidationException("dateRange", "dueAfter must be earlier than or equal to dueBefore.");
        }
    }

    public static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ValidationException("page", "Page must be greater than or equal to 1.");
        }

        if (pageSize < 1)
        {
            throw new ValidationException("pageSize", "PageSize must be greater than or equal to 1.");
        }

        if (pageSize > 100)
        {
            throw new ValidationException("pageSize", "PageSize must be less than or equal to 100.");
        }
    }
}
