namespace AutomationBackend.DTOs
{
    public class ContactRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string InquiryType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
