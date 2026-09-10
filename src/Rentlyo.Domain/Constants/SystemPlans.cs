namespace Rentlyo.Domain.Constants;

public static class SystemPlans
{
    public const string Free = "free";
    public const string Starter = "starter";
    public const string Pro = "pro";

    public static readonly Guid FreeId = Guid.Parse("33333333-3333-3333-3333-333333333301");
    public static readonly Guid StarterId = Guid.Parse("33333333-3333-3333-3333-333333333302");
    public static readonly Guid ProId = Guid.Parse("33333333-3333-3333-3333-333333333303");
}
