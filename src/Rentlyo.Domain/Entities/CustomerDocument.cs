using Rentlyo.Domain.Enums;

namespace Rentlyo.Domain.Entities;

public class CustomerDocument
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public CustomerDocumentType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
}
