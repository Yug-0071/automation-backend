using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using AutomationBackend.Data;
using AutomationBackend.Models;
using System;
using System.Linq;

namespace AutomationBackend.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            if (!string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) return StatusCode(403, "Only admins can perform this action");

            var totalUsers = _context.Users.Count();
            var newUsers24h = _context.Users.Count(u => u.CreatedAt >= AutomationBackend.Helpers.TimeHelper.GetIST().AddHours(-24));
            
            // Real pending requests count
            var pendingRequests = _context.ContactMessages.Count(m => m.Status == "Pending"); 

            return Ok(new
            {
                totalUsers,
                newUsers24h,
                pendingRequests
            });
        }

        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            if (!string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) return StatusCode(403, "Only admins can perform this action");

            var users = _context.Users
                .OrderBy(u => u.Id)
                .ToList()
                .Select((u, index) => new
            {
                id = u.Id,
                displayId = index + 1,
                name = $"{u.FirstName} {u.LastName}",
                email = u.Email,
                mobileNumber = u.MobileNumber,
                password = u.Password,
                role = u.Role,
                userGuid = u.UserGuid,
                createdAt = u.CreatedAt,
                lastLogin = u.LastLogin,
                status = "Active" // Mock status
            }).ToList();

            return Ok(users);
        }

        [HttpDelete("users/{id}")]
        public IActionResult DeleteUser(int id)
        {
            if (!string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) return StatusCode(403, "Only admins can perform this action");

            var user = _context.Users.Find(id);
            if (user == null) return NotFound("User not found");

            // Prevent deleting any admin
            if (user.Role == "Admin") return StatusCode(403, "Administrators cannot be deleted");

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok(new { message = "User deleted successfully" });
        }
    }
}
