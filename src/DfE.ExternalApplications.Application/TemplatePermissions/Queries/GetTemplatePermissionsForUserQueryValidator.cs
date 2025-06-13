using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.TemplatePermissions.Queries
{
    internal class GetTemplatePermissionsForUserQueryValidator : AbstractValidator<GetTemplatePermissionsForUserQuery>
    {
        public GetTemplatePermissionsForUserQueryValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}