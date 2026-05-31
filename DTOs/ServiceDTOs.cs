using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutomationBackend.DTOs
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public string IconName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new();
        public List<DetailedFeatureDto> DetailedFeatures { get; set; } = new();
        public List<FileMetadataDto> FilesMetadata { get; set; } = new();
        [Required]
        public int CategoryId { get; set; }
    }
}
