using EnterprisePlatform.Api.Extensions;
using EnterprisePlatform.Api.Filters;
using EnterprisePlatform.Api.Infrastructure;
using EnterprisePlatform.Api.Middleware;
using EnterprisePlatform.Core.Options;
using EnterprisePlatform.Repository;
using EnterprisePlatform.Service;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlatformFileLogging(builder.Configuration);
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.SectionName));
builder.Services.AddScoped<IAvatarStorageService, LocalAvatarStorageService>();
builder.Services.AddScoped<PermissionAuthorizeFilter>();
builder.Services.AddControllers(options =>
{
    // Action 过滤器：在 TokenAuthorizeFilter（Authorization 阶段）之后执行
    options.Filters.Add<PermissionAuthorizeFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddPlatformSwagger();
builder.Services.AddPlatformAuthentication(builder.Configuration);
builder.Services.AddPlatformRequestSign(builder.Configuration);
builder.Services.AddPlatformCors(builder.Configuration);
builder.Services.AddRepositoryLayer(builder.Configuration);
builder.Services.AddServiceLayer();

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestSignatureMiddleware>();
app.UseMiddleware<OperationLogMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EnterprisePlatform API v1");
        options.DocumentTitle = "EnterprisePlatform API";
    });
}

app.UseHttpsRedirection();
app.UseCors("PlatformCors");

var fileStorageOptions = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
var uploadsPhysicalPath = Path.Combine(app.Environment.ContentRootPath, fileStorageOptions.UploadRootPath);
Directory.CreateDirectory(uploadsPhysicalPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPhysicalPath),
    RequestPath = "/uploads"
});

app.MapControllers();

app.Run();
