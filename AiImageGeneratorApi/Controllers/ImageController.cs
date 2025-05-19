using AiImageGeneratorApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AiImageGeneratorApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ImageController : ControllerBase
    {
        private readonly HuggingFaceService _huggingFaceService;

        public ImageController(HuggingFaceService huggingFaceService)
        {
            _huggingFaceService = huggingFaceService;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateImage([FromBody] ImageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { Error = "Prompt is required." });
            }

            try
            {
                var imageBase64 = await _huggingFaceService.GenerateImageAsync(request.Prompt);
                return Ok(new { ImageData = imageBase64 });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { Error = $"Failed to generate image: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Internal server error: {ex.Message}" });
            }
        }
    }

    public class ImageRequest
    {
        [Required(ErrorMessage = "Prompt is required")]
        [StringLength(500, ErrorMessage = "Prompt cannot exceed 500 characters")]
        public string Prompt { get; set; }
    }
}