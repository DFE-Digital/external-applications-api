using System.Diagnostics.CodeAnalysis;

namespace DfE.ExternalApplications.Application.Common.Exceptions;

/// <summary>
/// Represents a 409 Conflict error in the domain.
/// </summary>
[ExcludeFromCodeCoverage]
public class ConflictException(string message) : Exception(message);