using FluentAssertions;
using NextTurn.Application.Staff.Commands.UpdateStaff;

namespace NextTurn.UnitTests.Application.Staff;

public sealed class UpdateStaffCommandValidatorTests
{
    private readonly UpdateStaffCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithInvalidShiftWindow_ReturnsError()
    {
        var command = new UpdateStaffCommand(
            Guid.NewGuid(),
            "Counter Agent",
            "0711111111",
            new[] { Guid.NewGuid() },
            "Counter A",
            TimeSpan.Parse("18:00"),
            TimeSpan.Parse("09:00"));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }
}
