using Rentlyo.Application.DTOs.Dashboard;

namespace Rentlyo.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
}
