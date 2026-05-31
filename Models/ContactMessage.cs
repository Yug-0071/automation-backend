using System;

namespace AutomationBackend.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string InquiryType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = AutomationBackend.Helpers.TimeHelper.GetIST();
        public string Status { get; set; } = "Pending"; // Pending, Resolved, etc.
    }
}
