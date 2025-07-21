using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using FluentValidation;
using MediatR;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public class GetUploadsForApplicationQueryValidator : AbstractValidator<GetUploadsForApplicationQuery>
{
    public GetUploadsForApplicationQueryValidator()
    {
        RuleFor(x => x.ApplicationId).NotNull();
    }
}
