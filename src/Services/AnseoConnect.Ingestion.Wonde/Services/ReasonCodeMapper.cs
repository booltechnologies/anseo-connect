using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Ingestion.Wonde.Services;

/// <summary>
/// Maps SIS provider-specific reason codes to normalized internal reason codes.
/// </summary>
public interface IReasonCodeMapper
{
    /// <summary>
    /// Maps a provider-specific reason code to an internal normalized code.
    /// </summary>
    Task<string?> MapToInternalAsync(string provider, string providerCode, Guid tenantId, Guid schoolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps an internal normalized code back to a provider-specific code (reverse mapping).
    /// </summary>
    Task<string?> MapToProviderAsync(string provider, string internalCode, Guid tenantId, Guid schoolId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of reason code mapping service.
/// </summary>
public sealed class ReasonCodeMapper : IReasonCodeMapper
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<ReasonCodeMapper> _logger;
    private readonly ITenantContext _tenantContext;

    public ReasonCodeMapper(
        AnseoConnectDbContext dbContext,
        ILogger<ReasonCodeMapper> logger,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    public async Task<string?> MapToInternalAsync(
        string provider,
        string providerCode,
        Guid tenantId,
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerCode))
        {
            return null;
        }

        // Try school-specific mapping first
        var mapping = await _dbContext.ReasonCodeMappings
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId &&
                       m.SchoolId == schoolId &&
                       m.ProviderId == provider &&
                       m.ProviderCode == providerCode &&
                       m.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (mapping != null)
        {
            return mapping.InternalCode;
        }

        // Try tenant-level mapping (schoolId = Guid.Empty would indicate tenant-level default)
        mapping = await _dbContext.ReasonCodeMappings
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId &&
                       m.SchoolId == Guid.Empty &&
                       m.ProviderId == provider &&
                       m.ProviderCode == providerCode &&
                       m.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (mapping != null)
        {
            return mapping.InternalCode;
        }

        // If no mapping found, return the provider code as-is (fallback)
        _logger.LogDebug("No mapping found for provider {Provider} code {Code}, using provider code as internal code", provider, providerCode);
        return providerCode;
    }

    public async Task<string?> MapToProviderAsync(
        string provider,
        string internalCode,
        Guid tenantId,
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(internalCode))
        {
            return null;
        }

        // Try school-specific mapping first (reverse lookup)
        var mapping = await _dbContext.ReasonCodeMappings
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId &&
                       m.SchoolId == schoolId &&
                       m.ProviderId == provider &&
                       m.InternalCode == internalCode &&
                       m.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (mapping != null)
        {
            return mapping.ProviderCode;
        }

        // Try tenant-level mapping
        mapping = await _dbContext.ReasonCodeMappings
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId &&
                       m.SchoolId == Guid.Empty &&
                       m.ProviderId == provider &&
                       m.InternalCode == internalCode &&
                       m.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (mapping != null)
        {
            return mapping.ProviderCode;
        }

        // If no mapping found, return the internal code as-is (fallback)
        _logger.LogDebug("No reverse mapping found for provider {Provider} internal code {Code}, using internal code as provider code", provider, internalCode);
        return internalCode;
    }
}
