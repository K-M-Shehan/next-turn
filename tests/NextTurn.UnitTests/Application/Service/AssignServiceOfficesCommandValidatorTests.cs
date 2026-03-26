using FluentAssertions;
using NextTurn.Application.Service.Commands.AssignServiceOffices;

namespace NextTurn.UnitTests.Application.Service;

public sealed class AssignServiceOfficesCommandValidatorTests
{
    private readonly AssignServiceOfficesCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var command = new AssignServiceOfficesCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { Guid.NewGuid() });

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithNoOffices_ReturnsError()
    {
        var command = new AssignServiceOfficesCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<Guid>());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(AssignServiceOfficesCommand.OfficeIds));
    }
}
