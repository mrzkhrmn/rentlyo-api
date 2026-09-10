using StackExchange.Redis;

namespace Rentlyo.Infrastructure.Redis;

public static class RedisConnectionFactory
{
    public static IConnectionMultiplexer Create(string connectionString)
    {
        return ConnectionMultiplexer.Connect(connectionString);
    }
}
