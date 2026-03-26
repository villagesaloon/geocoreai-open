using StackExchange.Redis;

namespace GeoCore.SaaS.Services.Detection;

/// <summary>
/// Redis 连接服务
/// 管理 Redis 连接的生命周期
/// 配置从 ConfigCacheService 读取（数据库 system_configs 表）
/// </summary>
public class RedisConnectionService : IDisposable
{
    private readonly ILogger<RedisConnectionService> _logger;
    private readonly ConfigCacheService _configCache;
    private ConnectionMultiplexer? _connection;
    private string? _currentConnectionString;
    private readonly object _lock = new();

    public RedisConnectionService(ConfigCacheService configCache, ILogger<RedisConnectionService> logger)
    {
        _logger = logger;
        _configCache = configCache;
    }

    /// <summary>
    /// 获取 Redis 连接字符串（从缓存读取）
    /// </summary>
    private string GetConnectionString()
    {
        var connectionString = _configCache.GetSystemValue("redis", "connection_string");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Redis connection string not configured in system_configs (redis.connection_string)");
        }
        return connectionString;
    }

    /// <summary>
    /// 获取 Redis 连接
    /// </summary>
    public ConnectionMultiplexer Connection
    {
        get
        {
            var connectionString = GetConnectionString();
            
            // 如果连接字符串变化，重新连接
            if (_currentConnectionString != connectionString)
            {
                lock (_lock)
                {
                    if (_currentConnectionString != connectionString)
                    {
                        _connection?.Dispose();
                        _connection = null;
                        _currentConnectionString = connectionString;
                    }
                }
            }

            if (_connection != null && _connection.IsConnected)
                return _connection;

            lock (_lock)
            {
                if (_connection != null && _connection.IsConnected)
                    return _connection;

                _logger.LogInformation("[Redis] Connecting to Redis...");
                
                try
                {
                    _connection = ConnectionMultiplexer.Connect(connectionString);
                    _currentConnectionString = connectionString;
                    _logger.LogInformation("[Redis] Connected successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Redis] Failed to connect");
                    throw;
                }

                return _connection;
            }
        }
    }

    /// <summary>
    /// 获取 Redis 数据库
    /// </summary>
    public IDatabase GetDatabase() => Connection.GetDatabase();

    /// <summary>
    /// 获取 Redis 订阅者
    /// </summary>
    public ISubscriber GetSubscriber() => Connection.GetSubscriber();

    /// <summary>
    /// 检查连接状态
    /// </summary>
    public bool IsConnected => _connection?.IsConnected ?? false;

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
