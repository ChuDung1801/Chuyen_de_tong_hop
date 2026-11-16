using System.Text;
using AIRecruitment.Application.Interfaces;
using AIRecruitment.Application.Services;
using AIRecruitment.Infrastructure.AI;
using AIRecruitment.Infrastructure.Persistence;
using AIRecruitment.Infrastructure.Repositories;
using AIRecruitment.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// ============== 
// TV1 — JWT authentication/authorization.
// TV2/TV3 — Register Sprint 2 services in DI.
// TV4 — CORS for local React development.
// ==============
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
    builder.Services.AddScoped<IJobRepository, JobRepository>();
}
else
{
    builder.Services.AddSingleton<IJobRepository, InMemoryJobRepository>();
}

builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRecruiterAccessService, RecruiterAccessService>();
builder.Services.AddSingleton<IRecruiterVerificationStorage, LocalRecruiterVerificationStorage>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ICompanyJobService, CompanyJobService>();
builder.Services.AddScoped<ISprint3JobService, Sprint3JobService>();
builder.Services.AddScoped<ISavedJobService, SavedJobService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<ICvAnalysisService, CvAnalysisService>();
builder.Services.AddHttpClient<IMLServiceClient, MLServiceClient>(client =>
{
    var baseUrl = builder.Configuration["AIService:BaseUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(12);
});
builder.Services.AddSingleton<ICvStorage, LocalCvStorage>();
builder.Services.AddHttpClient<IMatchingClient, MatchingClient>(client =>
{
    var baseUrl = builder.Configuration["AIService:BaseUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
});

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (!string.IsNullOrWhiteSpace(jwtSecret) && Encoding.UTF8.GetByteCount(jwtSecret) >= 32)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
}
else
{
    throw new InvalidOperationException("Cần cấu hình Jwt:Secret/JWT_SECRET có ít nhất 32 byte trong môi trường.");
}
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("VerifiedRecruiterOrAdmin", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("Admin") ||
        (context.User.IsInRole("Recruiter") && context.User.HasClaim("recruiter_verified", "true"))));
    options.AddPolicy("CandidateOrVerifiedRecruiterOrAdmin", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("Candidate") || context.User.IsInRole("Admin") ||
        (context.User.IsInRole("Recruiter") && context.User.HasClaim("recruiter_verified", "true"))));
});
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { name = "AI Recruitment Platform API", sprint = 5 }));
app.MapControllers();
app.Run();

public sealed class InMemoryJobRepository : IJobRepository
{
    private readonly List<AIRecruitment.Domain.Entities.Job> _jobs = [];
    public Task<IReadOnlyList<AIRecruitment.Domain.Entities.Job>> GetPublishedAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AIRecruitment.Domain.Entities.Job>>(_jobs.Where(j => j.IsPublished).ToArray());
    public Task<AIRecruitment.Domain.Entities.Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(_jobs.FirstOrDefault(j => j.Id == id));
    public Task<AIRecruitment.Domain.Entities.Job> AddAsync(AIRecruitment.Domain.Entities.Job job, CancellationToken cancellationToken = default) { job.Id = _jobs.Count + 1; _jobs.Add(job); return Task.FromResult(job); }
}
