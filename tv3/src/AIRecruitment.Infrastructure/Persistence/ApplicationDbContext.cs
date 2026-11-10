using AIRecruitment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIRecruitment.Infrastructure.Persistence;

// ============== 
// TV2 + TV3 — DbSet, khóa ngoại, index và constraint cho dữ liệu Sprint 2.
// ==============
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<CVAnalysis> CVAnalyses => Set<CVAnalysis>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<RecruiterProfile> RecruiterProfiles => Set<RecruiterProfile>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CV> CVs => Set<CV>();
    // TV2 — Sprint 3: saved jobs.
    public DbSet<SavedJob> SavedJobs => Set<SavedJob>();
    // TV4 — Luồng ứng tuyển và phỏng vấn.
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Interview> Interviews => Set<Interview>();
    // TV5 — Truy vết phiên bản AI.
    public DbSet<AIModelVersion> AIModelVersions => Set<AIModelVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>(e => { e.ToTable("Users"); e.HasKey(x => x.Id); e.Property(x => x.Email).HasMaxLength(256).IsRequired(); e.HasIndex(x => x.Email).IsUnique(); e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired(); e.Property(x => x.Role).HasMaxLength(30).IsRequired(); e.ToTable(t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Candidate','Recruiter','Admin')")); });
        modelBuilder.Entity<CandidateProfile>(e => { e.ToTable("CandidateProfiles"); e.HasKey(x => x.Id); e.Property(x => x.FullName).HasMaxLength(150).IsRequired(); e.Property(x => x.Phone).HasMaxLength(40); e.Property(x => x.Location).HasMaxLength(200); e.Property(x => x.Summary).HasMaxLength(3000); e.Property(x => x.SkillsCsv).HasMaxLength(2000); e.HasIndex(x => x.UserId).IsUnique(); e.HasOne(x => x.User).WithOne(x => x.CandidateProfile).HasForeignKey<CandidateProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<RecruiterProfile>(e => { e.ToTable("RecruiterProfiles"); e.HasKey(x => x.Id); e.Property(x => x.FullName).HasMaxLength(150).IsRequired(); e.Property(x => x.Phone).HasMaxLength(40); e.Property(x => x.Position).HasMaxLength(150); e.HasIndex(x => x.UserId).IsUnique(); e.HasOne(x => x.User).WithOne(x => x.RecruiterProfile).HasForeignKey<RecruiterProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Company>(e => { e.ToTable("Companies"); e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(200).IsRequired(); e.Property(x => x.Website).HasMaxLength(500); e.Property(x => x.Industry).HasMaxLength(150); e.Property(x => x.Description).HasMaxLength(4000); e.Property(x => x.Location).HasMaxLength(200); e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.OwnerUserId, x.Name }); });
        modelBuilder.Entity<CV>(e => { e.ToTable("CVs"); e.HasKey(x => x.Id); e.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired(); e.Property(x => x.StoredFileName).HasMaxLength(100).IsRequired(); e.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired(); e.Property(x => x.ContentType).HasMaxLength(150).IsRequired(); e.HasOne(x => x.CandidateProfile).WithMany(x => x.CVs).HasForeignKey(x => x.CandidateProfileId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.CandidateProfileId, x.UploadedAtUtc }); });
        modelBuilder.Entity<Job>(e => { e.ToTable("Jobs"); e.HasKey(x => x.Id); e.Property(x => x.Title).HasMaxLength(200).IsRequired(); e.Property(x => x.Description).HasMaxLength(8000).IsRequired(); e.Property(x => x.Location).HasMaxLength(200); e.Property(x => x.EmploymentType).HasMaxLength(50); e.Property(x => x.RequiredSkillsCsv).HasMaxLength(2000); e.HasIndex(x => new { x.IsPublished, x.CreatedAtUtc }); e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Company).WithMany(x => x.Jobs).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.SetNull); });
        modelBuilder.Entity<CVAnalysis>(e => { e.ToTable("CVAnalyses"); e.HasKey(x => x.Id); e.Property(x => x.ParserVersion).HasMaxLength(100).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).IsRequired(); e.Property(x => x.Education).HasMaxLength(500); e.HasOne(x => x.CV).WithMany().HasForeignKey(x => x.CVId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.CVId, x.CreatedAtUtc }); });
        modelBuilder.Entity<MatchResult>(e => { e.ToTable("MatchResults"); e.HasKey(x => x.Id); e.Property(x => x.MatchScore).HasPrecision(5, 2); e.Property(x => x.ModelVersion).HasMaxLength(150).IsRequired(); e.ToTable(t => t.HasCheckConstraint("CK_MatchResults_MatchScore", "[MatchScore] >= 0 AND [MatchScore] <= 100")); e.HasIndex(x => new { x.CVId, x.JobId, x.CreatedAtUtc }); e.HasOne(x => x.CV).WithMany().HasForeignKey(x => x.CVId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.AIModelVersion).WithMany().HasForeignKey(x => x.AIModelVersionId).OnDelete(DeleteBehavior.SetNull); });
        // ============== 
        // TV5 — Version catalog for explainability and auditability.
        // ==============
        modelBuilder.Entity<AIModelVersion>(e => { e.ToTable("AIModelVersions"); e.HasKey(x => x.Id); e.Property(x => x.Version).HasMaxLength(150).IsRequired(); e.HasIndex(x => x.Version).IsUnique(); e.Property(x => x.Algorithm).HasMaxLength(150).IsRequired(); e.Property(x => x.DatasetDescription).HasMaxLength(1000); e.Property(x => x.MetricsJson).HasMaxLength(4000); });
        // ============== 
        // TV4 — Application uniqueness and ownership relations.
        // ==============
        modelBuilder.Entity<JobApplication>(e => { e.ToTable("JobApplications"); e.HasKey(x => x.Id); e.Property(x => x.Status).HasMaxLength(40).IsRequired(); e.Property(x => x.CoverLetter).HasMaxLength(4000); e.HasIndex(x => new { x.CandidateProfileId, x.JobId }).IsUnique(); e.HasIndex(x => new { x.JobId, x.Status, x.AppliedAtUtc }); e.HasOne(x => x.CandidateProfile).WithMany().HasForeignKey(x => x.CandidateProfileId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.CV).WithMany().HasForeignKey(x => x.CVId).OnDelete(DeleteBehavior.SetNull); });
        modelBuilder.Entity<Interview>(e => { e.ToTable("Interviews"); e.HasKey(x => x.Id); e.Property(x => x.Status).HasMaxLength(30).IsRequired(); e.Property(x => x.MeetingUrl).HasMaxLength(1000); e.Property(x => x.Location).HasMaxLength(300); e.Property(x => x.Notes).HasMaxLength(3000); e.HasIndex(x => new { x.ApplicationId, x.ScheduledAtUtc }); e.HasOne(x => x.Application).WithMany(x => x.Interviews).HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict); });
        // ==============
        // TV2 — Unique saved-job per candidate/job pair; cascade when profile/job is removed.
        // ==============
        modelBuilder.Entity<SavedJob>(e => { e.ToTable("SavedJobs"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CandidateProfileId, x.JobId }).IsUnique(); e.HasOne(x => x.CandidateProfile).WithMany().HasForeignKey(x => x.CandidateProfileId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade); });
    }
}
