using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Users.Queries
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
