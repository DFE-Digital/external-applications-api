using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Users
{
    public class GetUserByEmailQueryObjectTests
    {
        [Theory, CustomAutoData(typeof(UserCustomization))]
        public void Apply_ShouldReturnOnlyUsersWithMatchingEmail_IgnoringCase(
            string rawEmail,
            [Frozen] IList<User> userList,
            UserCustomization customization)
        {
            var normalized = rawEmail.Trim().ToLowerInvariant();

            customization.OverrideEmail = rawEmail;
            var fixtureMatching = new Fixture().Customize(customization);
            var matchingUser = fixtureMatching.Create<User>();

            // Two additional random users (email doesn’t matter)
            var otherFixture = new Fixture().Customize(new UserCustomization());
            var other1 = otherFixture.Create<User>();
            var other2 = otherFixture.Create<User>();

            // Populate the frozen list
            userList.Clear();
            userList.Add(matchingUser);
            userList.Add(other1);
            userList.Add(other2);

            var sut = new GetUserByEmailQueryObject(rawEmail);

            var result = sut.Apply(userList.AsQueryable()).ToList();

            Assert.Single(result);
            Assert.Equal(normalized, result[0].Email.ToLowerInvariant());
        }
    }
}
