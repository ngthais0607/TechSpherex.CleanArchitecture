using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.AcceptanceTests;

/// <summary>
/// Kịch bản 1: Tạo container với số sai số kiểm tra → 400 C1.
/// </summary>
[Collection("Api")]
public sealed class Scenario01_CheckDigitTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario01_CreateContainer_WithInvalidCheckDigit_Returns400_C1()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        // CSQU3054383 là hợp lệ, đổi số cuối thành 9 là sai check digit
        var invalidNumber = "CSQU3054389";

        var payload = new
        {
            ContainerNumber = invalidNumber,
            IsoCode = "22G1",
            Size = "Ft20",
            Type = "Dry",
            TareWeight = 2200,
            MaxGrossWeight = 30480,
            Grade = "A",
            Condition = "Normal"
        };

        var response = await client.PostAsJsonAsync("/api/containers", payload, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        // Kiểm tra mã lỗi chứa ContainerNumber.CheckDigitMismatch hoặc ruleCode C1
        Assert.True(
            body.Contains("ContainerNumber.CheckDigitMismatch") ||
            (json.TryGetProperty("ruleCode", out var rc) && rc.GetString() == "C1") ||
            (json.TryGetProperty("code", out var c) && c.GetString() == "ContainerNumber.CheckDigitMismatch"),
            $"Expected C1 or ContainerNumber.CheckDigitMismatch error, got: {body}");
    }
}
