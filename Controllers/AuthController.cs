using Asp.Versioning;
using AutomationBackend.Data;
using AutomationBackend.DTOs;
using AutomationBackend.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace AutomationBackend.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly AutomationBackend.Services.IEmailService _emailService;

        public AuthController(AppDbContext context, IConfiguration configuration, AutomationBackend.Services.IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;

            //// Get Admin credentials from configuration
            //var adminEmail = _configuration["AdminCredentials:Email"] ?? "mahavirautomation111@gmail.com";
            //var adminPassword = _configuration["AdminCredentials:Password"] ?? "M9#xT4";

            //// Remove old admin if it exists
            //var oldAdmin = _context.Users.FirstOrDefault(u => u.Email == "admin@gmail.com");
            //if (oldAdmin != null)
            //{
            //    _context.Users.Remove(oldAdmin);
            //    _context.SaveChanges();
            //}

            //// Seed or Update the new Admin
            //var currentAdmins = _context.Users.Where(u => u.Email.ToLower() == adminEmail.ToLower()).ToList();
            //if (!currentAdmins.Any())
            //{
            //    _context.Users.Add(new User
            //    {
            //        FirstName = "Mahavir",
            //        LastName = "Admin",
            //        Email = adminEmail,
            //        Password = adminPassword,
            //        Role = "Admin",
            //        UserGuid = Guid.NewGuid().ToString(),
            //        Address = "123 Admin St, Tech City, Automation Land",
            //        EmailVerified = true
            //    });
            //    _context.SaveChanges();
            //}
            //else
            //{
            //    var adminToKeep = currentAdmins.First();

            //    // If there are multiple records for the same email, remove duplicates
            //    if (currentAdmins.Count > 1)
            //    {
            //        _context.Users.RemoveRange(currentAdmins.Skip(1));
            //        _context.SaveChanges();
            //    }

            //    adminToKeep.EmailVerified = true;

            //    // Force update the password just to ensure it's set correctly
            //    if (adminToKeep.Password != adminPassword)
            //    {
            //        adminToKeep.Password = adminPassword;
            //        _context.SaveChanges();
            //    }
            //}
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (_context.Users.Any(u => u.Email == model.Email || u.MobileNumber == model.Mobile))
            {
                return BadRequest(new { message = "User already exists" });
            }

            var token = Guid.NewGuid().ToString("N");

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                MobileNumber = model.Mobile,
                Password = model.Password, // Actual password entered by user
                UserGuid = Guid.NewGuid().ToString(),
                Role = "User",
                EmailVerified = false,
                VerificationToken = token,
                VerificationTokenExpiry = AutomationBackend.Helpers.TimeHelper.GetIST().AddHours(24)
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Send Verification Email
            try
            {
                var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5017";
                string verifyLink = $"{frontendUrl.TrimEnd('/')}/verify-email?token={token}";
                string subject = "Email Verification Link";
                string body = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                        <p>Hello {user.FirstName},</p>
                        <p>Thank you for registering. Please click the button below to verify your email address:</p>
                        <p style='margin: 30px 0;'>
                            <a href='{verifyLink}' style='background-color: #10b981; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold;'>Verify Email</a>
                        </p>
                        <p>This link will expire in 24 hours.</p>
                        <p>Thank you,<br/>Support Team</p>
                    </div>";

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                // Graceful fallback for Render/SMTP port blocks: Auto-verify user so they can log in immediately
                user.EmailVerified = true;
                user.VerificationToken = null;
                user.VerificationTokenExpiry = null;
                await _context.SaveChangesAsync();

                Console.WriteLine($"SMTP PORT BLOCKED / ERROR: {ex.Message}. User {user.Email} has been auto-verified as a fallback.");

                return Ok(new { 
                    message = "Registration successful! (Auto-verified due to temporary email server limitations). You can log in now.", 
                    userGuid = user.UserGuid 
                });
            }

            return Ok(new { message = "Registration successful. Please check your email to verify your account.", userGuid = user.UserGuid });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto model)
        {
            if (model == null) return BadRequest(new { message = "Invalid request" });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var mobileOrEmail = model.MobileOrEmail?.Trim() ?? string.Empty;
            var password = model.Password?.Trim() ?? string.Empty;

            var user = _context.Users.FirstOrDefault(u => 
                (u.Email != null && u.Email.ToLower() == mobileOrEmail.ToLower()) || u.MobileNumber == mobileOrEmail);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email/mobile or password" });
            }

            bool isPasswordValid = false;
            if (user.Password == password)
            {
                isPasswordValid = true;
            }
            else
            {
                try
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
                }
                catch (Exception)
                {
                    isPasswordValid = false;
                }
            }

            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Invalid email/mobile or password" });
            }

            if (!user.EmailVerified && user.Role != "Admin") 
            {
                return Unauthorized(new { message = "Please verify your email before login." });
            }

            user.LastLogin = AutomationBackend.Helpers.TimeHelper.GetIST();
            _context.SaveChanges();

            return Ok(new 
            { 
                message = "Login successful",
                role = user.Role,
                token = "fake-jwt-token-" + user.UserGuid,
                user = new {
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    mobileNumber = user.MobileNumber,
                    address = user.Address,
                    userGuid = user.UserGuid
                }
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            if (string.IsNullOrEmpty(model.Email)) return BadRequest(new { message = "Email is required" });

            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower());
            if (user == null)
            {
                // To prevent email enumeration, you might want to return Ok anyway, 
                // but usually for internal apps/specific requests we return error.
                return BadRequest(new { message = "User with this email does not exist" });
            }

            // Security: Limit resend attempts (e.g., 1 minute between sends)
            if (user.LastOtpSentAt.HasValue && (AutomationBackend.Helpers.TimeHelper.GetIST() - user.LastOtpSentAt.Value).TotalMinutes < 1)
            {
                return BadRequest(new { message = "Please wait a minute before requesting another password reset link" });
            }

            // Generate a secure token
            var token = Guid.NewGuid().ToString("N");
            
            user.ResetOtpToken = token;
            user.OtpExpiry = AutomationBackend.Helpers.TimeHelper.GetIST().AddMinutes(15); // 15 minutes expiry
            user.LastOtpSentAt = AutomationBackend.Helpers.TimeHelper.GetIST();
            
            _context.SaveChanges();

            // Send Email
            try
            {
                var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
                string resetLink = $"{frontendUrl.TrimEnd('/')}/reset-password?token={token}";
                string subject = "Password Reset Verification Link";
                string body = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                        <p>Hello,</p>
                        <p>We received a request to reset your password.</p>
                        <p>Click the button below to create a new password:</p>
                        <p style='margin: 30px 0;'>
                            <a href='{resetLink}' style='background-color: #3498db; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold;'>Reset Password</a>
                        </p>
                        <p>This link will expire in 15 minutes for security reasons.</p>
                        <p>If you did not request a password reset, you can safely ignore this email.</p>
                        <p>Thank you,<br/>Support Team</p>
                    </div>";

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                // In production, log the exception
                return StatusCode(500, new { message = $"SMTP Error: {ex.Message} (Check your appsettings.json password!)", error = ex.Message });
            }

            return Ok(new { message = "Password reset link sent to your email" });
        }

        [HttpGet("verify-email")]
        public IActionResult VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest(new { message = "Token is required" });

            var user = _context.Users.FirstOrDefault(u => u.VerificationToken == token);
            if (user == null) return BadRequest(new { message = "Invalid or expired token" });

            if (user.VerificationTokenExpiry < AutomationBackend.Helpers.TimeHelper.GetIST())
            {
                return BadRequest(new { message = "Verification token has expired. Please register again or request a new link." });
            }

            user.EmailVerified = true;
            user.VerificationToken = null;
            user.VerificationTokenExpiry = null;
            
            _context.SaveChanges();

            return Ok(new { message = "Email verified successfully" });
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.NewPassword))
                return BadRequest(new { message = "Token and new password are required" });

            if (model.NewPassword != model.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            var user = _context.Users.FirstOrDefault(u => u.ResetOtpToken == model.Token);

            if (user == null || string.IsNullOrEmpty(user.ResetOtpToken)) 
                return BadRequest(new { message = "Invalid or expired token" });

            if (user.OtpExpiry < AutomationBackend.Helpers.TimeHelper.GetIST())
            {
                // Token has expired
                user.ResetOtpToken = null;
                user.OtpExpiry = null;
                _context.SaveChanges();
                return BadRequest(new { message = "Token has expired. Please request a new password reset link." });
            }

            // Save the new password in plain text
            user.Password = model.NewPassword;
            
            // Clear token data after successful reset
            user.ResetOtpToken = null;
            user.OtpExpiry = null;
            
            _context.SaveChanges();

            return Ok(new { message = "Password has been reset successfully. You can now login with your new password." });
        }
    }
}
