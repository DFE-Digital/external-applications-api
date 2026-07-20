using FluentValidation;

namespace GovUK.Dfe.FlexForms.Application.Users.Queries
{
    public class ExchangeTokenQueryValidator : AbstractValidator<ExchangeTokenQuery>
    {
        public ExchangeTokenQueryValidator()
        {
            RuleFor(x => x.SubjectToken).NotEmpty().WithMessage("Subject token must be provided");
        }
    }
}
