using System.Diagnostics.CodeAnalysis;

namespace DfE.ExternalApplications.Application.Common.Exceptions;

/// <summary>
/// Represents a 400 Bad Request error in the domain.
/// </summary>
[ExcludeFromCodeCoverage]
public class BadRequestException(string message) : Exception(message);