using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Payments;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Enums;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<PaymentListItem>>>> List(
        [FromQuery] PaymentStatus? status,
        [FromQuery] Guid? reservationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await paymentService.ListAsync(status, reservationId, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<PaymentListItem>>.Success(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await paymentService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<PaymentResponse>.Success(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var data = await paymentService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<PaymentResponse>.Success(data, "Payment created."));
    }

    [HttpPost("{id:guid}/checkout")]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> Checkout(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await paymentService.StartCheckoutAsync(id, cancellationToken);
        return Ok(ApiResponse<PaymentResponse>.Success(data, "Checkout started."));
    }

    [HttpPost("{id:guid}/record")]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> Record(
        Guid id,
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var data = await paymentService.RecordManualAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PaymentResponse>.Success(data, "Payment recorded."));
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> Refund(
        Guid id,
        [FromBody] RefundPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var data = await paymentService.RefundAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PaymentResponse>.Success(data, "Refund completed."));
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<ActionResult<ApiResponse<object?>>> Webhook(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        PaymentWebhookRequest? payload;
        try
        {
            payload = System.Text.Json.JsonSerializer.Deserialize<PaymentWebhookRequest>(
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

        var signature = Request.Headers["X-Payment-Signature"].FirstOrDefault();
        await paymentService.HandleWebhookAsync(payload, signature, rawBody, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Webhook processed."));
    }
}
