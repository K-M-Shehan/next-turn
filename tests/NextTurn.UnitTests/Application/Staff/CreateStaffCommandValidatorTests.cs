using FluentAssertions;
using NextTurn.Application.Staff.Commands.CreateStaff;

namespace NextTurn.UnitTests.Application.Staff;

public sealed class CreateStaffCommandValidatorTests
{
    private readonly CreateStaffCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var command = new CreateStaffCommand(
            "Counter Agent",
            "staff@example.com",
            "0711111111",
            new[] { Guid.NewGuid() },
            "Counter A",
            TimeSpan.Parse("09:00"),
            TimeSpan.Parse("17:00"));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithoutOffices_ReturnsError()
    {
        var command = new CreateStaffCommand(
            "Counter Agent",
            "staff@example.com",
            null,
            Array.Empty<Guid>(),
            null,
            null,
            null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateStaffCommand.OfficeIds));
    }
}
