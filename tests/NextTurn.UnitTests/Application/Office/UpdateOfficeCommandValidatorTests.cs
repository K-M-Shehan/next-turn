using FluentAssertions;
using NextTurn.Application.Office.Commands.UpdateOffice;

namespace NextTurn.UnitTests.Application.Office;

public sealed class UpdateOfficeCommandValidatorTests
{
    private readonly UpdateOfficeCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var command = new UpdateOfficeCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Main Branch",
            "123 Main Street",
            6.9271m,
            79.8612m,
            "{\"mon\":\"09:00-17:00\"}");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyName_ReturnsError()
    {
        var command = new UpdateOfficeCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            string.Empty,
            "123 Main Street",
            null,
            null,
            "{\"mon\":\"09:00-17:00\"}");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateOfficeCommand.Name));
    }
}
