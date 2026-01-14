using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AnseoConnect.Data.Services;

/// <summary>
/// Service for logging audit events (append-only audit trail for GDPR compliance).
/// </summary>
public interface IAuditService
{
    Task LogAsync(AuditEventBuilder builder, CancellationToken cancellationToken = default);
    Task<(List<AuditEvent> Events, int TotalCount)> SearchAsync(
        AuditSearchRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Builder pattern for constructing audit events.
/// </summary>
public sealed class AuditEventBuilder
{
    private readonly Guid _tenantId;
    private readonly Guid? _schoolId;
    private readonly string _actorId;
    private readonly string _actorName;
    private readonly string _actorType;
    private readonly string _ipAddress;
    private readonly string _userAgent;
    private string? _action;
    private string? _entityType;
    private string? _entityId;
    private string? _entityDisplayName;
    private object? _metadata;

    public AuditEventBuilder(
        Guid tenantId,
        Guid? schoolId,
        string actorId,
        string actorName,
        string actorType,
        string ipAddress = "",
        string userAgent = "")
    {
        _tenantId = tenantId;
        _schoolId = schoolId;
        _actorId = actorId;
        _actorName = actorName;
        _actorType = actorType;
        _ipAddress = ipAddress;
        _userAgent = userAgent;
    }

    public AuditEventBuilder Action(string action)
    {
        _action = action;
        return this;
    }

    public AuditEventBuilder Entity(string entityType, string entityId, string? entityDisplayName = null)
    {
        _entityType = entityType;
        _entityId = entityId;
        _entityDisplayName = entityDisplayName;
        return this;
    }

    public AuditEventBuilder Metadata(object metadata)
    {
        _metadata = metadata;
        return this;
    }

    internal AuditEvent Build()
    {
        if (string.IsNullOrWhiteSpace(_action))
        {
            throw new InvalidOperationException("Action is required");
        }
        if (string.IsNullOrWhiteSpace(_entityType))
        {
            throw new InvalidOperationException("EntityType is required");
        }

        var metadataJson = _metadata != null
            ? JsonSerializer.Serialize(_metadata)
            : "{}";

        var auditEvent = new AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            TenantId = _tenantId,
            SchoolId = _schoolId,
            ActorId = _actorId,
            ActorName = _actorName,
            ActorType = _actorType,
            IpAddress = _ipAddress,
            UserAgent = _userAgent,
            Action = _action,
            EntityType = _entityType,
            EntityId = _entityId ?? "",
            EntityDisplayName = _entityDisplayName ?? "",
            MetadataJson = metadataJson,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        // TODO: Implement hash chain for tamper evidence (PreviousEventHash, EventHash)

        return auditEvent;
    }
}

/// <summary>
/// Request model for audit search.
/// </summary>
public sealed class AuditSearchRequest
{
    public Guid? SchoolId { get; set; }
    public string? ActorId { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

/// <summary>
/// Implementation of audit service.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        AnseoConnectDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<AuditService> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Logs an audit event (append-only).
    /// </summary>
    public async Task LogAsync(AuditEventBuilder builder, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = builder.Build();
            _dbContext.AuditEvents.Add(auditEvent);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event: {Action}", builder);
            // Don't throw - audit logging should never break the main flow
        }
    }

    /// <summary>
    /// Searches audit events with filters.
    /// </summary>
    public async Task<(List<AuditEvent> Events, int TotalCount)> SearchAsync(
        AuditSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var query = _dbContext.AuditEvents
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId);

        if (request.SchoolId.HasValue)
        {
            query = query.Where(e => e.SchoolId == request.SchoolId);
        }

        if (!string.IsNullOrWhiteSpace(request.ActorId))
        {
            query = query.Where(e => e.ActorId == request.ActorId);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(e => e.Action == request.Action);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(e => e.EntityType == request.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityId))
        {
            query = query.Where(e => e.EntityId == request.EntityId);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(e => e.OccurredAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(e => e.OccurredAtUtc <= request.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var events = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return (events, totalCount);
    }
}
