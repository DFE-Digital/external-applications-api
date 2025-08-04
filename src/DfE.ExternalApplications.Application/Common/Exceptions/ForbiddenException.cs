using System.Diagnostics.CodeAnalysis;

namespace DfE.ExternalApplications.Application.Common.Exceptions;

/// <summary>
/// Represents a 403 Forbidden error in the domain.
/// </summary>
[ExcludeFromCodeCoverage]
public class ForbiddenException(string message) : Exception(message);