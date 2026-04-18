using FluentValidation;

namespace NextTurn.Application.Auth.Commands.UpdateAppointmentNotificationPreferences;

public sealed class UpdateAppointmentNotificationPreferencesCommandValidator
    : AbstractValidator<UpdateAppointmentNotificationPreferencesCommand>
{
    public UpdateAppointmentNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
