using System.ComponentModel.DataAnnotations;

namespace AutomationBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string UserGuid { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = AutomationBackend.Helpers.TimeHelper.GetIST();
        public DateTime? LastLogin { get; set; }
        public string? ResetOtp { get; set; }
        public DateTime? OtpExpiry { get; set; }
        public int OtpAttempts { get; set; } = 0;
        public DateTime? LastOtpSentAt { get; set; }
        public string? ResetOtpToken { get; set; } // Added to verify state after OTP success
        public bool EmailVerified { get; set; } = false;
        public string? VerificationToken { get; set; }
        public DateTime? VerificationTokenExpiry { get; set; }
    }
}
