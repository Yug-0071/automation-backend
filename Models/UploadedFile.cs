using System;

namespace AutomationBackend.Models
{
    public class UploadedFile
    {
        public int Id { get; set; }
        public string FilePath { get; set; } = string.Empty; // e.g. "/downloads/unique-guid_filename.pdf"
        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = AutomationBackend.Helpers.TimeHelper.GetIST();
    }
}
