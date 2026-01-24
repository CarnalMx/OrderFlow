using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Xunit;

namespace OrderFlow.Tests.Api;

public class ExceptionHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExceptionHandlingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CrashEndpoint_Returns500_WithJsonError()
    {
        var res = await _client.GetAsync("/debug/crash");

        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);

        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Internal server error", body);
        Assert.Contains("correlationId", body);
    }
}
