using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Reservations;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Enums;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/reservations")]
public class ReservationsController(IReservationService reservationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ReservationListItem>>>> List(
        [FromQuery] ReservationStatus? status,
        [FromQuery] Guid? vehicleId,
        [FromQuery] Guid? customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await reservationService.ListAsync(
            status, vehicleId, customerId, from, to, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<ReservationListItem>>.Success(data));
    }

    [HttpGet("availability")]
    public async Task<ActionResult<ApiResponse<AvailabilityResponse>>> Availability(
        [FromQuery] Guid vehicleId,
        [FromQuery] DateTime startAt,
        [FromQuery] DateTime endAt,
        [FromQuery] Guid? excludeReservationId,
        CancellationToken cancellationToken = default)
    {
        var data = await reservationService.CheckAvailabilityAsync(
            vehicleId, startAt, endAt, excludeReservationId, cancellationToken);
        return Ok(ApiResponse<AvailabilityResponse>.Success(data));
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReservationCalendarItem>>>> Calendar(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken = default)
    {
        var data = await reservationService.GetCalendarAsync(from, to, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReservationCalendarItem>>.Success(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await reservationService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ReservationResponse>.Success(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var data = await reservationService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<ReservationResponse>.Success(data, "Reservation created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> Update(
        Guid id,
        [FromBody] UpdateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var data = await reservationService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ReservationResponse>.Success(data, "Reservation updated."));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> Confirm(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await reservationService.ConfirmAsync(id, cancellationToken);
        return Ok(ApiResponse<ReservationResponse>.Success(data, "Reservation confirmed."));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await reservationService.CancelAsync(id, cancellationToken);
        return Ok(ApiResponse<ReservationResponse>.Success(data, "Reservation cancelled."));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> Complete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var data = await reservationService.CompleteAsync(id, cancellationToken);
        return Ok(ApiResponse<ReservationResponse>.Success(data, "Reservation completed."));
    }
}
