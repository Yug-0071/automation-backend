using System;

namespace AutomationBackend.Models
{
    public class DownloadItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // e.g., pdf, zip
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; } = AutomationBackend.Helpers.TimeHelper.GetIST();
        public byte[]? FileData { get; set; }
    }
}
