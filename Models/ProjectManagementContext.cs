using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SWD1813.Models;

public partial class ProjectManagementContext : DbContext
{
    public ProjectManagementContext()
    {
    }

    public ProjectManagementContext(DbContextOptions<ProjectManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApiIntegration> ApiIntegrations { get; set; }

    public virtual DbSet<Commit> Commits { get; set; }

    public virtual DbSet<ContributorStat> ContributorStats { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<GroupMember> GroupMembers { get; set; }

    public virtual DbSet<JiraIssue> JiraIssues { get; set; }

    public virtual DbSet<Lecturer> Lecturers { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Repository> Repositories { get; set; }

    public virtual DbSet<Sprint> Sprints { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer("Server=.;Database=swp391_project_management;Trusted_Connection=True;TrustServerCertificate=True");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiIntegration>(entity =>
        {
            entity.HasKey(e => e.IntegrationId).HasName("PK__api_inte__B403D887F56342C5");

            entity.ToTable("api_integrations");

            entity.Property(e => e.IntegrationId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("integration_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.GithubToken).HasColumnName("github_token");
            entity.Property(e => e.JiraToken).HasColumnName("jira_token");
            entity.Property(e => e.ProjectId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("project_id");

            entity.HasOne(d => d.Project).WithMany(p => p.ApiIntegrations)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK__api_integ__proje__06CD04F7");
        });

        modelBuilder.Entity<Commit>(entity =>
        {
            entity.HasKey(e => e.CommitId).HasName("PK__commits__1C807873FB86E5C7");

            entity.ToTable("commits");

            entity.Property(e => e.CommitId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("commit_id");
            entity.Property(e => e.Additions).HasColumnName("additions");
            entity.Property(e => e.AuthorEmail)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("author_email");
            entity.Property(e => e.AuthorName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("author_name");
            entity.Property(e => e.CommitDate)
                .HasColumnType("datetime")
                .HasColumnName("commit_date");
            entity.Property(e => e.Deletions).HasColumnName("deletions");
            entity.Property(e => e.FilesChanged).HasColumnName("files_changed");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.RepoId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("repo_id");

            entity.HasOne(d => d.Repo).WithMany(p => p.Commits)
                .HasForeignKey(d => d.RepoId)
                .HasConstraintName("FK__commits__repo_id__778AC167");
        });

        modelBuilder.Entity<ContributorStat>(entity =>
        {
            entity.HasKey(e => e.StatId).HasName("PK__contribu__B8A52560876E2F63");

            entity.ToTable("contributor_stats");

            entity.Property(e => e.StatId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("stat_id");
            entity.Property(e => e.LastCommit)
                .HasColumnType("datetime")
                .HasColumnName("last_commit");
            entity.Property(e => e.RepoId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("repo_id");
            entity.Property(e => e.TotalAdditions)
                .HasDefaultValue(0)
                .HasColumnName("total_additions");
            entity.Property(e => e.TotalCommits)
                .HasDefaultValue(0)
                .HasColumnName("total_commits");
            entity.Property(e => e.TotalDeletions)
                .HasDefaultValue(0)
                .HasColumnName("total_deletions");
            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Repo).WithMany(p => p.ContributorStats)
                .HasForeignKey(d => d.RepoId)
                .HasConstraintName("FK__contribut__repo___7E37BEF6");

            entity.HasOne(d => d.User).WithMany(p => p.ContributorStats)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__contribut__user___7D439ABD");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.GroupId).HasName("PK__groups__D57795A0A10388A7");

            entity.ToTable("groups");

            entity.Property(e => e.GroupId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("group_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.GroupName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("group_name");
            entity.Property(e => e.LecturerId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("lecturer_id");

            entity.HasOne(d => d.Lecturer).WithMany(p => p.Groups)
                .HasForeignKey(d => d.LecturerId)
                .HasConstraintName("FK__groups__lecturer__5DCAEF64");
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__group_me__3213E83FE9BCBDC5");

            entity.ToTable("group_members");

            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.GroupId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("group_id");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("joined_at");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK__group_mem__group__619B8048");

            entity.HasOne(d => d.User).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__group_mem__user___628FA481");
        });

        modelBuilder.Entity<JiraIssue>(entity =>
        {
            entity.HasKey(e => e.IssueId).HasName("PK__jira_iss__D6185C3980FAD455");

            entity.ToTable("jira_issues");

            entity.Property(e => e.IssueId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("issue_id");
            entity.Property(e => e.Assignee)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("assignee");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IssueKey)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("issue_key");
            entity.Property(e => e.IssueType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("issue_type");
            entity.Property(e => e.Priority)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("priority");
            entity.Property(e => e.ProjectId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("project_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Project).WithMany(p => p.JiraIssues)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK__jira_issu__proje__6D0D32F4");
        });

        modelBuilder.Entity<Lecturer>(entity =>
        {
            entity.HasKey(e => e.LecturerId).HasName("PK__lecturer__D4D1DAB15638DEF7");

            entity.ToTable("lecturers");

            entity.HasIndex(e => e.UserId, "UQ__lecturer__B9BE370EB3C91E72").IsUnique();

            entity.Property(e => e.LecturerId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("lecturer_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Department)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("department");
            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Lecturer)
                .HasForeignKey<Lecturer>(d => d.UserId)
                .HasConstraintName("FK__lecturers__user___59FA5E80");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__projects__BC799E1FC8A80DF7");

            entity.ToTable("projects");

            entity.Property(e => e.ProjectId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("project_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.GroupId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("group_id");
            entity.Property(e => e.JiraProjectKey)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("jira_project_key");
            entity.Property(e => e.ProjectName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("project_name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");

            entity.HasOne(d => d.Group).WithMany(p => p.Projects)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK__projects__group___66603565");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__reports__779B7C589DA9364A");

            entity.ToTable("reports");

            entity.Property(e => e.ReportId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("report_id");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("file_url");
            entity.Property(e => e.GeneratedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("generated_at");
            entity.Property(e => e.GeneratedBy)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("generated_by");
            entity.Property(e => e.ProjectId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("project_id");
            entity.Property(e => e.ReportType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("report_type");

            entity.HasOne(d => d.GeneratedByNavigation).WithMany(p => p.Reports)
                .HasForeignKey(d => d.GeneratedBy)
                .HasConstraintName("FK__reports__generat__02FC7413");

            entity.HasOne(d => d.Project).WithMany(p => p.Reports)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK__reports__project__02084FDA");
        });

        modelBuilder.Entity<Repository>(entity =>
        {
            entity.HasKey(e => e.RepoId).HasName("PK__reposito__E2D3BC802CCE93E8");

            entity.ToTable("repositories");

            entity.Property(e => e.RepoId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("repo_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.GithubOwner)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("github_owner");
            entity.Property(e => e.ProjectId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("project_id");
            entity.Property(e => e.RepoName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("repo_name");
            entity.Property(e => e.RepoUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("repo_url");

            entity.HasOne(d => d.Project).WithMany(p => p.Repositories)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK__repositor__proje__6A30C649");
        });

        modelBuilder.Entity<Sprint>(entity =>
        {
            entity.HasKey(e => e.SprintId).HasName("PK__sprints__396C18028DF547C0");

            entity.ToTable("sprints");

            entity.Property(e => e.SprintId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("sprint_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.ProjectId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("project_id");
            entity.Property(e => e.SprintName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("sprint_name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");

            entity.HasOne(d => d.Project).WithMany(p => p.Sprints)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK__sprints__project__74AE54BC");
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__tasks__0492148D0EA24106");

            entity.ToTable("tasks");

            entity.Property(e => e.TaskId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("task_id");
            entity.Property(e => e.AssignedTo)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("assigned_to");
            entity.Property(e => e.Deadline).HasColumnName("deadline");
            entity.Property(e => e.IssueId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("issue_id");
            entity.Property(e => e.Progress)
                .HasDefaultValue(0)
                .HasColumnName("progress");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.AssignedToNavigation).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.AssignedTo)
                .HasConstraintName("FK__tasks__assigned___71D1E811");

            entity.HasOne(d => d.Issue).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.IssueId)
                .HasConstraintName("FK__tasks__issue_id__70DDC3D8");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370F99F0A1FB");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "UQ__users__AB6E616418C614EB").IsUnique();

            entity.Property(e => e.UserId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("full_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
