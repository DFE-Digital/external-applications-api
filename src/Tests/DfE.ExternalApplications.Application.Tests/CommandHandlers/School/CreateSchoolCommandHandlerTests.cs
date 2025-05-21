using AutoFixture.Xunit2;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Schools.Commands.CreateSchool;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Tests.Common.Customizations.Commands;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using DfE.ExternalApplications.Tests.Common.Customizations.Models;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.School
{
    public class CreateSchoolCommandHandlerTests
    {
        [Theory]
        [CustomAutoData(
            typeof(SchoolCustomization),
            typeof(PrincipalDetailsCustomization),
            typeof(CreateSchoolCommandCustomization))]
        public async Task Handle_ShouldCreateAndReturnSchoolId_WhenCommandIsValid(
            [Frozen] IEaRepository<Domain.Entities.Schools.School> mockSchoolRepository,
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
