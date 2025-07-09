using DfE.ExternalApplications.Application.Templates.Commands;
using FluentValidation.TestHelper;
using System.Text.RegularExpressions;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Templates
{
    public class CreateTemplateVersionCommandValidatorTests
    {
        private readonly CreateTemplateVersionCommandValidator _validator = new();

        [Fact]
        public void Should_have_error_when_TemplateId_is_empty()
        {
            var command = new CreateTemplateVersionCommand(Guid.Empty, "1.0.0", "c2NoZW1h");
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.TemplateId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("1.0")]
        [InlineData("a.b.c")]
        [InlineData("1.0.0-alpha")]
        public void Should_have_error_when_VersionNumber_is_invalid(string version)
        {
            var command = new CreateTemplateVersionCommand(Guid.NewGuid(), version, "c2NoZW1h");
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.VersionNumber);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("not-base64")]
        public void Should_have_error_when_JsonSchema_is_not_valid_base64(string schema)
        {
            var command = new CreateTemplateVersionCommand(Guid.NewGuid(), "1.0.0", schema);
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.JsonSchema);
        }

        [Fact]
        public void Should_not_have_error_when_command_is_valid()
        {
            var command = new CreateTemplateVersionCommand(Guid.NewGuid(), "1.2.3", "eyJuYW1lIjoiVGVzdCJ9");
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
} 