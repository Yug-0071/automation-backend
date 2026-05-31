using FluentValidation;
using FluentValidation.AspNetCore;
using Asp.Versioning;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using AutomationBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AutomationBackend.Services.IEmailService, AutomationBackend.Services.EmailService>();


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

//builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();
     
if (!app.Environment.IsDevelopment())
{
    //app.UseHttpsRedirection();
}

// Intercept requests for uploads/downloads/images and serve from DB if they don't exist on Render's ephemeral disk
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (!string.IsNullOrEmpty(path) && 
        (path.StartsWith("/downloads/", StringComparison.OrdinalIgnoreCase) ||
         path.StartsWith("/product-downloads/", StringComparison.OrdinalIgnoreCase) ||
         path.StartsWith("/service-downloads/", StringComparison.OrdinalIgnoreCase) ||
         path.StartsWith("/product-images/", StringComparison.OrdinalIgnoreCase)))
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var physicalPath = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), path.TrimStart('/'));

        if (!System.IO.File.Exists(physicalPath))
        {
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var normalizedPath = path.Replace("\\", "/");
            var dbFile = await dbContext.UploadedFiles.FirstOrDefaultAsync(f => f.FilePath.ToLower() == normalizedPath.ToLower());

            if (dbFile != null)
            {
                context.Response.ContentType = dbFile.ContentType;
                await context.Response.Body.WriteAsync(dbFile.FileData, 0, dbFile.FileData.Length);
                return;
            }
        }
    }
    await next();
});

app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
