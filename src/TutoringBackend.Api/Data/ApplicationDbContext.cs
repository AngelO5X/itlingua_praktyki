using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TutoringBackend.Api.Models;

namespace TutoringBackend.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<TeacherStudentAssignment> TeacherStudentAssignments => Set<TeacherStudentAssignment>();
    public DbSet<ScheduleSlot> ScheduleSlots => Set<ScheduleSlot>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonMaterial> LessonMaterials => Set<LessonMaterial>();
    public DbSet<BalanceTransaction> BalanceTransactions => Set<BalanceTransaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            b.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            b.Property(u => u.Balance).HasColumnType("numeric(10,2)");
        });

        builder.Entity<TeacherStudentAssignment>(b =>
        {
            b.Property(a => a.RatePerLesson).HasColumnType("numeric(10,2)");

            b.HasOne(a => a.Teacher)
                .WithMany()
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Uczeń ma jedno aktywne przypisanie do nauczyciela na raz (zgodnie ze specyfikacją).
            b.HasIndex(a => a.StudentId).IsUnique().HasFilter("\"IsActive\" = true");
        });

        builder.Entity<ScheduleSlot>(b =>
        {
            b.HasOne(s => s.Assignment)
                .WithMany(a => a.ScheduleSlots)
                .HasForeignKey(s => s.TeacherStudentAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(s => new { s.TeacherStudentAssignmentId, s.DayOfWeek });
        });

        builder.Entity<Lesson>(b =>
        {
            b.Property(l => l.Price).HasColumnType("numeric(10,2)");
            b.Property(l => l.Topic).HasMaxLength(200);
            b.Property(l => l.Description).HasMaxLength(2000);
            b.Property(l => l.Status).HasConversion<string>().HasMaxLength(30);

            b.HasOne(l => l.Assignment)
                .WithMany(a => a.Lessons)
                .HasForeignKey(l => l.TeacherStudentAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(l => l.ScheduleSlot)
                .WithMany(s => s.GeneratedLessons)
                .HasForeignKey(l => l.ScheduleSlotId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(l => new { l.TeacherStudentAssignmentId, l.LessonDate });
            // Zapobiega przypadkowemu utworzeniu dwóch lekcji tego samego dnia/godziny dla tej samej pary.
            b.HasIndex(l => new { l.TeacherStudentAssignmentId, l.LessonDate, l.StartTime }).IsUnique();
        });

        builder.Entity<LessonMaterial>(b =>
        {
            b.Property(m => m.Type).HasConversion<string>().HasMaxLength(10);
            b.Property(m => m.Url).HasMaxLength(2000);
            b.Property(m => m.DisplayName).HasMaxLength(200);

            b.HasOne(m => m.Lesson)
                .WithMany(l => l.Materials)
                .HasForeignKey(m => m.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BalanceTransaction>(b =>
        {
            b.Property(t => t.Amount).HasColumnType("numeric(10,2)");
            b.Property(t => t.Type).HasConversion<string>().HasMaxLength(30);
            b.Property(t => t.Note).HasMaxLength(500);

            b.HasOne(t => t.Student)
                .WithMany()
                .HasForeignKey(t => t.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(t => t.Lesson)
                .WithMany()
                .HasForeignKey(t => t.LessonId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(t => t.StudentId);
        });
    }
}
