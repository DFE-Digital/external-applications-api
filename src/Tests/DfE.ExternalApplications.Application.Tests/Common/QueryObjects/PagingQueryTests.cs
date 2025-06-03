using AutoFixture.Xunit2;
using DfE.ExternalApplications.Application.Common.QueriesObjects;

namespace DfE.ExternalApplications.Application.Tests.Common.QueryObjects
{
    public class PagingQueryTests
    {
        [Theory, AutoData]
        public void Apply_ShouldSkipAndTake_CorrectNumberOfItems(
            int rawPage,
            int rawCount)
        {
            var page = System.Math.Abs(rawPage) % 5;   // 0..4
            var count = (System.Math.Abs(rawCount) % 5) + 1; // 1..5

            // Create a list of 20 integers: 0..19
            var sourceList = Enumerable.Range(0, 20).ToList();
            var queryable = sourceList.AsQueryable();

            var paging = new PagingQuery<int>(page, count);

            // Apply Skip/Take
            var result = paging.Apply(queryable).ToList();

            var expectedSkip = page * count;
            var expectedTake = count;

            var expected = sourceList
                .Skip(expectedSkip)
                .Take(expectedTake)
                .ToList();

            Assert.Equal(expected, result);
        }
    }
}
