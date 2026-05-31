using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using AutomationBackend.Data;
using AutomationBackend.Models;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.AspNetCore.Hosting;
using AutomationBackend.DTOs;

namespace AutomationBackend.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DownloadController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DownloadController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult GetDownloads()
        {
            return Ok(_context.Downloads.OrderByDescending(d => d.UploadDate).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> AddDownload([FromForm] DownloadUploadDto dto)
        {
            try
            {
                // 🔒 Role check
                var role = Request.Headers["X-User-Role"].ToString();
                if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                    return StatusCode(403, "Only admins can perform this action");

                // 📁 File validation
                if (dto.File == null || dto.File.Length == 0)
                    return BadRequest(new { message = "No file uploaded." });

                var ext = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
                if (ext != ".pdf" && ext != ".zip")
                    return BadRequest(new { message = "Only PDF and ZIP files are allowed." });

                // 📦 Read file ONCE (IMPORTANT for Render stability)
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await dto.File.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                // 🧠 Create DB file record (backup storage)
                var dbFile = new UploadedFile
                {
                    FilePath = "/downloads/" + Guid.NewGuid() + "_" + Path.GetFileName(dto.File.FileName),
                    FileData = fileBytes,
                    ContentType = ext == ".pdf" ? "application/pdf" : "application/zip"
                };

                _context.UploadedFiles.Add(dbFile);

                // 📄 Main download record
                var download = new DownloadItem
                {
                    Name = dto.Name ?? "Untitled",
                    Description = dto.Description ?? "",
                    FilePath = dbFile.FilePath,
                    FileType = ext.Replace(".", ""),
                    FileSize = dto.File.Length,
                    UploadDate = AutomationBackend.Helpers.TimeHelper.GetIST(),
                    FileData = fileBytes
                };

                _context.Downloads.Add(download);

                // 💾 Save to DB
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Upload successful",
                    download
                });
            }
            catch (Exception ex)
            {
                // 🔥 This will now show real Render error
                return StatusCode(500, new
                {
                    message = "Server error",
                    error = ex.Message
                });
            }
        }

        [HttpGet("file/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var download = await _context.Downloads.FindAsync(id);
            if (download == null) return NotFound("Download record not found");

            var contentType = "application/octet-stream";
            if (download.FileType.ToLower() == "pdf") contentType = "application/pdf";
            if (download.FileType.ToLower() == "zip") contentType = "application/zip";

            // Attempt to serve from the database first (Render persistent overlay)
            var dbFile = await _context.UploadedFiles.FirstOrDefaultAsync(f => f.FilePath.ToLower() == download.FilePath.ToLower());
            if (dbFile != null)
            {
                return File(dbFile.FileData, contentType, download.Name + "." + download.FileType);
            }

            // Fallback to local disk (development / legacy files)
            var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(rootPath, download.FilePath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath)) return NotFound("File not found on disk or database");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

            return File(fileBytes, contentType, download.Name + "." + download.FileType);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDownload(int id)
        {
            if (!string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) return StatusCode(403, "Only admins can perform this action");
            
            var download = await _context.Downloads.FindAsync(id);
            if (download == null) return NotFound();

            var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(rootPath, download.FilePath.TrimStart('/'));

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            _context.Downloads.Remove(download);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Download deleted successfully" });
        }
    }
}
