using System.Diagnostics.CodeAnalysis;

namespace GovUK.Dfe.FlexForms.Application.Common.Exceptions;

/// <summary>
/// Represents a 429 Too Many Requests error in the domain.
/// </summary>
[ExcludeFromCodeCoverage]
public class RateLimitExceededException(string message) : Exception(message);
