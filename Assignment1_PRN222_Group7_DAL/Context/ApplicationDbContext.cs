using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_PRN222_Group7_DAL.Context
{
    /// <summary>
    /// DbContext thuần — không dùng ASP.NET Identity.
    /// Authentication bằng Cookie Schema (custom).
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ─── Tables ───────────────────────────────────────────────────────────
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<MessageSource> MessageSources { get; set; }
        public DbSet<Experiment> Experiments { get; set; }
        public DbSet<ExperimentConfiguration> ExperimentConfigurations { get; set; }
        public DbSet<TestQuestion> TestQuestions { get; set; }
        public DbSet<ExperimentResult> ExperimentResults { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ─── Role ─────────────────────────────────────────────────────────
            builder.Entity<Role>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(50).IsRequired();
                e.Property(x => x.NormalizedName).HasMaxLength(50).IsRequired();
                e.Property(x => x.Description).HasMaxLength(200);
                e.HasIndex(x => x.NormalizedName).IsUnique();

                // Seed 3 roles mặc định
                e.HasData(
                    new Role { Id = 1, Name = "Student",  NormalizedName = "STUDENT",  Description = "Sinh viên" },
                    new Role { Id = 2, Name = "Lecturer", NormalizedName = "LECTURER", Description = "Giảng viên" },
                    new Role { Id = 3, Name = "Admin",    NormalizedName = "ADMIN",    Description = "Quản trị viên" }
                );
            });

            // ─── User ─────────────────────────────────────────────────────────
            builder.Entity<User>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
                e.Property(x => x.Email).HasMaxLength(256).IsRequired();
                e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
                e.Property(x => x.StudentOrStaffId).HasMaxLength(50);
                e.HasIndex(x => x.Email).IsUnique();
                e.HasOne(x => x.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── SubscriptionPlan ─────────────────────────────────────────────
            builder.Entity<SubscriptionPlan>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(50).IsRequired();
                e.Property(x => x.Price).HasColumnType("decimal(18,2)");
                e.HasIndex(x => x.Tier).IsUnique();

                // Seed: Free, Basic, Premium
                e.HasData(
                    new SubscriptionPlan
                    {
                        Id = 1, Tier = SubscriptionTier.Free, Name = "Free",
                        Description = "Dùng thử miễn phí",
                        MaxDocumentsUpload = 5, MaxChatsPerDay = 10,
                        MaxMessagesPerSession = 20, MaxSubjectsAccess = 1,
                        Price = 0, IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = 2, Tier = SubscriptionTier.Basic, Name = "Basic",
                        Description = "Gói cơ bản cho sinh viên",
                        MaxDocumentsUpload = 50, MaxChatsPerDay = 100,
                        MaxMessagesPerSession = 50, MaxSubjectsAccess = 5,
                        Price = 99000, IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = 3, Tier = SubscriptionTier.Premium, Name = "Premium",
                        Description = "Không giới hạn cho giảng viên & nghiên cứu",
                        MaxDocumentsUpload = -1, MaxChatsPerDay = -1,
                        MaxMessagesPerSession = -1, MaxSubjectsAccess = -1,
                        Price = 299000, IsActive = true
                    }
                );
            });

            // ─── UserSubscription ─────────────────────────────────────────────
            builder.Entity<UserSubscription>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.User)
                    .WithMany(u => u.Subscriptions)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Plan)
                    .WithMany(p => p.UserSubscriptions)
                    .HasForeignKey(x => x.PlanId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ScheduledPlan)
                    .WithMany()
                    .HasForeignKey(x => x.ScheduledPlanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── Subject ──────────────────────────────────────────────────────
            builder.Entity<Subject>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(20).IsRequired();
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();
                e.HasOne(x => x.Creator)
                    .WithMany(u => u.CreatedSubjects)
                    .HasForeignKey(x => x.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.Lecturer)
                    .WithMany(u => u.ManagedSubjects)
                    .HasForeignKey(x => x.LecturerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── Chapter ──────────────────────────────────────────────────────
            builder.Entity<Chapter>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).HasMaxLength(300).IsRequired();
                e.HasOne(x => x.Subject)
                    .WithMany(s => s.Chapters)
                    .HasForeignKey(x => x.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── Document ─────────────────────────────────────────────────────
            builder.Entity<Document>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).HasMaxLength(500).IsRequired();
                e.Property(x => x.OriginalFileName).HasMaxLength(500).IsRequired();
                e.Property(x => x.StoredFileName).HasMaxLength(500).IsRequired();
                e.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
                e.HasOne(x => x.Subject)
                    .WithMany(s => s.Documents)
                    .HasForeignKey(x => x.SubjectId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Chapter)
                    .WithMany(c => c.Documents)
                    .HasForeignKey(x => x.ChapterId)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.Uploader)
                    .WithMany(u => u.UploadedDocuments)
                    .HasForeignKey(x => x.UploadedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── DocumentChunk ────────────────────────────────────────────────
            builder.Entity<DocumentChunk>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired();
                e.Property(x => x.EmbeddingId).HasMaxLength(200);
                e.Property(x => x.EmbeddingModel).HasMaxLength(100);
                e.HasOne(x => x.Document)
                    .WithMany(d => d.Chunks)
                    .HasForeignKey(x => x.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.DocumentId, x.ChunkIndex });
            });

            // ─── ChatSession ──────────────────────────────────────────────────
            builder.Entity<ChatSession>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).HasMaxLength(500);
                e.HasOne(x => x.User)
                    .WithMany(u => u.ChatSessions)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Subject)
                    .WithMany(s => s.ChatSessions)
                    .HasForeignKey(x => x.SubjectId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── ChatMessage ──────────────────────────────────────────────────
            builder.Entity<ChatMessage>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired();
                e.HasOne(x => x.Session)
                    .WithMany(s => s.Messages)
                    .HasForeignKey(x => x.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── MessageSource ────────────────────────────────────────────────
            builder.Entity<MessageSource>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.CitedContent).HasColumnType("nvarchar(max)");
                e.HasOne(x => x.Message)
                    .WithMany(m => m.Sources)
                    .HasForeignKey(x => x.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Chunk)
                    .WithMany(c => c.MessageSources)
                    .HasForeignKey(x => x.ChunkId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── Experiment ───────────────────────────────────────────────────
            builder.Entity<Experiment>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(300).IsRequired();
                e.HasOne(x => x.Subject)
                    .WithMany(s => s.Experiments)
                    .HasForeignKey(x => x.SubjectId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Creator)
                    .WithMany(u => u.Experiments)
                    .HasForeignKey(x => x.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── ExperimentConfiguration ──────────────────────────────────────
            builder.Entity<ExperimentConfiguration>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ConfigName).HasMaxLength(200).IsRequired();
                e.HasOne(x => x.Experiment)
                    .WithMany(ex => ex.Configurations)
                    .HasForeignKey(x => x.ExperimentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── TestQuestion ─────────────────────────────────────────────────
            builder.Entity<TestQuestion>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Question).HasColumnType("nvarchar(max)").IsRequired();
                e.Property(x => x.GroundTruth).HasColumnType("nvarchar(max)").IsRequired();
                e.HasOne(x => x.Experiment)
                    .WithMany(ex => ex.TestQuestions)
                    .HasForeignKey(x => x.ExperimentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── ExperimentResult ─────────────────────────────────────────────
            builder.Entity<ExperimentResult>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.GeneratedAnswer).HasColumnType("nvarchar(max)");
                e.Property(x => x.RetrievedContexts).HasColumnType("nvarchar(max)");
                e.HasOne(x => x.Experiment)
                    .WithMany(ex => ex.Results)
                    .HasForeignKey(x => x.ExperimentId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Configuration)
                    .WithMany(c => c.Results)
                    .HasForeignKey(x => x.ConfigId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Question)
                    .WithMany(q => q.Results)
                    .HasForeignKey(x => x.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}
