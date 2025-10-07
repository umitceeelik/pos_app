using FluentValidation;
using Server.Application.Sessions;

namespace Server.Application.Sessions;

/// <summary>
/// FluentValidation validators for Session-related DTOs.
/// These run before the endpoint logic via a minimal-API filter.
/// </summary>
public class OpenSessionRequestValidator : AbstractValidator<OpenSessionRequest>
{
    public OpenSessionRequestValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("RoomId is required.");

        RuleFor(x => x.CustomerName)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerName));
    }
}

public class AddItemRequestValidator : AbstractValidator<AddItemRequest>
{
    public AddItemRequestValidator()
    {
        RuleFor(x => x.ServiceName)
            .NotEmpty().WithMessage("ServiceName is required.")
            .MaximumLength(64);

        RuleFor(x => x.Qty)
            .GreaterThan(0).WithMessage("Qty must be > 0.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("UnitPrice must be >= 0.");
    }
}

public class AddPaymentRequestValidator : AbstractValidator<AddPaymentRequest>
{
    public AddPaymentRequestValidator()
    {
        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Method is required.")
            .MaximumLength(8);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be > 0.");
    }
}
