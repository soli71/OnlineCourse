using Newtonsoft.Json;
using System.Text;

namespace OnlineCourse.Services;

public class LicenseResponse
{
    [JsonProperty("_id")]
    public string Id { get; set; }

    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}

public class SpotPlayerResponse
{
    public bool IsSuccess { get; set; }
    public string Description { get; set; }
    public LicenseResponse Result { get; set; }
}

public class SpotPlayerService : ISpotPlayerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SpotPlayerService> _logger;

    public SpotPlayerService(IHttpClientFactory httpClientFactory, ILogger<SpotPlayerService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<SpotPlayerResponse> GetLicenseAsync(string courseId, string userName, bool isTest = false)
    {
        var url = "https://panel.spotplayer.ir/license/edit/";
        var requestData = new
        {
            test = isTest,
            course = new[] { courseId },
            name = userName,
            watermark = new
            {
                texts = new[]
                {
                        new { text = userName }
                    }
            }
        };

        var apiKey = Environment.GetEnvironmentVariable("SpotPlayerApiKey") ?? "";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("SpotPlayerApiKey is not set");
            return new SpotPlayerResponse
            {
                IsSuccess = false,
                Description = "SpotPlayerApiKey is not set"
            };
        }

        var json = JsonConvert.SerializeObject(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("$API", apiKey);
        _httpClient.DefaultRequestHeaders.Add("$LEVEL", "-1");

        var response = await _httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var licenseResponse = JsonConvert.DeserializeObject<LicenseResponse>(responseContent);
            return new SpotPlayerResponse
            {
                IsSuccess = true,
                Result = licenseResponse
            };
        }
        else
        {
            return new SpotPlayerResponse
            {
                IsSuccess = false,
                Description = $"Error: {response}"
            };
        }
    }
}