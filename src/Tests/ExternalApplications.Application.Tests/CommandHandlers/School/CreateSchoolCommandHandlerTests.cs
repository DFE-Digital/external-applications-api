using AutoFixture.Xunit2;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using ExternalApplications.Application.Schools.Commands.CreateSchool;
using ExternalApplications.Domain.Interfaces.Repositories;
using ExternalApplications.Tests.Common.Customizations.Commands;
using ExternalApplications.Tests.Common.Customizations.Entities;
using ExternalApplications.Tests.Common.Customizations.Models;
using NSubstitute;

namespace ExternalApplications.Application.Tests.CommandHandlers.School
{
    public class CreateSchoolCommandHandlerTests
    {
        [Theory]
        [CustomAutoData(
            typeof(SchoolCustomization),
            typeof(PrincipalDetailsCustomization),
            typeof(CreateSchoolCommandCustomization))]
        public async Task Handle_ShouldCreateAndReturnSchoolId_WhenCommandIsValid(
            [Frozen] ISclRepository<Domain.Entities.Schools.School> mockSchoolRepository,
            CreateSchoolCommandHandler handler,
            CreateSchoolCommand command,
            Domain.Entities.Schools.School school)
        {
            // Arrange
            mockSchoolRepository.AddAsync(Arg.Any<Domain.Entities.Schools.School>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(school));

            // Act
            await handler.Handle(command, default);

            // Assert
            await mockSchoolRepository.Received(1).AddAsync(Arg.Is<Domain.Entities.Schools.School>(s => s.SchoolName == command.SchoolName), default);
        }
    }
}
