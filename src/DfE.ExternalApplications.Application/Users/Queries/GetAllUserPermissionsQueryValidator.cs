using FluentValidation;

namespace DfE.ExternalApplications.Application.Users.Queries
{
    internal class GetAllUserPermissionsQueryValidator : AbstractValidator<GetAllUserPermissionsQuery>
    {
        public GetAllUserPermissionsQueryValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}
