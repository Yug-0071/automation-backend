using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.Collections.Generic;
using System.Linq;
using AutomationBackend.Data;
using AutomationBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutomationBackend.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetCategories([FromQuery] string? type = null)
        {
            var query = _context.Categories.AsQueryable();
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(c => c.CategoryType == type);
            }
            return Ok(query.ToList());
        }

        [HttpPost]
        public IActionResult UpdateCategories([FromBody] List<Category> categories)
        {
            if (!string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) return StatusCode(403, "Only admins can perform this action");
            if (categories == null) return BadRequest("Categories list cannot be null");

            var incomingIds = categories.Where(c => c.Id > 0).Select(c => c.Id).ToList();
            var toRemove = _context.Categories.Where(c => !incomingIds.Contains(c.Id)).ToList();
            if (toRemove.Any())
            {
                _context.Categories.RemoveRange(toRemove);
            }

            foreach (var cat in categories)
            {
                if (cat.Id > 0)
                {
                    var existing = _context.Categories.FirstOrDefault(c => c.Id == cat.Id);
                    if (existing != null)
                    {
                        existing.Name = cat.Name;
                        existing.ParentId = cat.ParentId;
                        existing.CategoryType = cat.CategoryType;
                        _context.Categories.Update(existing);
                    }
                }
                else
                {
                    _context.Categories.Add(new Category { Name = cat.Name, ParentId = cat.ParentId, CategoryType = cat.CategoryType });
                }
            }

            _context.SaveChanges();
            return Ok(_context.Categories.ToList());
        }

        [HttpPost("add")]
        public IActionResult AddCategory([FromBody] Category category)
        {
            if (!string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) return StatusCode(403, "Only admins can perform this action");
            _context.Categories.Add(category);
            _context.SaveChanges();
            return Ok(category);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateCategory(int id, [FromBody] Category category)
        {
            if (!string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) return StatusCode(403, "Only admins can perform this action");
            var existing = _context.Categories.Find(id);
            if (existing == null) return NotFound();

            existing.Name = category.Name;
            existing.ParentId = category.ParentId;
            existing.CategoryType = category.CategoryType;
            _context.SaveChanges();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteCategory(int id)
        {
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();

            var children = _context.Categories.Where(c => c.ParentId == id).ToList();
            if (children.Any())
            {
                _context.Categories.RemoveRange(children);
            }

            _context.Categories.Remove(category);
            _context.SaveChanges();
            return Ok(new { message = "Category and its children deleted successfully" });
        }

    }
}

