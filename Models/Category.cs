using System.ComponentModel.DataAnnotations;

namespace AutomationBackend.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string CategoryType { get; set; } = "Product"; // "Product" or "Service"
    }
}
