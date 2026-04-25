using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace CarrierRatesQueryV2.Tests.Infrastructure;

/// <summary>
/// Fixture for simulating carrier API failures in integration tests.
/// Uses WireMock to mock external HTTP calls to carrier APIs.
/// </summary>
public class CarrierFailureSimulationFixture : IDisposable
{
    private readonly WireMockServer _server;

    public CarrierFailureSimulationFixture()
    {
        _server = WireMockServer.Start();
    }

    /// <summary>
    /// Configures a carrier endpoint to return a specific HTTP status code for a specified number of calls.
    /// After the specified number of calls, it will return success.
    /// </summary>
    public void ConfigureCarrierToFail(string carrierName, HttpStatusCode statusCode, int failureCount = 1)
    {
        var carrierLower = carrierName.ToLowerInvariant();
        
        // Remove any existing stubs for this carrier
        _server.Reset();
        
        // Configure failure responses followed by success
        for (int i = 0; i < failureCount; i++)
        {
            _server.Given(
                    Request.Create()
                        .WithPath($"/{carrierLower}/*")
                        .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode((int)statusCode)
                        .WithBody($"{carrierName} service unavailable"));
        }
        
        // Configure success response for subsequent calls
        _server.Given(
                Request.Create()
                    .WithPath($"/{carrierLower}/*")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBody("{\"success\": true}"));
    }

    /// <summary>
    /// Configures a carrier endpoint to timeout (simulate by hanging connection) for a specified number of calls.
    /// </summary>
    public void ConfigureCarrierToTimeout(string carrierName, int timeoutCount = 1)
    {
        var carrierLower = carrierName.ToLowerInvariant();
        
        // Remove any existing stubs for this carrier
        _server.Reset();
        
        // Configure timeout responses (simulated by delayed response)
        for (int i = 0; i < timeoutCount; i++)
        {
            _server.Given(
                    Request.Create()
                        .WithPath($"/{carrierLower}/*")
                        .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithBody("{\"success\": true}")
                        .WithDelay(TimeSpan.FromSeconds(30))); // Long delay to simulate timeout
        }
        
        // Configure success response for subsequent calls
        _server.Given(
                Request.Create()
                    .WithPath($"/{carrierLower}/*")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBody("{\"success\": true}"));
    }

    /// <summary>
    /// Resets all carrier simulations to success responses.
    /// </>
    public void ResetAllToSuccess()
    {
        _server.Reset();
        _server.Given(
                Request.Create()
                    .WithPath("/*")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBody("{\"success\": true}"));
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}