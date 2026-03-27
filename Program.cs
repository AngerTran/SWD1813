using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SWD1813.Configuration;
using SWD1813.Models;
using SWD1813.Repositories;
using SWD1813.Hubs;
using SWD1813.Services.Implementations;
using SWD1813.Services.Interfaces;

namespace SWD1813
{
    public class Program
    {
        public static async System.Threading.Tasks.Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? builder.Configuration.GetConnectionString("MvcMovieContext");
            builder.Services.AddDbContext<ProjectManagementContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            builder.Services.Configure<JiraIntegrationOptions>(
                builder.Configuration.GetSection(JiraIntegrationOptions.SectionName));
            builder.Services.Configure<GitHubIntegrationOptions>(
                builder.Configuration.GetSection(GitHubIntegrationOptions.SectionName));
            builder.Services.Configure<IntegrationAutoSyncOptions>(
                builder.Configuration.GetSection(IntegrationAutoSyncOptions.SectionName));

            builder.Services.AddHttpClient("Jira", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(2);
            });
            builder.Services.AddHttpClient("GitHub", client =>
            {
                client.BaseAddress = new Uri("https://api.github.com/");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SWD1813-Integration/1.0");
                client.Timeout = TimeSpan.FromMinutes(2);
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.ExpireTimeSpan = TimeSpan.FromDays(14);
                    options.SlidingExpiration = true;
                });
            builder.Services.AddAuthorization();

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IGroupService, GroupService>();
            builder.Services.AddScoped<IProjectService, ProjectService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<ISrsService, SrsService>();
            builder.Services.AddScoped<IIntegrationSyncService, IntegrationSyncService>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IReportContentService, ReportContentService>();
            builder.Services.AddHttpClient();
            builder.Services.AddHostedService<IntegrationAutoSyncHostedService>();

            builder.Services.AddSignalR();
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
                await auth.EnsureSeedAdminAsync();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await DatabaseSchemaEnsure.EnsureChatMessagesTableAsync(unitOfWork);
                await SampleCommitsSeeder.EnsureAsync(
                    scope.ServiceProvider.GetRequiredService<IRepository<Project>>(),
                    scope.ServiceProvider.GetRequiredService<IRepository<Repository>>(),
                    scope.ServiceProvider.GetRequiredService<IRepository<Commit>>(),
                    unitOfWork);
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.MapHub<ProjectChatHub>("/hubs/projectchat");

            await app.RunAsync();
        }
    }
}
