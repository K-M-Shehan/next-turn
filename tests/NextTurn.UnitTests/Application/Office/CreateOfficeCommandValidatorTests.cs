using FluentAssertions;
using NextTurn.Application.Office.Commands.CreateOffice;

namespace NextTurn.UnitTests.Application.Office;

public sealed class CreateOfficeCommandValidatorTests
{
    private readonly CreateOfficeCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var command = new CreateOfficeCommand(
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
    public async Task ValidateAsync_WithInvalidLatitude_ReturnsError()
    {
        var command = new CreateOfficeCommand(
            Guid.NewGuid(),
            "Main Branch",
            "123 Main Street",
            140m,
            79.8612m,
            "{\"mon\":\"09:00-17:00\"}");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateOfficeCommand.Latitude));
    }
}
