using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace AiImageGeneratorApi.Services
{
    public class HuggingFaceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public HuggingFaceService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["HuggingFace:ApiKey"] ?? throw new ArgumentNullException("HuggingFace:ApiKey is missing in configuration");
            _apiUrl = "https://api-inference.huggingface.co/models/stabilityai/stable-diffusion-xl-base-1.0"; // Sử dụng mô hình được hỗ trợ
            //_apiUrl = "https://api-inference.huggingface.co/models/Lykon/dreamshaper-7"; // Sử dụng mô hình được hỗ trợ
        }

        public async Task<string> GenerateImageAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Prompt cannot be empty or null", nameof(prompt));

            var payload = new
            {
                inputs = prompt,
                parameters = new
                {
                    negative_prompt = "blurry, low quality, distorted",
                    num_inference_steps = 50,
                    guidance_scale = 7.5
                },
                options = new { wait_for_model = true }
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync(_apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error generating image: {response.ReasonPhrase}. Details: {errorContent}");
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            var base64Image = Convert.ToBase64String(imageBytes);
            return $"data:image/png;base64,{base64Image}";
        }
    }
}