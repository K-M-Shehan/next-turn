using FluentValidation;

namespace NextTurn.Application.Staff.Commands.DeactivateStaff;

public sealed class DeactivateStaffCommandValidator : AbstractValidator<DeactivateStaffCommand>
{
    public DeactivateStaffCommandValidator()
    {
        RuleFor(x => x.StaffUserId)
            .NotEmpty().WithMessage("Staff user ID is required.");
    }
}
