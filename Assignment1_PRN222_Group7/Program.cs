using Assignment1_PRN222_Group7_DAL.Context;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Repositories;
using Assignment1_PRN222_Group7_BLL.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_PRN222_Group7
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ─── Database (EF Core + SQL Server) ─────────────────────────────
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("Assignment1_PRN222_Group7_DAL")
                ));

            // ─── Cookie Authentication (đơn giản, không dùng Identity) ────────
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name        = "ChatbotAuth";
                    options.Cookie.HttpOnly    = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite    = SameSiteMode.Lax;
                    options.LoginPath          = "/Account/Login";
                    options.LogoutPath         = "/Account/Logout";
                    options.AccessDeniedPath   = "/Account/AccessDenied";
                    options.ExpireTimeSpan     = TimeSpan.FromDays(7);
                    options.SlidingExpiration  = true;
                });

            // ─── Authorization với Policies theo Role ─────────────────────────
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly",    p => p.RequireRole("Admin"));
                options.AddPolicy("LecturerUp",   p => p.RequireRole("Admin", "Lecturer"));
                options.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser());
            });

            // ─── Unit of Work & Business Services ──────────────────────────────
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<ISubjectService, SubjectService>();
            builder.Services.AddScoped<IChapterService, ChapterService>();
            builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
            builder.Services.AddScoped<IExperimentService, ExperimentService>();
            builder.Services.AddHttpClient<IMoMoService, MoMoService>();
            builder.Services.AddScoped<IVnPayService, VnPayService>();
            builder.Services.AddHttpClient<IAiService, GeminiService>();
            builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>();
            builder.Services.AddScoped<IChatService, ChatService>();

            // ─── Document & Indexing Services ────────────────────────────────
            builder.Services.AddScoped<ITextExtractorService, TextExtractorService>();
            builder.Services.AddScoped<IChunkingService, ChunkingService>();
            builder.Services.AddScoped<IDocumentService, DocumentService>();
            builder.Services.AddScoped<IDocumentIndexingService, DocumentIndexingService>();
            builder.Services.AddHttpClient<IVectorDbService, ChromaVectorDbService>(client =>
            {
                var chromaUrl = builder.Configuration["ChromaDb:Url"];
                if (string.IsNullOrEmpty(chromaUrl))
                {
                    throw new InvalidOperationException("ChromaDb URL configuration is missing.");
                }
                client.BaseAddress = new Uri(chromaUrl);
            });

            // ─── Background Hosted Services ──────────────────────────────────
            builder.Services.AddHostedService<Assignment1_PRN222_Group7.BackgroundServices.SubscriptionExpirationWorker>();
            builder.Services.AddHostedService<Assignment1_PRN222_Group7.BackgroundServices.ChromaDbStartWorker>();

            // ─── Razor Pages & SignalR ───────────────────────────────────
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();

            var app = builder.Build();

            // ─── Seed dữ liệu ban đầu ─────────────────────────────────────────
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.EnsureCreatedAsync();
                await db.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE object_id = OBJECT_ID(N'[dbo].[Subjects]') AND name = N'LecturerId'
                    )
                    BEGIN
                        ALTER TABLE [dbo].[Subjects] ADD [LecturerId] INT NULL;
                        ALTER TABLE [dbo].[Subjects] ADD CONSTRAINT [FK_Subjects_Users_LecturerId] FOREIGN KEY ([LecturerId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION;
                    END
                ");
                await SeedAsync(db);
            }

            // ─── HTTP Pipeline ────────────────────────────────────────────────
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication(); // Cookie middleware
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages();
            app.MapHub<Assignment1_PRN222_Group7.Hubs.SubjectHub>("/subjectHub");

            await app.RunAsync();
        }

        /// <summary>
        /// Seed Admin user mặc định nếu chưa có.
        /// Mật khẩu được hash bằng BCrypt.
        /// </summary>
        private static async Task SeedAsync(ApplicationDbContext db)
        {
            if (!await db.Users.AnyAsync(u => u.RoleId == 3)) // RoleId=3 là Admin
            {
                db.Users.Add(new User
                {
                    FullName     = "System Administrator",
                    Email        = "admin@chatbot.edu.vn",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    RoleId       = 3,   // Admin
                    IsActive     = true,
                    CreatedAt    = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
