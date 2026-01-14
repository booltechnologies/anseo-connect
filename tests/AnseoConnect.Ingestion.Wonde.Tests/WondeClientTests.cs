using AnseoConnect.Ingestion.Wonde.Client;
using FluentAssertions;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace AnseoConnect.Ingestion.Wonde.Tests;

public class WondeClientTests : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly HttpClient _httpClient;
    private readonly IWondeClient _wondeClient;

    public WondeClientTests()
    {
        _mockServer = WireMockServer.Start();
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_mockServer.Url!)
        };
        var logger = new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<WondeClient>();
        _wondeClient = new WondeClient(_httpClient, "test-token", "localhost", logger, disposeHttpClient: false);
    }

    [Fact]
    public async Task GetStudentsAsync_ReturnsPagedResponse()
    {
        // Arrange
        var schoolId = "test-school-123";
        var mockResponse = new
        {
            data = new[]
            {
                new { id = "student-1", forename = "John", surname = "Doe", active = true }
            },
            meta = new
            {
                pagination = new { more = false, next = (string?)null, per_page = 100, current_page = 1 }
            }
        };

        _mockServer
            .Given(Request.Create()
                .WithPath($"/v1.0/schools/{schoolId}/students")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(mockResponse)));

        // Act
        var result = await _wondeClient.GetStudentsAsync(schoolId);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data[0].Id.Should().Be("student-1");
        result.Data[0].Forename.Should().Be("John");
        result.Data[0].Surname.Should().Be("Doe");
    }

    [Fact]
    public async Task GetStudentsAsync_HandlesPagination()
    {
        // Arrange
        var schoolId = "test-school-123";
        var firstPage = new
        {
            data = new[] { new { id = "student-1", forename = "John", surname = "Doe", active = true } },
            meta = new
            {
                pagination = new { more = true, next = $"{_mockServer.Url}/v1.0/schools/{schoolId}/students?page=2", per_page = 1, current_page = 1 }
            }
        };

        var secondPage = new
        {
            data = new[] { new { id = "student-2", forename = "Jane", surname = "Smith", active = true } },
            meta = new
            {
                pagination = new { more = false, next = (string?)null, per_page = 1, current_page = 2 }
            }
        };

        _mockServer
            .Given(Request.Create()
                .WithPath($"/v1.0/schools/{schoolId}/students")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(firstPage)));

        _mockServer
            .Given(Request.Create()
                .WithPath($"/v1.0/schools/{schoolId}/students")
                .WithParam("page", "2")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(secondPage)));

        // Act
        var result = await _wondeClient.GetStudentsAsync(schoolId);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.Data[0].Id.Should().Be("student-1");
        result.Data[1].Id.Should().Be("student-2");
    }

    [Fact]
    public async Task GetStudentsAsync_RespectsUpdatedAfterParameter()
    {
        // Arrange
        var schoolId = "test-school-123";
        var updatedAfter = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        _mockServer
            .Given(Request.Create()
                .WithPath($"/v1.0/schools/{schoolId}/students")
                .WithParam("updated_after", "2026-01-01 00:00:00")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(new { data = Array.Empty<object>(), meta = new { pagination = new { more = false } } })));

        // Act
        var result = await _wondeClient.GetStudentsAsync(schoolId, updatedAfter);

        // Assert
        result.Should().NotBeNull();
        _mockServer.LogEntries.Should().Contain(e => 
            e.RequestMessage.Path.Contains("/students") && 
            e.RequestMessage.Query.ContainsKey("updated_after"));
    }

    public void Dispose()
    {
        _mockServer?.Stop();
        _mockServer?.Dispose();
        _httpClient?.Dispose();
    }
}
