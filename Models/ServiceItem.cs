using System.ComponentModel.DataAnnotations;

namespace AutomationBackend.Models
{
    public class ServiceItem
    {
        public int Id { get; set; }
        public string IconName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FeaturesJson { get; set; } = "[]";
        public string DetailedFeaturesJson { get; set; } = "[]";
        public string FileMetadataJson { get; set; } = "{}";
        [Required]
        public int CategoryId { get; set; }
    }
}
