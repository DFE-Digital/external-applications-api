using System.Diagnostics.CodeAnalysis;

namespace DfE.ExternalApplications.Application.Common.Exceptions;

/// <summary>
/// Represents a 404 Not Found error in the domain.
/// </summary>
[ExcludeFromCodeCoverage]
public class NotFoundException(string message) : Exception(message);