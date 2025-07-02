using DfE.ExternalApplications.Application.Common.Behaviours;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using Xunit;
using ValidationException = DfE.ExternalApplications.Application.Common.Exceptions.ValidationException;

namespace DfE.ExternalApplications.Application.Tests.Common.Behaviours;

public class ValidationBehaviourTests
{
    public record TestRequest(string Value) : IRequest<string>;

    [Fact]
    public async Task Handle_ShouldProceedToNext_WhenNoValidators()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>>();
        var behaviour = new ValidationBehaviour<TestRequest, string>(validators);
        var request = new TestRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("success");

        // Act
        var result = await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_ShouldProceedToNext_WhenAllValidatorsPass()
    {
        // Arrange
        var validator1 = Substitute.For<IValidator<TestRequest>>();
        var validator2 = Substitute.For<IValidator<TestRequest>>();
        
        validator1.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        validator2.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var validators = new List<IValidator<TestRequest>> { validator1, validator2 };
        var behaviour = new ValidationBehaviour<TestRequest, string>(validators);
        var request = new TestRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("success");

        // Act
        var result = await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenSingleValidatorFails()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestRequest>>();
        var validationFailure = new ValidationFailure("Value", "Value is required");
        var validationResult = new ValidationResult(new[] { validationFailure });
        
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(validationResult);

        var validators = new List<IValidator<TestRequest>> { validator };
        var behaviour = new ValidationBehaviour<TestRequest, string>(validators);
        var request = new TestRequest("");
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behaviour.Handle(request, next, CancellationToken.None));

        Assert.Single(exception.Errors);
        Assert.Contains("Value", exception.Errors.Keys);
        Assert.Contains("Value is required", exception.Errors["Value"]);
        await next.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task Handle_ShouldAggregateMultipleValidationFailures()
    {
        // Arrange
        var validator1 = Substitute.For<IValidator<TestRequest>>();
        var validator2 = Substitute.For<IValidator<TestRequest>>();
        
        var failure1 = new ValidationFailure("Value", "Value is required");
        var failure2 = new ValidationFailure("Value", "Value must be at least 5 characters");
        var failure3 = new ValidationFailure("Value", "Value cannot contain special characters");
        
        validator1.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { failure1, failure2 }));
        validator2.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { failure3 }));

        var validators = new List<IValidator<TestRequest>> { validator1, validator2 };
        var behaviour = new ValidationBehaviour<TestRequest, string>(validators);
        var request = new TestRequest("");
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behaviour.Handle(request, next, CancellationToken.None));

        Assert.Single(exception.Errors);
        Assert.Contains("Value", exception.Errors.Keys);
        Assert.Equal(3, exception.Errors["Value"].Length);
        Assert.Contains("Value is required", exception.Errors["Value"]);
        Assert.Contains("Value must be at least 5 characters", exception.Errors["Value"]);
        Assert.Contains("Value cannot contain special characters", exception.Errors["Value"]);
        await next.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToValidators()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var validators = new List<IValidator<TestRequest>> { validator };
        var behaviour = new ValidationBehaviour<TestRequest, string>(validators);
        var request = new TestRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("success");
        var cancellationToken = new CancellationToken();

        // Act
        await behaviour.Handle(request, next, cancellationToken);

        // Assert
        await validator.Received(1).ValidateAsync(
            Arg.Is<ValidationContext<TestRequest>>(ctx => ctx.InstanceToValidate == request),
            Arg.Is<CancellationToken>(ct => ct == cancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldFilterOutValidatorsWithNoErrors()
    {
        // Arrange
        var validatorWithErrors = Substitute.For<IValidator<TestRequest>>();
        var validatorWithoutErrors = Substitute.For<IValidator<TestRequest>>();
        var validatorWithEmptyErrors = Substitute.For<IValidator<TestRequest>>();
        
        var failure = new ValidationFailure("Value", "Value is required");
        validatorWithErrors.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { failure }));
        validatorWithoutErrors.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        validatorWithEmptyErrors.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(Array.Empty<ValidationFailure>()));

        var validators = new List<IValidator<TestRequest>> { validatorWithErrors, validatorWithoutErrors, validatorWithEmptyErrors };
        var behaviour = new ValidationBehaviour<TestRequest, string>(validators);
        var request = new TestRequest("");
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behaviour.Handle(request, next, CancellationToken.None));

        // Only the validator with errors should contribute to the exception
        Assert.Single(exception.Errors);
        Assert.Contains("Value", exception.Errors.Keys);
        Assert.Contains("Value is required", exception.Errors["Value"]);
        await next.DidNotReceive().Invoke();
    }
} 