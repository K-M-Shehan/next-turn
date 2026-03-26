using FluentAssertions;
using NextTurn.Application.Service.Commands.RemoveServiceOfficeAssignment;

namespace NextTurn.UnitTests.Application.Service;

public sealed class RemoveServiceOfficeAssignmentCommandValidatorTests
{
    private readonly RemoveServiceOfficeAssignmentCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var command = new RemoveServiceOfficeAssignmentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyOfficeId_ReturnsError()
    {
        var command = new RemoveServiceOfficeAssignmentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(RemoveServiceOfficeAssignmentCommand.OfficeId));
    }
}
