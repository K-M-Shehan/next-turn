using FluentValidation;

namespace NextTurn.Application.Staff.Commands.UpdateStaff;

public sealed class UpdateStaffCommandValidator : AbstractValidator<UpdateStaffCommand>
{
    public UpdateStaffCommandValidator()
    {
        RuleFor(x => x.StaffUserId)
            .NotEmpty().WithMessage("Staff user ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.OfficeIds)
            .NotNull().WithMessage("Office assignments are required.");

        RuleForEach(x => x.OfficeIds)
            .NotEmpty().WithMessage("Office ID cannot be empty.");

        RuleFor(x => x.CounterName)
            .MaximumLength(80).WithMessage("Counter name must not exceed 80 characters.");

        RuleFor(x => x)
            .Must(x => x.ShiftStart.HasValue == x.ShiftEnd.HasValue)
            .WithMessage("Shift start and shift end must both be provided.")
            .Must(x => !x.ShiftStart.HasValue || !x.ShiftEnd.HasValue || x.ShiftStart.Value < x.ShiftEnd.Value)
            .WithMessage("Shift end must be after shift start.");
    }
}
