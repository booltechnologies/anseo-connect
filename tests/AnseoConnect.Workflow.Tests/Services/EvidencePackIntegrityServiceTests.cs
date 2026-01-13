using AnseoConnect.Data;
using AnseoConnect.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace AnseoConnect.Workflow.Tests.Services;

public class EvidencePackIntegrityServiceTests
{
    [Fact]
    public void ComputeContentHash_WithPdfBytes_ReturnsSha256Hash()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AnseoConnectDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new AnseoConnectDbContext(options, new Mock<AnseoConnect.Data.MultiTenancy.ITenantContext>().Object);
        var logger = new Mock<ILogger<EvidencePackIntegrityService>>();
        var service = new EvidencePackIntegrityService(dbContext, logger.Object);

        var pdfBytes = Encoding.UTF8.GetBytes("Test PDF content");

        // Act
        var hash = service.ComputeContentHash(pdfBytes);

        // Assert
        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length); // SHA-256 hex string is 64 characters

        // Verify it's actually SHA-256
        using var sha256 = SHA256.Create();
        var expectedHash = Convert.ToHexString(sha256.ComputeHash(pdfBytes)).ToLowerInvariant();
        Assert.Equal(expectedHash, hash);
    }

    [Fact]
    public void ComputeManifestHash_WithJsonString_ReturnsSha256Hash()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AnseoConnectDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new AnseoConnectDbContext(options, new Mock<AnseoConnect.Data.MultiTenancy.ITenantContext>().Object);
        var logger = new Mock<ILogger<EvidencePackIntegrityService>>();
        var service = new EvidencePackIntegrityService(dbContext, logger.Object);

        var json = """{"test": "data"}""";

        // Act
        var hash = service.ComputeManifestHash(json);

        // Assert
        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length);

        // Verify it's actually SHA-256
        var bytes = Encoding.UTF8.GetBytes(json);
        using var sha256 = SHA256.Create();
        var expectedHash = Convert.ToHexString(sha256.ComputeHash(bytes)).ToLowerInvariant();
        Assert.Equal(expectedHash, hash);
    }
}
