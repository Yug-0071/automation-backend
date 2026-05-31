using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AutomationBackend.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ImageKey { get; set; }
        public decimal Price { get; set; }
        public List<string>? Features { get; set; }
        public List<DetailedFeatureDto>? DetailedFeatures { get; set; }
        [FromForm]
        public List<FileMetadataDto>? FilesMetadata { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public IFormFile? ImageFile { get; set; }
    }

    public class DetailedFeatureDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class FileMetadataDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string LastUpdated { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    public class ProductFileUploadDto
    {
        public IFormFile File { get; set; } = default!;
    }
}
