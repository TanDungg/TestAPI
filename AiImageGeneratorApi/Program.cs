using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AiImageGeneratorApi.Data;
using AiImageGeneratorApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using AiImageGeneratorApi.Interfaces;
using AiImageGeneratorApi.Hubs;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ======= Cơ sở dữ liệu & Dịch vụ ==========
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Chuỗi kết nối 'Default' không được tìm thấy.")));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<HuggingFaceService>(); // Đăng ký HuggingFaceService
builder.Services.AddHttpClient(); // Kích hoạt IHttpClientFactory

// ======= Xác thực: JWT Bearer ==========
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            //OnMessageReceived = context =>
            //{
            //    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            //    if (!string.IsNullOrEmpty(authHeader))
            //    {
            //        context.Token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            //            ? authHeader["Bearer ".Length..]
            //            : authHeader;
            //    }
            //    return Task.CompletedTask;
            //}
            OnMessageReceived = context =>
            {
                // 👇 Thêm đoạn này để hỗ trợ SignalR qua query string
                var accessToken = context.Request.Query["access_token"];

                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                else
                {
                    // Giữ đoạn này để Swagger và API thường vẫn hoạt động
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader))
                    {
                        context.Token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                            ? authHeader["Bearer ".Length..]
                            : authHeader;
                    }
                }

                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key không có trong cấu hình")))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// ======= Bộ điều khiển & Swagger ==========
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Tiêu đề xác thực JWT sử dụng Bearer scheme. Ví dụ: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ======= Cấu hình IIS Integration ==========
builder.WebHost.ConfigureKestrel(options =>
{
    // Cấu hình để chạy ứng dụng trên IIS với Kestrel (máy chủ trong ứng dụng)
    options.ConfigureEndpointDefaults(lo => lo.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
});

builder.WebHost.UseIISIntegration(); // Kết nối với IIS

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

var app = builder.Build();
// Use CORS
app.UseCors("AllowFrontend");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "");
    c.RoutePrefix = string.Empty;
    c.DisplayRequestDuration();
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Uploads")),
    RequestPath = "/Uploads",
    OnPrepareResponse = ctx =>
    {
        var origin = ctx.Context.Request.Headers["Origin"].ToString();
        if (!string.IsNullOrWhiteSpace(origin))
        {
            ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            ctx.Context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
        }
    }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseDeveloperExceptionPage();
app.MapControllers();
app.MapHub<ChatHub>("/chatHub");
app.Run();
