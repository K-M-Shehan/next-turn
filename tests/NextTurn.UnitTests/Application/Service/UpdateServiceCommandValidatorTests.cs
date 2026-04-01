using FluentAssertions;
using NextTurn.Application.Service.Commands.UpdateService;

namespace NextTurn.UnitTests.Application.Service;

public sealed class UpdateServiceCommandValidatorTests
{
    private readonly UpdateServiceCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var command = new UpdateServiceCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Passport Renewal",
            "Renewal processing",
            20);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidDuration_ReturnsError()
    {
        var command = new UpdateServiceCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Passport Renewal",
            "Renewal processing",
            0);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateServiceCommand.EstimatedDurationMinutes));
    }
}
