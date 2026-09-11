using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Subscriptions;
using Rentlyo.Application.Interfaces;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/subscriptions")]
public class SubscriptionsController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> GetCurrent(
        CancellationToken cancellationToken)
    {
        var data = await subscriptionService.GetCurrentAsync(cancellationToken);
        return Ok(ApiResponse<SubscriptionResponse>.Success(data));
    }

    [HttpGet("payments")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<SubscriptionPaymentListItem>>>> ListPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await subscriptionService.ListPaymentsAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<SubscriptionPaymentListItem>>.Success(data));
    }

    [HttpPost("change-plan")]
    public async Task<ActionResult<ApiResponse<ChangeSubscriptionPlanResult>>> ChangePlan(
        [FromBody] ChangeSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var data = await subscriptionService.ChangePlanAsync(request, cancellationToken);
        return Ok(ApiResponse<ChangeSubscriptionPlanResult>.Success(
            data,
            data.RequiresPayment ? "Payment required to activate plan." : "Plan updated."));
    }

    [HttpPost("payments/{id:guid}/checkout")]
    public async Task<ActionResult<ApiResponse<SubscriptionPaymentListItem>>> Checkout(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await subscriptionService.StartCheckoutAsync(id, cancellationToken);
        return Ok(ApiResponse<SubscriptionPaymentListItem>.Success(data, "Checkout started."));
    }

    [HttpPost("payments/{id:guid}/record")]
    public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> Record(
        Guid id,
        [FromBody] RecordSubscriptionPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var data = await subscriptionService.RecordManualPaymentAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SubscriptionResponse>.Success(data, "Payment recorded."));
    }

    [HttpPost("cancel")]
    public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> Cancel(
        CancellationToken cancellationToken)
    {
        var data = await subscriptionService.CancelAsync(cancellationToken);
        return Ok(ApiResponse<SubscriptionResponse>.Success(data, "Cancellation scheduled at period end."));
    }

    [HttpPost("resume")]
    public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> Resume(
        CancellationToken cancellationToken)
    {
        var data = await subscriptionService.ResumeAsync(cancellationToken);
        return Ok(ApiResponse<SubscriptionResponse>.Success(data, "Cancellation withdrawn."));
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<ActionResult<ApiResponse<object?>>> Webhook(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        SubscriptionWebhookRequest? payload;
        try
        {
            payload = System.Text.Json.JsonSerializer.Deserialize<SubscriptionWebhookRequest>(
                rawBody,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return BadRequest(ApiResponse<object?>.Failure("Invalid webhook payload."));
        }

        if (payload is null)
        {
            return BadRequest(ApiResponse<object?>.Failure("Invalid webhook payload."));
        }

        var signature = Request.Headers["X-Subscription-Signature"].FirstOrDefault();
        await subscriptionService.HandleWebhookAsync(payload, signature, rawBody, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Webhook processed."));
    }
}
