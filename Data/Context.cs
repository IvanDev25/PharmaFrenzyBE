using Api.Constant;
using Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace Api.Data
{
    public class Context : IdentityDbContext<User>
    {
        public Context(DbContextOptions<Context> options) : base(options)
        {
        }

        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<ExamAttemptAnswer> ExamAttemptAnswers { get; set; }
        public DbSet<StudentModulePoint> StudentModulePoints { get; set; }
        public DbSet<StudentRankingBadge> StudentRankingBadges { get; set; }
        public DbSet<RankingPeriodAward> RankingPeriodAwards { get; set; }
        public DbSet<StudentWithdrawalRequest> StudentWithdrawalRequests { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<StudentSubscription> StudentSubscriptions { get; set; }
        public DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }
        public DbSet<QuestionSetAccess> QuestionSetAccesses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>()
                .Property(x => x.FirstName)
                .HasMaxLength(100);

            builder.Entity<User>()
                .Property(x => x.LastName)
                .HasMaxLength(100);

            builder.Entity<User>()
                .Property(x => x.Status)
                .HasMaxLength(50);

            builder.Entity<User>()
                .Property(x => x.Image)
                .HasMaxLength(500);

            builder.Entity<User>()
                .Property(x => x.Gender)
                .HasMaxLength(20);

            builder.Entity<User>()
                .Property(x => x.University)
                .HasMaxLength(150);

            builder.Entity<Module>()
                .HasIndex(x => x.Name)
                .IsUnique();

            builder.Entity<Module>()
                .Property(x => x.IsPremium)
                .HasDefaultValue(false);

            builder.Entity<Subject>()
                .HasIndex(x => new { x.ModuleId, x.Name })
                .IsUnique();

            builder.Entity<Subject>()
                .Property(x => x.IsPremium)
                .HasDefaultValue(false);

            builder.Entity<Subject>()
                .HasOne(x => x.Module)
                .WithMany(x => x.Subjects)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<QuestionSetAccess>()
                .HasIndex(x => new { x.SubjectId, x.QuestionSetNumber })
                .IsUnique();

            builder.Entity<QuestionSetAccess>()
                .Property(x => x.QuestionSetNumber)
                .HasDefaultValue(1);

            builder.Entity<QuestionSetAccess>()
                .HasOne(x => x.Subject)
                .WithMany(x => x.QuestionSetAccesses)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Question>()
                .Property(x => x.QuestionSetNumber)
                .HasDefaultValue(1);

            builder.Entity<Question>()
                .HasIndex(x => new { x.SubjectId, x.QuestionSetNumber });

            builder.Entity<Question>()
                .HasOne(x => x.Subject)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExamAttempt>()
                .Property(x => x.QuestionSetNumber)
                .HasDefaultValue(1);

            builder.Entity<ExamAttempt>()
                .HasIndex(x => new { x.SubjectId, x.QuestionSetNumber, x.StudentId, x.Status });

            builder.Entity<ExamAttempt>()
                .HasOne(x => x.Subject)
                .WithMany()
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExamAttempt>()
                .HasOne(x => x.Student)
                .WithMany(x => x.ExamAttempts)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExamAttemptAnswer>()
                .HasOne(x => x.ExamAttempt)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ExamAttemptAnswer>()
                .HasOne(x => x.Question)
                .WithMany(x => x.ExamAttemptAnswers)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentModulePoint>()
                .Property(x => x.Points)
                .HasColumnType("decimal(18,2)");

            builder.Entity<StudentModulePoint>()
                .HasIndex(x => new { x.StudentId, x.ModuleId })
                .IsUnique();

            builder.Entity<User>()
                .Property(x => x.TotalPoints)
                .HasColumnType("decimal(18,2)");

            builder.Entity<User>()
                .Property(x => x.ExperiencePoints)
                .HasColumnType("decimal(18,2)");

            builder.Entity<User>()
                .Property(x => x.RxCoinBalance)
                .HasColumnType("decimal(18,2)");

            builder.Entity<User>()
                .Property(x => x.RxCoinOnHold)
                .HasColumnType("decimal(18,2)");

            builder.Entity<StudentModulePoint>()
                .HasOne(x => x.Student)
                .WithMany(x => x.ModulePoints)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentModulePoint>()
                .HasOne(x => x.Module)
                .WithMany()
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentRankingBadge>()
                .Property(x => x.Scope)
                .HasMaxLength(20);

            builder.Entity<StudentRankingBadge>()
                .Property(x => x.PeriodType)
                .HasMaxLength(20);

            builder.Entity<StudentRankingBadge>()
                .HasIndex(x => new { x.StudentId, x.Scope, x.ModuleId, x.PeriodType, x.Rank, x.PeriodStartUtc, x.PeriodEndUtc })
                .IsUnique();

            builder.Entity<StudentRankingBadge>()
                .HasOne(x => x.Student)
                .WithMany(x => x.RankingBadges)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentRankingBadge>()
                .HasOne(x => x.Module)
                .WithMany()
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RankingPeriodAward>()
                .Property(x => x.Scope)
                .HasMaxLength(20);

            builder.Entity<RankingPeriodAward>()
                .Property(x => x.PeriodType)
                .HasMaxLength(20);

            builder.Entity<RankingPeriodAward>()
                .HasIndex(x => new { x.Scope, x.ModuleId, x.PeriodType, x.PeriodStartUtc, x.PeriodEndUtc })
                .IsUnique();

            builder.Entity<RankingPeriodAward>()
                .HasOne(x => x.Module)
                .WithMany()
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentWithdrawalRequest>()
                .Property(x => x.RxCoinAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<StudentWithdrawalRequest>()
                .Property(x => x.PesoAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<StudentWithdrawalRequest>()
                .Property(x => x.Status)
                .HasMaxLength(30);

            builder.Entity<StudentWithdrawalRequest>()
                .HasOne(x => x.Student)
                .WithMany(x => x.WithdrawalRequests)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentWithdrawalRequest>()
                .HasOne(x => x.ReviewedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentWithdrawalRequest>()
                .HasIndex(x => new { x.StudentId, x.Status });

            builder.Entity<SubscriptionPlan>()
                .Property(x => x.Code)
                .HasMaxLength(50);

            builder.Entity<SubscriptionPlan>()
                .Property(x => x.Name)
                .HasMaxLength(150);

            builder.Entity<SubscriptionPlan>()
                .Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<SubscriptionPlan>()
                .HasIndex(x => x.Code)
                .IsUnique();

            builder.Entity<SubscriptionPlan>()
                .HasData(
                    new SubscriptionPlan
                    {
                        Id = 1,
                        Code = SubscriptionPlanCodes.TwoMonths,
                        Name = "2 Months Premium Access",
                        Amount = 299m,
                        DurationMonths = 2,
                        IsLifetime = false,
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = 2,
                        Code = SubscriptionPlanCodes.Lifetime,
                        Name = "Lifetime Premium Access",
                        Amount = 1500m,
                        DurationMonths = null,
                        IsLifetime = true,
                        IsActive = true
                    });

            builder.Entity<StudentSubscription>()
                .Property(x => x.Status)
                .HasMaxLength(30);

            builder.Entity<StudentSubscription>()
                .HasIndex(x => new { x.StudentId, x.Status, x.ExpiresAt });

            builder.Entity<StudentSubscription>()
                .HasOne(x => x.Student)
                .WithMany(x => x.StudentSubscriptions)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentSubscription>()
                .HasOne(x => x.Plan)
                .WithMany(x => x.StudentSubscriptions)
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SubscriptionPayment>()
                .Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<SubscriptionPayment>()
                .Property(x => x.Status)
                .HasMaxLength(30);

            builder.Entity<SubscriptionPayment>()
                .Property(x => x.PaymongoCheckoutSessionId)
                .HasMaxLength(120);

            builder.Entity<SubscriptionPayment>()
                .Property(x => x.PaymongoPaymentId)
                .HasMaxLength(120);

            builder.Entity<SubscriptionPayment>()
                .Property(x => x.CheckoutUrl)
                .HasMaxLength(1000);

            builder.Entity<SubscriptionPayment>()
                .HasIndex(x => x.PaymongoCheckoutSessionId)
                .IsUnique();

            builder.Entity<SubscriptionPayment>()
                .HasIndex(x => x.PaymongoPaymentId)
                .IsUnique();

            builder.Entity<SubscriptionPayment>()
                .HasIndex(x => new { x.StudentId, x.Status });

            builder.Entity<SubscriptionPayment>()
                .HasOne(x => x.Student)
                .WithMany(x => x.SubscriptionPayments)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SubscriptionPayment>()
                .HasOne(x => x.Plan)
                .WithMany(x => x.SubscriptionPayments)
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
