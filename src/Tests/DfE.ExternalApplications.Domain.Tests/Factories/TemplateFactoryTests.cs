using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.ValueObjects;
using Xunit;

namespace DfE.ExternalApplications.Domain.Tests.Factories
{
    public class TemplateFactoryTests
    {
        private readonly TemplateFactory _factory = new();
        private readonly Template _template;
        private readonly UserId _userId = new(Guid.NewGuid());

        public TemplateFactoryTests()
        {
            _template = new Template(new TemplateId(Guid.NewGuid()), "Test Template", DateTime.UtcNow, _userId);
        }

        [Fact]
        public void AddVersionToTemplate_ShouldAddNewVersion_WhenValid()
        {
            // Arrange
            var versionNumber = "1.0.0";
            var jsonSchema = "{'prop':'value'}";
            
            // Act
            var result = _factory.AddVersionToTemplate(_template, versionNumber, jsonSchema, _userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(versionNumber, result.VersionNumber);
            Assert.Equal(jsonSchema, result.JsonSchema);
            Assert.Equal(_userId, result.CreatedBy);
            Assert.Single(_template.TemplateVersions);
            Assert.Contains(result, _template.TemplateVersions);
        }

        [Fact]
        public void AddVersionToTemplate_ShouldThrowArgumentNullException_WhenTemplateIsNull()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                _factory.AddVersionToTemplate(null!, "1.0.0", "schema", _userId));
            Assert.Equal("template", ex.ParamName);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void AddVersionToTemplate_ShouldThrowArgumentException_WhenVersionNumberIsInvalid(string versionNumber)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => 
                _factory.AddVersionToTemplate(_template, versionNumber, "schema", _userId));
            Assert.Equal("versionNumber", ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void AddVersionToTemplate_ShouldThrowArgumentException_WhenJsonSchemaIsInvalid(string jsonSchema)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _factory.AddVersionToTemplate(_template, "1.0.0", jsonSchema, _userId));
            Assert.Equal("jsonSchema", ex.ParamName);
        }
        
        [Fact]
        public void AddVersionToTemplate_ShouldThrowArgumentNullException_WhenCreatedByIsNull()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                _factory.AddVersionToTemplate(_template, "1.0.0", "schema", null!));
            Assert.Equal("createdBy", ex.ParamName);
        }
        
        [Fact]
        public void AddVersionToTemplate_ShouldThrowInvalidOperationException_WhenVersionExists()
        {
            // Arrange
            var existingVersion = new TemplateVersion(
                new TemplateVersionId(Guid.NewGuid()), 
                _template.Id!, 
                "1.0.0", 
                "existing schema", 
                DateTime.UtcNow, 
                _userId);
            _template.AddVersion(existingVersion);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _factory.AddVersionToTemplate(_template, "1.0.0", "new schema", _userId));
        }
    }
} 