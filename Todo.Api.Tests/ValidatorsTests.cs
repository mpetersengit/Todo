using System.Linq;
using Todo.Api.Validation;
using Xunit;

namespace Todo.Api.Tests;

public class ValidatorsTests
{
    [Fact]
    public void ValidateTitle_WithValidInput_DoesNotThrow()
    {
        // Arrange
        var title = "  valid title  ";

        // Act
        var exception = Record.Exception(() => Validators.ValidateTitle(title));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTitle_WithBlankInput_Throws(string? title)
    {
        // Arrange
        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() => Validators.ValidateTitle(title));
        Assert.Equal("title", ex.Errors.Keys.Single());
    }

    [Fact]
    public void ValidateTitle_WithTooLongInput_Throws()
    {
        // Arrange
        var title = new string('a', Validators.MaxTitleLength + 1);

        // Act & Assert
        Assert.Throws<ValidationException>(() => Validators.ValidateTitle(title));
    }

    [Fact]
    public void ParseDate_WithValidInput_ReturnsDate()
    {
        // Arrange
        const string input = "2025-12-25";

        // Act
        var result = Validators.ParseDate(input);

        // Assert
        Assert.Equal(new DateOnly(2025, 12, 25), result);
    }

    [Fact]
    public void ParseDate_WithInvalidInput_Throws()
    {
        // Arrange
        const string input = "12/25/2025";

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() => Validators.ParseDate(input));
        Assert.Equal("dueDate", ex.Errors.Keys.Single());
    }

    [Fact]
    public void EnsureDateRange_WithInvalidRange_Throws()
    {
        // Arrange
        var from = new DateOnly(2025, 2, 1);
        var to = new DateOnly(2025, 1, 1);

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() => Validators.EnsureDateRange(from, to));
        Assert.Equal("dateRange", ex.Errors.Keys.Single());
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void ValidatePagination_WithInvalidValues_Throws(int page, int pageSize)
    {
        // Arrange
        // Act & Assert
        Assert.Throws<ValidationException>(() => Validators.ValidatePagination(page, pageSize));
    }

    [Fact]
    public void ValidatePagination_WithValidValues_DoesNotThrow()
    {
        // Arrange
        // Act
        var exception = Record.Exception(() => Validators.ValidatePagination(2, 50));

        // Assert
        Assert.Null(exception);
    }
}

