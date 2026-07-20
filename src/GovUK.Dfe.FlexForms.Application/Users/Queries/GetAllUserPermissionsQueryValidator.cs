using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.FlexForms.Application.Tests")]
namespace GovUK.Dfe.FlexForms.Application.Users.Queries
{
    internal class GetAllUserPermissionsQueryValidator : AbstractValidator<GetAllUserPermissionsQuery>
    {
        public GetAllUserPermissionsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotNull();
        }
    }
}
