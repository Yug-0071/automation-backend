using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Asp.Versioning;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using AutomationBackend.Data;
using AutomationBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using AutomationBackend.DTOs;

namespace AutomationBackend.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public ServicesController(AppDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        [HttpGet]
        public IActionResult GetServices()
        {
            var services = _context.Services.ToList();
            var result = services.Select(s => MapToDto(s)).ToList();
            return Ok(result);
        }

        [HttpPost]
        public IActionResult UpdateServices([FromBody] List<ServiceDto> services)
        {
            if (services == null) return BadRequest("Invalid services data");
            if (!string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) return StatusCode(403, "Only admins can perform this action");

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Clear existing services
                _context.Services.RemoveRange(_context.Services);
                _context.SaveChanges();

                // Add new services
                foreach (var s in services)
                {
                    _context.Services.Add(new ServiceItem
                    {
                        IconName = s.IconName,
                        Title = s.Title,
                        Description = s.Description,
                        FeaturesJson = JsonSerializer.Serialize(s.Features),
                        DetailedFeaturesJson = JsonSerializer.Serialize(s.DetailedFeatures),
                        FileMetadataJson = JsonSerializer.Serialize(s.FilesMetadata ?? new List<FileMetadataDto>()),
                        CategoryId = s.CategoryId
                    });
                }
                _context.SaveChanges();
                transaction.Commit();
                return Ok();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("add")]
        public IActionResult AddService([FromBody] ServiceDto s)
        {
            if (s == null) return BadRequest("Service data is required");
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");

            var service = new ServiceItem
            {
                IconName = s.IconName,
                Title = s.Title,
                Description = s.Description,
                FeaturesJson = JsonSerializer.Serialize(s.Features ?? new List<string>()),
                DetailedFeaturesJson = JsonSerializer.Serialize(s.DetailedFeatures ?? new List<DetailedFeatureDto>()),
                FileMetadataJson = JsonSerializer.Serialize(s.FilesMetadata ?? new List<FileMetadataDto>()),
                CategoryId = s.CategoryId
            };
            _context.Services.Add(service);
            _context.SaveChanges();

            return Ok(MapToDto(service));
        }

        [HttpPut("{id}")]
        public IActionResult UpdateService(int id, [FromBody] ServiceDto s)
        {
            if (s == null) return BadRequest("Service data is required");
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");

            var existing = _context.Services.Find(id);
            if (existing == null) return NotFound();

            existing.IconName = s.IconName;
            existing.Title = s.Title;
            existing.Description = s.Description;
            existing.FeaturesJson = JsonSerializer.Serialize(s.Features ?? new List<string>());
            
            // Only update detailed features if they are provided, to prevent accidental overwrite
            if (s.DetailedFeatures != null && s.DetailedFeatures.Count > 0)
            {
                existing.DetailedFeaturesJson = JsonSerializer.Serialize(s.DetailedFeatures);
            }

            // Only update file metadata if it's provided with a valid path to avoid overwriting with empty defaults
            if (s.FilesMetadata != null)
            {
                existing.FileMetadataJson = JsonSerializer.Serialize(s.FilesMetadata);
            }

            existing.CategoryId = s.CategoryId;

            _context.SaveChanges();
            return Ok(MapToDto(existing));
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteService(int id)
        {
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");
            var service = _context.Services.Find(id);
            if (service == null) return NotFound();

            _context.Services.Remove(service);
            _context.SaveChanges();
            return Ok(new { message = "Service deleted successfully" });
        }

        [HttpPost("{id}/upload-file")]
        public async Task<IActionResult> UploadServiceFile(int id, [FromForm] ProductFileUploadDto dto)
        {
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");
            
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            if (dto.File == null || dto.File.Length == 0) return BadRequest(new { message = "No file uploaded." });
            
            var ext = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".zip") return BadRequest(new { message = "Only PDF and ZIP files are allowed." });

            var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(rootPath, "service-downloads");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var uniqueName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(dto.File.FileName);
            var filePath = Path.Combine(folder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            // Save file bytes persistently in the database SQL Server for Render
            using (var memoryStream = new MemoryStream())
            {
                await dto.File.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var dbFile = new UploadedFile
                {
                    FilePath = "/service-downloads/" + uniqueName,
                    FileData = fileBytes,
                    ContentType = ext == ".pdf" ? "application/pdf" : "application/zip"
                };
                _context.UploadedFiles.Add(dbFile);
            }

            var metadata = new FileMetadataDto
            {
                FileName = dto.File.FileName,
                FileSize = (dto.File.Length / 1024 / 1024.0).ToString("0.00") + " MB",
                LastUpdated = AutomationBackend.Helpers.TimeHelper.GetIST().ToString("MMMM dd, yyyy"),
                FilePath = "/service-downloads/" + uniqueName
            };
            
            var files = new List<FileMetadataDto>();
            if (!string.IsNullOrWhiteSpace(service.FileMetadataJson) && service.FileMetadataJson.Trim().StartsWith("["))
            {
                files = JsonSerializer.Deserialize<List<FileMetadataDto>>(service.FileMetadataJson) ?? new List<FileMetadataDto>();
            }
            else if (!string.IsNullOrWhiteSpace(service.FileMetadataJson) && service.FileMetadataJson.Trim().StartsWith("{"))
            {
                var single = JsonSerializer.Deserialize<FileMetadataDto>(service.FileMetadataJson);
                if (single != null && !string.IsNullOrEmpty(single.FilePath)) files.Add(single);
            }
            files.Add(metadata);

            service.FileMetadataJson = JsonSerializer.Serialize(files);
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "File uploaded successfully.", metadata = metadata });
        }

        [HttpDelete("{id}/delete-file")]
        public async Task<IActionResult> DeleteServiceFile(int id, [FromQuery] string filePath)
        {
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");
            
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(service.FileMetadataJson) && service.FileMetadataJson.Trim().StartsWith("["))
            {
                var files = JsonSerializer.Deserialize<List<FileMetadataDto>>(service.FileMetadataJson) ?? new List<FileMetadataDto>();
                var fileToRemove = files.FirstOrDefault(f => f.FilePath == filePath);
                
                if (fileToRemove != null)
                {
                    var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var fullPath = Path.Combine(rootPath, fileToRemove.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

                    files.Remove(fileToRemove);
                    service.FileMetadataJson = JsonSerializer.Serialize(files);
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "File deleted successfully." });
                }
            }

            return NotFound("File not found.");
        }

        [HttpGet("{id}/download-file")]
        public async Task<IActionResult> DownloadServiceFile(int id, [FromQuery] string filePath)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            if (string.IsNullOrWhiteSpace(service.FileMetadataJson)) return NotFound("No files attached to this service.");
            
            var files = new List<FileMetadataDto>();
            if (service.FileMetadataJson.Trim().StartsWith("["))
            {
                files = JsonSerializer.Deserialize<List<FileMetadataDto>>(service.FileMetadataJson) ?? new List<FileMetadataDto>();
            }

            var meta = files.FirstOrDefault(f => f.FilePath == filePath);
            if (meta == null || string.IsNullOrEmpty(meta.FilePath)) return NotFound("File not found in service metadata.");

            var ext = Path.GetExtension(meta.FilePath).ToLowerInvariant();
            var contentType = ext == ".pdf" ? "application/pdf" : "application/zip";

            // Attempt to serve from the database first (Render persistent overlay)
            var dbFile = await _context.UploadedFiles.FirstOrDefaultAsync(f => f.FilePath.ToLower() == meta.FilePath.ToLower());
            if (dbFile != null)
            {
                return File(dbFile.FileData, contentType, meta.FileName);
            }

            // Fallback to local disk (development / legacy files)
            var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(rootPath, meta.FilePath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath)) return NotFound("File not found on disk or database.");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, contentType, meta.FileName);
        }

        private object MapToDto(ServiceItem s)
        {
            List<string> features = new List<string>();
            try {
                if (!string.IsNullOrWhiteSpace(s.FeaturesJson) && s.FeaturesJson.Trim().StartsWith("["))
                    features = JsonSerializer.Deserialize<List<string>>(s.FeaturesJson, _jsonOptions) ?? new List<string>();
            } catch { }

            List<DetailedFeatureDto> detailedFeatures = new List<DetailedFeatureDto>();
            try {
                if (!string.IsNullOrWhiteSpace(s.DetailedFeaturesJson) && s.DetailedFeaturesJson.Trim().StartsWith("["))
                    detailedFeatures = JsonSerializer.Deserialize<List<DetailedFeatureDto>>(s.DetailedFeaturesJson, _jsonOptions) ?? new List<DetailedFeatureDto>();
            } catch { }

            List<FileMetadataDto> filesMetadata = new List<FileMetadataDto>();
            try {
                if (!string.IsNullOrWhiteSpace(s.FileMetadataJson) && s.FileMetadataJson.Trim().StartsWith("["))
                    filesMetadata = JsonSerializer.Deserialize<List<FileMetadataDto>>(s.FileMetadataJson, _jsonOptions) ?? new List<FileMetadataDto>();
                else if (!string.IsNullOrWhiteSpace(s.FileMetadataJson) && s.FileMetadataJson.Trim().StartsWith("{"))
                {
                    var single = JsonSerializer.Deserialize<FileMetadataDto>(s.FileMetadataJson, _jsonOptions);
                    if (single != null && !string.IsNullOrEmpty(single.FilePath)) filesMetadata.Add(single);
                }
            } catch { }

            return new
            {
                id = s.Id,
                iconName = s.IconName,
                title = s.Title,
                description = s.Description,
                features = features,
                detailedFeatures = detailedFeatures,
                filesMetadata = filesMetadata,
                categoryId = s.CategoryId
            };
        }

    }
}

