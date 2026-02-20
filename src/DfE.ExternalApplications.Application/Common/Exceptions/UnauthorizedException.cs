using System.Diagnostics.CodeAnalysis;

namespace DfE.ExternalApplications.Application.Common.Exceptions;

/// <summary>
/// Thrown when the request is not authenticated (no valid user token).
/// Mapped to HTTP 401 by the global exception handler.
/// </summary>
[ExcludeFromCodeCoverage]
public class UnauthorizedException() : Exception("No valid user token");
