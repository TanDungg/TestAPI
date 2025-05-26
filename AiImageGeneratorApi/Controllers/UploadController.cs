using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.IO;
using AiImageGeneratorApi.Models.DTOs;
using System.Text;
using AiImageGeneratorApi.Models.Entities;
using AiImageGeneratorApi;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private static readonly object LockObj = new();

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File rỗng");

        var saved = await SaveFileAsync(file);
        return Ok(saved);
    }

    [HttpPost("multi")]
    public async Task<IActionResult> UploadMultiple(List<IFormFile> lstFiles)
    {
        if (lstFiles == null || !lstFiles.Any())
            return BadRequest("Không có file nào được gửi");

        var results = new List<UploadFile>();
        foreach (var file in lstFiles)
        {
            var saved = await SaveFileAsync(file);
            results.Add(saved);
        }

        return Ok(results);
    }

    private async Task<UploadFile> SaveFileAsync(IFormFile file)
    {
        lock (LockObj)
        {
            var now = DateTime.Now;
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var fileName = $"{timestamp}_{Commons.TiengVietKhongDau(file.FileName)}";

            var relativePath = Path.Combine("Uploads", now.Year.ToString(), now.Month.ToString());
            var fullFolderPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

            if (!Directory.Exists(fullFolderPath))
                Directory.CreateDirectory(fullFolderPath);

            var fullPath = Path.Combine(fullFolderPath, fileName);
            using var stream = new FileStream(fullPath, FileMode.Create);
            file.CopyTo(stream);

            return new UploadFile
            {
                FileName = file.FileName,
                Path = "/" + Path.Combine(relativePath, fileName).Replace("\\", "/")
            };
        }
    }
}
