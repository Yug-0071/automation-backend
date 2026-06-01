using Asp.Versioning;
using AutomationBackend.Data;
using AutomationBackend.DTOs;
using AutomationBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;

namespace AutomationBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public ProductController(AppDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public class JsonModelBinder : IModelBinder
        {
            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));
                var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
                if (valueProviderResult == ValueProviderResult.None) return Task.CompletedTask;
                bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
                var value = valueProviderResult.FirstValue;
                if (string.IsNullOrEmpty(value)) return Task.CompletedTask;
                try
                {
                    var result = JsonSerializer.Deserialize(value, bindingContext.ModelType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (result != null)
                    {
                        bindingContext.Result = ModelBindingResult.Success(result);
                    }
                }
                catch
                {
                    bindingContext.Result = ModelBindingResult.Failed();
                }
                return Task.CompletedTask;
            }
        }

        [HttpGet]
        public IActionResult GetProducts()
        {
            var products = _context.Products.ToList();
            var result = products.Select(p => MapToDto(p)).ToList();
            return Ok(result);
        }

        [HttpPost]
        public IActionResult UpdateProducts([FromBody] List<ProductDto> products)
        {
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");
            _context.Products.ExecuteDelete();
            foreach (var p in products)
            {
                _context.Products.Add(new Product
                {
                    Title = p.Title ?? "",
                    Description = p.Description ?? "",
                    ImageKey = p.ImageKey ?? "",
                    Price = p.Price,
                    CategoryId = p.CategoryId,
                    FeaturesJson = JsonSerializer.Serialize(p.Features ?? new List<string>()),
                    DetailedFeaturesJson = JsonSerializer.Serialize(p.DetailedFeatures ?? new List<DetailedFeatureDto>()),
                    FileMetadataJson = JsonSerializer.Serialize(p.FilesMetadata ?? new List<FileMetadataDto>())
                });
            }
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddProduct([FromForm] ProductDto p,
            [FromForm, ModelBinder(BinderType = typeof(JsonModelBinder))] List<string>? features,
            [FromForm, ModelBinder(BinderType = typeof(JsonModelBinder))] List<DetailedFeatureDto>? detailedFeatures,
            [FromForm, ModelBinder(BinderType = typeof(JsonModelBinder))] List<FileMetadataDto>? filesMetadata)
        {
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");

            string? imagePath = p.ImageKey;
            if (p.ImageFile != null && p.ImageFile.Length > 0)
            {
                imagePath = await HandleImageUpload(p.ImageFile);
            }

            var product = new Product
            {
                Title = p.Title ?? "",
                Description = p.Description ?? "",
                ImageKey = imagePath ?? "",
                Price = p.Price,
                CategoryId = p.CategoryId,
                FeaturesJson = features != null ? JsonSerializer.Serialize(features) : "[]",
                DetailedFeaturesJson = detailedFeatures != null ? JsonSerializer.Serialize(detailedFeatures) : "[]",
                FileMetadataJson = filesMetadata != null ? JsonSerializer.Serialize(filesMetadata) : "[]"
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(MapToDto(product));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDto p,
            [FromForm, ModelBinder(BinderType = typeof(JsonModelBinder))] List<string>? features,
            [FromForm, ModelBinder(BinderType = typeof(JsonModelBinder))] List<DetailedFeatureDto>? detailedFeatures,
            [FromForm, ModelBinder(BinderType = typeof(JsonModelBinder))] List<FileMetadataDto>? filesMetadata)
        {
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");
            var existing = await _context.Products.FindAsync(id);
            if (existing == null) return NotFound();

            if (p.ImageFile != null && p.ImageFile.Length > 0)
            {
                // Delete old image if it exists
                if (!string.IsNullOrEmpty(existing.ImageKey) && existing.ImageKey.StartsWith("/product-images/"))
                {
                    var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var oldPath = Path.Combine(rootPath, existing.ImageKey.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }
                existing.ImageKey = await HandleImageUpload(p.ImageFile);
            }
            else if (!string.IsNullOrEmpty(p.ImageKey))
            {
                existing.ImageKey = p.ImageKey;
            }

            existing.Title = p.Title ?? existing.Title ?? "";
            existing.Description = p.Description ?? existing.Description ?? "";
            existing.Price = p.Price;
            existing.CategoryId = p.CategoryId;

            if (features != null)
                existing.FeaturesJson = JsonSerializer.Serialize(features);

            if (detailedFeatures != null)
                existing.DetailedFeaturesJson = JsonSerializer.Serialize(detailedFeatures);

            if (filesMetadata != null)
            {
                existing.FileMetadataJson = JsonSerializer.Serialize(filesMetadata);
            }

            await _context.SaveChangesAsync();
            return Ok(MapToDto(existing));
        }

      private async Task<string> HandleImageUpload(IFormFile file)
{
    var uniqueName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);

    using var memoryStream = new MemoryStream();

    await file.CopyToAsync(memoryStream);

    var fileBytes = memoryStream.ToArray();

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

    var contentType = "image/png";

    if (ext == ".jpg" || ext == ".jpeg")
        contentType = "image/jpeg";
    else if (ext == ".gif")
        contentType = "image/gif";
    else if (ext == ".svg")
        contentType = "image/svg+xml";
    else if (ext == ".webp")
        contentType = "image/webp";

    var dbFile = new UploadedFile
    {
        FilePath = "/product-images/" + uniqueName,
        FileData = fileBytes,
        ContentType = contentType
    };

    _context.UploadedFiles.Add(dbFile);

    await _context.SaveChangesAsync();

    return "/product-images/" + uniqueName;
}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Delete image file if it exists
            if (!string.IsNullOrEmpty(product.ImageKey) && product.ImageKey.StartsWith("/product-images/"))
            {
                var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var fullPath = Path.Combine(rootPath, product.ImageKey.TrimStart('/'));
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Product deleted successfully" });
        }

        [HttpPost("{id}/upload-file")]
        public async Task<IActionResult> UploadProductFile(int id, [FromForm] ProductFileUploadDto dto)
        {
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (dto.File == null || dto.File.Length == 0) return BadRequest(new { message = "No file uploaded." });

            var ext = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".zip") return BadRequest(new { message = "Only PDF and ZIP files are allowed." });

            var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(rootPath, "product-downloads");
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
                    FilePath = "/product-downloads/" + uniqueName,
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
                FilePath = "/product-downloads/" + uniqueName
            };

            var files = new List<FileMetadataDto>();
            if (!string.IsNullOrWhiteSpace(product.FileMetadataJson) && product.FileMetadataJson.Trim().StartsWith("["))
            {
                files = JsonSerializer.Deserialize<List<FileMetadataDto>>(product.FileMetadataJson) ?? new List<FileMetadataDto>();
            }
            else if (!string.IsNullOrWhiteSpace(product.FileMetadataJson) && product.FileMetadataJson.Trim().StartsWith("{"))
            {
                var single = JsonSerializer.Deserialize<FileMetadataDto>(product.FileMetadataJson);
                if (single != null && !string.IsNullOrEmpty(single.FilePath)) files.Add(single);
            }
            files.Add(metadata);

            product.FileMetadataJson = JsonSerializer.Serialize(files);

            await _context.SaveChangesAsync();
            return Ok(new { message = "File uploaded successfully.", metadata = metadata });
        }

        [HttpDelete("{id}/delete-file")]
        public async Task<IActionResult> DeleteProductFile(int id, [FromQuery] string filePath)
        {
            if (Request.Headers["X-User-Role"] != "Admin") return StatusCode(403, "Only admins can perform this action");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(product.FileMetadataJson) && product.FileMetadataJson.Trim().StartsWith("["))
            {
                var files = JsonSerializer.Deserialize<List<FileMetadataDto>>(product.FileMetadataJson) ?? new List<FileMetadataDto>();
                var fileToRemove = files.FirstOrDefault(f => f.FilePath == filePath);
                
                if (fileToRemove != null)
                {
                    var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var fullPath = Path.Combine(rootPath, fileToRemove.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

                    files.Remove(fileToRemove);
                    product.FileMetadataJson = JsonSerializer.Serialize(files);
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "File deleted successfully." });
                }
            }

            return NotFound("File not found.");
        }

        [HttpGet("{id}/download-file")]
        public async Task<IActionResult> DownloadProductFile(int id, [FromQuery] string filePath)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (string.IsNullOrWhiteSpace(product.FileMetadataJson)) return NotFound("No files attached to this product.");

            var files = new List<FileMetadataDto>();
            if (product.FileMetadataJson.Trim().StartsWith("["))
            {
                files = JsonSerializer.Deserialize<List<FileMetadataDto>>(product.FileMetadataJson) ?? new List<FileMetadataDto>();
            }

            var meta = files.FirstOrDefault(f => f.FilePath == filePath);
            if (meta == null || string.IsNullOrEmpty(meta.FilePath)) return NotFound("File not found in product metadata.");

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

        private object MapToDto(Product p)
        {
            List<string> features = new List<string>();
            try
            {
                if (!string.IsNullOrWhiteSpace(p.FeaturesJson) && p.FeaturesJson.Trim().StartsWith("["))
                    features = JsonSerializer.Deserialize<List<string>>(p.FeaturesJson) ?? new List<string>();
            }
            catch { }

            List<DetailedFeatureDto> detailedFeatures = new List<DetailedFeatureDto>();
            try
            {
                if (!string.IsNullOrWhiteSpace(p.DetailedFeaturesJson) && p.DetailedFeaturesJson.Trim().StartsWith("["))
                    detailedFeatures = JsonSerializer.Deserialize<List<DetailedFeatureDto>>(p.DetailedFeaturesJson) ?? new List<DetailedFeatureDto>();
            }
            catch { }

            List<FileMetadataDto> filesMetadata = new List<FileMetadataDto>();
            try
            {
                if (!string.IsNullOrWhiteSpace(p.FileMetadataJson) && p.FileMetadataJson.Trim().StartsWith("["))
                    filesMetadata = JsonSerializer.Deserialize<List<FileMetadataDto>>(p.FileMetadataJson) ?? new List<FileMetadataDto>();
                else if (!string.IsNullOrWhiteSpace(p.FileMetadataJson) && p.FileMetadataJson.Trim().StartsWith("{"))
                {
                    var single = JsonSerializer.Deserialize<FileMetadataDto>(p.FileMetadataJson);
                    if (single != null && !string.IsNullOrEmpty(single.FilePath)) filesMetadata.Add(single);
                }
            }
            catch { }

            return new
            {
                id = p.Id,
                title = p.Title,
                description = p.Description,
                imageKey = p.ImageKey,
                price = p.Price,
                categoryId = p.CategoryId,
                features = features,
                detailedFeatures = detailedFeatures,
                filesMetadata = filesMetadata
            };
        }

    }
}

