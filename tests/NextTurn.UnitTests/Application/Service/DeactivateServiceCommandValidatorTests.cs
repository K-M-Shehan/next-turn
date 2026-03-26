using FluentAssertions;
using NextTurn.Application.Service.Commands.DeactivateService;

namespace NextTurn.UnitTests.Application.Service;

public sealed class DeactivateServiceCommandValidatorTests
{
    private readonly DeactivateServiceCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var command = new DeactivateServiceCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyServiceId_ReturnsError()
    {
        var command = new DeactivateServiceCommand(Guid.NewGuid(), Guid.Empty);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(DeactivateServiceCommand.ServiceId));
    }
}
