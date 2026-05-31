using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System;
using System.Linq;
using System.Threading.Tasks;
using AutomationBackend.DTOs;
using AutomationBackend.Data;
using AutomationBackend.Models;
using AutomationBackend.Services;
using Microsoft.EntityFrameworkCore;

namespace AutomationBackend.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public ContactController(IConfiguration configuration, AppDbContext context, IEmailService emailService)
        {
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendContactEmail([FromBody] ContactRequestDto request)
        {
            try
            {
                // 1. Save to Database
                var contactMessage = new ContactMessage
                {
                    Name = request.Name,
                    Email = request.Email,
                    Mobile = request.Mobile,
                    InquiryType = request.InquiryType,
                    Message = request.Message,
                    CreatedAt = AutomationBackend.Helpers.TimeHelper.GetIST(),
                    Status = "Pending"
                };

                _context.ContactMessages.Add(contactMessage);
                await _context.SaveChangesAsync();

                // 2. Send Email (Optional/Background)
                try 
                {
                    var subject = $"New Contact Inquiry: {request.InquiryType} from {request.Name}";
                    var body = $@"
                        <h3>New Inquiry Details</h3>
                        <p><strong>Name:</strong> {request.Name}</p>
                        <p><strong>Email:</strong> {request.Email}</p>
                        <p><strong>Mobile:</strong> {request.Mobile}</p>
                        <p><strong>Inquiry Type:</strong> {request.InquiryType}</p>
                        <br/>
                        <p><strong>Message:</strong></p>
                        <p>{request.Message.Replace("\n", "<br/>")}</p>
                    ";

                    await _emailService.SendEmailAsync("mahavirautomation111@gmail.com", subject, body);
                } 
                catch (Exception ex) 
                {
                    Console.WriteLine($"Email sending failed but message was saved: {ex.Message}");
                }

                return Ok(new { message = "Your message has been sent successfully!", id = contactMessage.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to send message.", error = ex.Message });
            }
        }

        // Admin Endpoints
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages()
        {
            if (!string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) 
                return StatusCode(403, "Only admins can perform this action");

            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPut("messages/{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            if (!string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) 
                return StatusCode(403, "Only admins can perform this action");

            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound("Message not found");

            message.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated successfully" });
        }

        [HttpDelete("messages/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            if (!string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) 
                return StatusCode(403, "Only admins can perform this action");

            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound("Message not found");

            _context.ContactMessages.Remove(message);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Message deleted successfully" });
        }
    }
}
