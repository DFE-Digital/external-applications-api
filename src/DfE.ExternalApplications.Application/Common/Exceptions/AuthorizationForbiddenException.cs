using System.Diagnostics.CodeAnalysis;

namespace DfE.ExternalApplications.Application.Common.Exceptions;

/// <summary>
/// Thrown when the user is authenticated but does not have the required permissions.
/// Mapped to HTTP 403 by the global exception handler (e.g. from authorization middleware).
/// </summary>
[ExcludeFromCodeCoverage]
public class AuthorizationForbiddenException() : Exception("User does not have required permissions");
