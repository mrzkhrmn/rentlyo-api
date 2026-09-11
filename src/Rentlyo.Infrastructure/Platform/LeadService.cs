using Rentlyo.Application.DTOs.Public;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Entities;
using Rentlyo.Domain.Enums;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;

namespace Rentlyo.Infrastructure.Platform;

public class LeadService(ApplicationDbContext db) : ILeadService
{
    public async Task<LeadResponse> CreateAsync(
        CreateLeadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Company)
            || string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException("Name, company and email are required.");
        }

        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Company = request.Company.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
            WantsWebsite = request.WantsWebsite,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        db.Leads.Add(lead);
        await db.SaveChangesAsync(cancellationToken);

        return new LeadResponse
        {
            Id = lead.Id,
            Name = lead.Name,
            Company = lead.Company,
            Email = lead.Email,
            Phone = lead.Phone,
            Message = lead.Message,
            WantsWebsite = lead.WantsWebsite,
            Status = lead.Status.ToString(),
            CreatedAt = lead.CreatedAt
        };
    }
}
