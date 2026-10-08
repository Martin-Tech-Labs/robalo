using FluentValidation;
using Robalo.Controller.Api.Requests;

namespace Robalo.Controller.Api.Validation.Requests;

public class MessageApiRequestValidator : AbstractValidator<MessageApiRequest>
{
    public MessageApiRequestValidator()
    {
        RuleFor(x => x.Content)
        .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("content_must_be_provided")
            .MinimumLength(1).WithMessage("content_must_be_provided")
            .MaximumLength(100).WithMessage("content_must_not_exceed_100_characters");
    }
}