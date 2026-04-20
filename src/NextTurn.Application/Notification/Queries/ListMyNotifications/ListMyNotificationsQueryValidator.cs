using FluentValidation;

namespace NextTurn.Application.Notification.Queries.ListMyNotifications;

public sealed class ListMyNotificationsQueryValidator : AbstractValidator<ListMyNotificationsQuery>
{
    public ListMyNotificationsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Take)
            .InclusiveBetween(1, 100).WithMessage("Take must be between 1 and 100.");
    }
}
