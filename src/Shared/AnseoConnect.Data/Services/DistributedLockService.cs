using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnseoConnect.Data.Services;

public interface IDistributedLock : IAsyncDisposable
{
    bool Acquired { get; }
}

public interface IDistributedLockService
{
    Task<IDistributedLock> AcquireAsync(string lockName, TimeSpan timeout, CancellationToken cancellationToken = default);
}

public sealed class DistributedLockService : IDistributedLockService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<DistributedLockService> _logger;
    private readonly string _instanceId;

    public DistributedLockService(AnseoConnectDbContext dbContext, ILogger<DistributedLockService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _instanceId = $"{Environment.MachineName}-{Guid.NewGuid():N}";
    }

    public async Task<IDistributedLock> AcquireAsync(string lockName, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureTableAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var expiry = now.Add(timeout);

            var existing = await _dbContext.JobLocks.FirstOrDefaultAsync(l => l.LockName == lockName, cancellationToken);
            if (existing == null)
            {
                _dbContext.JobLocks.Add(new JobLock
                {
                    LockName = lockName,
                    HolderInstanceId = _instanceId,
                    AcquiredAtUtc = now,
                    ExpiresAtUtc = expiry
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
                return new DistributedLockHandle(true, _dbContext, lockName, _instanceId, _logger);
            }

            if (existing.ExpiresAtUtc <= now)
            {
                existing.HolderInstanceId = _instanceId;
                existing.AcquiredAtUtc = now;
                existing.ExpiresAtUtc = expiry;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return new DistributedLockHandle(true, _dbContext, lockName, _instanceId, _logger);
            }

            return new DistributedLockHandle(false, _dbContext, lockName, _instanceId, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Distributed lock acquisition failed for {LockName}", lockName);
            return new DistributedLockHandle(false, _dbContext, lockName, _instanceId, _logger);
        }
    }

    private Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        const string sql = """
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobLocks')
BEGIN
    CREATE TABLE [dbo].[JobLocks](
        [LockName] nvarchar(128) NOT NULL PRIMARY KEY,
        [HolderInstanceId] nvarchar(128) NOT NULL,
        [AcquiredAtUtc] datetimeoffset NOT NULL,
        [ExpiresAtUtc] datetimeoffset NOT NULL
    );
END
""";
        return _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private sealed class DistributedLockHandle : IDistributedLock
    {
        private readonly AnseoConnectDbContext _dbContext;
        private readonly string _lockName;
        private readonly string _instanceId;
        private readonly ILogger _logger;
        private bool _released;

        public bool Acquired { get; }

        public DistributedLockHandle(bool acquired, AnseoConnectDbContext dbContext, string lockName, string instanceId, ILogger logger)
        {
            Acquired = acquired;
            _dbContext = dbContext;
            _lockName = lockName;
            _instanceId = instanceId;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (!Acquired || _released)
            {
                return;
            }

            try
            {
                var row = await _dbContext.JobLocks.FirstOrDefaultAsync(l => l.LockName == _lockName && l.HolderInstanceId == _instanceId);
                if (row != null)
                {
                    row.ExpiresAtUtc = DateTimeOffset.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release lock {LockName}", _lockName);
            }
            finally
            {
                _released = true;
            }
        }
    }
}
