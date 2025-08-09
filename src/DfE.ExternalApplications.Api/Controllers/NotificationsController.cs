using Asp.Versioning;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Request;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Http.Models;
using DfE.ExternalApplications.Application.Notifications.Commands;
using DfE.ExternalApplications.Application.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DfE.ExternalApplications.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public class NotificationsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Creates a new notification for the current user.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "The created notification.", typeof(NotificationDto))]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [SwaggerResponse(429, "Too Many Requests.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> CreateNotificationAsync(
        [FromBody] AddNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddNotificationCommand(
            request.Message,
            request.Type,
            request.Category,
            request.Context,
            request.AutoDismiss,
            request.AutoDismissSeconds,
            request.ActionUrl,
            request.Metadata,
            request.Priority,
            request.ReplaceExistingContext);

        var result = await sender.Send(command, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Gets all unread notifications for the current user.
    /// </summary>
    [HttpGet("unread")]
    [SwaggerResponse(200, "List of unread notifications.", typeof(IEnumerable<NotificationDto>))]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> GetUnreadNotificationsAsync(CancellationToken cancellationToken)
    {
        var query = new GetUnreadNotificationsQuery();
        var result = await sender.Send(query, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Gets all notifications (read and unread) for the current user.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of all notifications.", typeof(IEnumerable<NotificationDto>))]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> GetAllNotificationsAsync(CancellationToken cancellationToken)
    {
        var query = new GetAllNotificationsQuery();
        var result = await sender.Send(query, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Gets notifications filtered by category.
    /// </summary>
    [HttpGet("category/{category}")]
    [SwaggerResponse(200, "List of notifications for the specified category.", typeof(IEnumerable<NotificationDto>))]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> GetNotificationsByCategoryAsync(
        [FromRoute] string category,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNotificationsByCategoryQuery(category, unreadOnly);
        var result = await sender.Send(query, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Gets the count of unread notifications for the current user.
    /// </summary>
    [HttpGet("unread/count")]
    [SwaggerResponse(200, "Count of unread notifications.", typeof(int))]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> GetUnreadNotificationCountAsync(CancellationToken cancellationToken)
    {
        var query = new GetUnreadNotificationCountQuery();
        var result = await sender.Send(query, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    [HttpPut("{notificationId}/read")]
    [SwaggerResponse(200, "Notification marked as read successfully.")]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(404, "Notification not found.", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [SwaggerResponse(429, "Too Many Requests.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> MarkNotificationAsReadAsync(
        [FromRoute] string notificationId,
        CancellationToken cancellationToken)
    {
        var command = new MarkNotificationAsReadCommand(notificationId);
        var result = await sender.Send(command, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Marks all notifications as read for the current user.
    /// </summary>
    [HttpPut("read-all")]
    [SwaggerResponse(200, "All notifications marked as read successfully.")]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [SwaggerResponse(429, "Too Many Requests.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> MarkAllNotificationsAsReadAsync(CancellationToken cancellationToken)
    {
        var command = new MarkAllNotificationsAsReadCommand();
        var result = await sender.Send(command, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Removes a specific notification.
    /// </summary>
    [HttpDelete("{notificationId}")]
    [SwaggerResponse(200, "Notification removed successfully.")]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(404, "Notification not found.", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [SwaggerResponse(429, "Too Many Requests.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> RemoveNotificationAsync(
        [FromRoute] string notificationId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveNotificationCommand(notificationId);
        var result = await sender.Send(command, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Clears all notifications for the current user.
    /// </summary>
    [HttpDelete("clear-all")]
    [SwaggerResponse(200, "All notifications cleared successfully.")]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [SwaggerResponse(429, "Too Many Requests.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> ClearAllNotificationsAsync(CancellationToken cancellationToken)
    {
        var command = new ClearAllNotificationsCommand();
        var result = await sender.Send(command, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Clears notifications by category for the current user.
    /// </summary>
    [HttpDelete("category/{category}")]
    [SwaggerResponse(200, "Notifications cleared successfully.")]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [SwaggerResponse(429, "Too Many Requests.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> ClearNotificationsByCategoryAsync(
        [FromRoute] string category,
        CancellationToken cancellationToken)
    {
        var command = new ClearNotificationsByCategoryCommand(category);
        var result = await sender.Send(command, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Clears notifications by context for the current user.
    /// </summary>
    [HttpDelete("context/{context}")]
    [SwaggerResponse(200, "Notifications cleared successfully.")]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [SwaggerResponse(429, "Too Many Requests.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> ClearNotificationsByContextAsync(
        [FromRoute] string context,
        CancellationToken cancellationToken)
    {
        var command = new ClearNotificationsByContextCommand(context);
        var result = await sender.Send(command, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }
}
