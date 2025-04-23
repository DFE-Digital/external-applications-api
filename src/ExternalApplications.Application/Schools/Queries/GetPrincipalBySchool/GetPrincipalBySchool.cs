using AutoMapper;
using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using ExternalApplications.Application.Common.Models;
using MediatR;
using ExternalApplications.Domain.Interfaces.Repositories;

namespace ExternalApplications.Application.Schools.Queries.GetPrincipalBySchool
{
    public record GetPrincipalBySchoolQuery(string SchoolName) : IRequest<Result<Principal?>>;

    public class GetPrincipalBySchoolQueryHandler(
        ISchoolRepository schoolRepository,
        IMapper mapper,
        ICacheService<IMemoryCacheType> cacheService)
        : IRequestHandler<GetPrincipalBySchoolQuery, Result<Principal?>>
    {
        public async Task<Result<Principal?>> Handle(GetPrincipalBySchoolQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"Principal_{CacheKeyHelper.GenerateHashedCacheKey(request.SchoolName)}";

            var methodName = nameof(GetPrincipalBySchoolQueryHandler);

            return await cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                var principal= await schoolRepository
                    .GetPrincipalBySchoolAsync(request.SchoolName, cancellationToken);

                var result = mapper.Map<Principal?>(principal);

                return result == null ? Result<Principal?>.Failure("") : Result<Principal?>.Success(result);

            }, methodName);
        }
    }
}
