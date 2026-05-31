using System.ComponentModel.DataAnnotations;

namespace AutomationBackend.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageKey { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string FeaturesJson { get; set; } = "[]";
        public string DetailedFeaturesJson { get; set; } = "[]";
        public string FileMetadataJson { get; set; } = "{}";
        [Required]
        public int CategoryId { get; set; }
    }
}
