using Microsoft.AspNetCore.Http;

namespace AutomationBackend.DTOs
{
    public class DownloadUploadDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IFormFile File { get; set; } = default!;
    }
}
