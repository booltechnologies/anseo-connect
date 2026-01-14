using AnseoConnect.Contracts.SIS;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Ingestion.Wonde;
using AnseoConnect.Ingestion.Wonde.Client;
using AnseoConnect.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AnseoConnect.Ingestion.Wonde.Tests;

public class WondeConnectorTests : IDisposable
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly TestTenantContext _tenantContext;
    private readonly IWondeClient _mockWondeClient;
    private readonly IMessageBus _mockMessageBus;
    private readonly WondeConnector _connector;

    public WondeConnectorTests()
    {
        var options = new DbContextOptionsBuilder<AnseoConnectDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options;

        _dbContext = new AnseoConnectDbContext(options, new TestTenantContext());
        _tenantContext = new TestTenantContext();

        _mockWondeClient = new MockWondeClient();
        _mockMessageBus = new MockMessageBus();

        var logger = new LoggerFactory().CreateLogger<WondeConnector>();
        _connector = new WondeConnector(_mockWondeClient, _dbContext, _mockMessageBus, logger, _tenantContext);
    }

    [Fact]
    public void ProviderId_ShouldBeWONDE()
    {
        _connector.ProviderId.Should().Be("WONDE");
    }

    [Fact]
    public void Capabilities_ShouldIncludeRosterContactsAttendance()
    {
        _connector.Capabilities.Should().Contain(SisCapability.RosterSync);
        _connector.Capabilities.Should().Contain(SisCapability.ContactsSync);
        _connector.Capabilities.Should().Contain(SisCapability.AttendanceSync);
    }

    [Fact]
    public async Task SyncRosterAsync_ShouldCreateSyncRun()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var school = new School
        {
            SchoolId = schoolId,
            TenantId = tenantId,
            Name = "Test School",
            WondeSchoolId = "wonde-school-123"
        };
        _dbContext.Schools.Add(school);
        await _dbContext.SaveChangesAsync();

        _tenantContext.Set(tenantId, schoolId);

        var options = new SyncOptions { ForceFullSync = false };

        // Act
        var result = await _connector.SyncRosterAsync(schoolId, options);

        // Assert
        result.Should().NotBeNull();
        result.SyncRunId.Should().NotBeEmpty();

        var syncRun = await _dbContext.SyncRuns.FindAsync(result.SyncRunId);
        syncRun.Should().NotBeNull();
        syncRun!.ProviderId.Should().Be("WONDE");
        syncRun.SyncType.Should().Be("Roster");
        syncRun.Status.Should().Be("SUCCEEDED");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}

// Mock implementations for testing
internal class MockWondeClient : IWondeClient
{
    public Task<WondeSchoolResponse?> GetSchoolAsync(string schoolId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<WondePagedResponse<WondeStudent>> GetStudentsAsync(string schoolId, DateTimeOffset? updatedAfter = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WondePagedResponse<WondeStudent>(
            new List<WondeStudent>
            {
                new("student-1", null, "John", "Doe", null, null, true)
            },
            null));
    }

    public Task<WondePagedResponse<WondeContact>> GetContactsAsync(string schoolId, DateTimeOffset? updatedAfter = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WondePagedResponse<WondeContact>(
            new List<WondeContact>
            {
                new("contact-1", null, "Parent", "One", null)
            },
            null));
    }

    public Task<WondePagedResponse<WondeAttendance>> GetAttendanceAsync(string schoolId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WondePagedResponse<WondeAttendance>(
            new List<WondeAttendance>(),
            null));
    }

    public Task<WondePagedResponse<WondeStudentAbsence>> GetStudentAbsencesAsync(string schoolId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<WondePagedResponse<WondeClass>> GetClassesAsync(string schoolId, DateTimeOffset? updatedAfter = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WondePagedResponse<WondeClass>(
            new List<WondeClass>(),
            null));
    }

    public Task<WondePagedResponse<WondeTimetable>> GetTimetableAsync(string schoolId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WondePagedResponse<WondeTimetable>(
            new List<WondeTimetable>(),
            null));
    }
}

internal class MockMessageBus : IMessageBus
{
    public Task PublishAsync<T>(AnseoConnect.Contracts.Common.MessageEnvelope<T> envelope, CancellationToken cancellationToken = default) where T : notnull
    {
        return Task.CompletedTask;
    }
}
