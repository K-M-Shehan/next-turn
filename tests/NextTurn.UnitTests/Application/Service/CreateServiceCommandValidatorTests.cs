using FluentAssertions;
using NextTurn.Application.Service.Commands.CreateService;

namespace NextTurn.UnitTests.Application.Service;

public sealed class CreateServiceCommandValidatorTests
{
    private readonly CreateServiceCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var command = new CreateServiceCommand(
            Guid.NewGuid(),
            "Passport Renewal",
            "SVC-01",
            "Renewal processing",
            20,
            true);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithMissingCode_ReturnsError()
    {
        var command = new CreateServiceCommand(
            Guid.NewGuid(),
            "Passport Renewal",
            string.Empty,
            "Renewal processing",
            20,
            true);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateServiceCommand.Code));
    }
}
