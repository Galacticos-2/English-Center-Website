
using System;
using System.Reflection.Emit;
using System.Security.Claims;
using System.Text;
using EnglishCenterSystem.API.Hubs;
using EnglishCenterSystem.API.Middlewares;
using EnglishCenterSystem.API.Services;
using EnglishCenterSystem.Application
    .Features.AdminDashboard.Services;
using EnglishCenterSystem.Application.Features.Auth.Services;
using EnglishCenterSystem.Application.Features.Competitions.Mappers;
using EnglishCenterSystem.Application.Features.Competitions.Services;
using EnglishCenterSystem.Application.Features.Courses.Services;
using EnglishCenterSystem.Application.Features.Exercises.Services;
using EnglishCenterSystem.Application.Features.Flashcards.Services;
using EnglishCenterSystem.Application.Features.Imports.Services;
using EnglishCenterSystem.Application.Features.Lessons.Services;
using EnglishCenterSystem.Application.Features.Listening.Services;
using EnglishCenterSystem.Application.Features.OnlineClassrooms.Services;
using EnglishCenterSystem.Application
    .Features.Notifications.Services;
using EnglishCenterSystem.Application.Features.Reading.Services;
using EnglishCenterSystem.Application.Features.Speaking.Services;
using EnglishCenterSystem.Application
    .Features.TeacherDashboard.Services;
using EnglishCenterSystem.Application
    .Features.TeacherStudents.Services;
using EnglishCenterSystem.Application.Features.Tests.Mappers;
using EnglishCenterSystem.Application.Features.Tests.Services;
using EnglishCenterSystem.Application.Features.Users.Services;
using EnglishCenterSystem.Application.Features.Writing.Services;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Application
    .Interfaces.Services;
using EnglishCenterSystem.Application.Validators.Auth;
using EnglishCenterSystem.Domain.Entities;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Infrastructure.BackgroundJobs;
using EnglishCenterSystem.Infrastructure.Identity;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using EnglishCenterSystem.Infrastructure.Persistence.Repositories;
using EnglishCenterSystem.Infrastructure.Persistence.UnitOfWorks;
using EnglishCenterSystem.Infrastructure.Services;
using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;


namespace Project_Web_EnglishCenterSystem.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Name = "Authorization",

                        Type = SecuritySchemeType.Http,

                        Scheme = "bearer",

                        BearerFormat = "JWT",

                        In = ParameterLocation.Header,

                        Description =
                            "Enter JWT Token"
                    });

                options.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType
                                .SecurityScheme,

                            Id = "Bearer"
                        }
                },

                Array.Empty<string>()
            }
                    });
            });
            builder.Services.AddScoped<ExerciseAssignmentService>();
            builder.Services.AddScoped<LessonService>();
            builder.Services.AddScoped<CourseService>();
            builder.Services.AddScoped<OnlineClassroomService>();
            builder.Services.AddScoped<FlashcardService>();
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            builder.Services.AddScoped<ExerciseResultService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<StudentExerciseService>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ExerciseService>();
            builder.Services.AddScoped<QuestionService>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 1024L * 1024L * 1024L;
            });
            builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection")));
          
            builder.Services.AddHangfireServer();
            builder.Services.AddHttpClient<IExerciseAiParserService,
    GroqExerciseAiParserService>();
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 1024L * 1024L * 1024L;
            });
            builder.Services.AddScoped<TestResultMapper>();
            builder.Services.AddScoped<
    IExerciseDocumentParserService,
    ExerciseDocumentParserService>();
            builder.Services.AddScoped<ExerciseProgressService>();
            builder.Services.AddScoped<IMinioService, MinioService>();
            builder.Services.AddScoped<IStudentGamificationService, StudentGamificationService>();
            builder.Services.AddScoped<
    IPdfPageRenderService,
    PdfPageRenderService>();
            builder.Services.AddScoped<
                IToeicPdfPromptImageExtractorService,
                ToeicPdfPromptImageExtractorService>();

            builder.Services.AddScoped<
                IReadingPassageSnapshotService,
                ReadingPassageSnapshotService>();
            builder.Services.AddHttpClient<
                IToeicReadingImportService,
                ToeicReadingImportService>();
            builder.Services.AddScoped<TeacherTestService>();
            builder.Services.AddScoped<IPdfParserService, PdfParserService>();
            builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<WritingExerciseImportService>();
            builder.Services.AddHttpClient<IWritingAiParserService,
                GroqWritingAiParserService>();
            builder.Services.AddHttpClient<IQuestionExplanationService,
                GroqQuestionExplanationService>();
            builder.Services.AddScoped<WritingGradingBackgroundJobService>();
            builder.Services.AddHttpClient<IWritingAiEvaluationService,
                GroqWritingAiEvaluationService>();
            builder.Services.AddScoped<IWritingScoreRepository,
                WritingScoreRepository>();
            builder.Services.AddScoped<ListeningCoreImportService>();
            builder.Services.AddScoped<ReadingCoreImportService>();
            builder.Services.AddScoped<WritingCoreImportService>();
            builder.Services.AddScoped<ImportPreflightService>();
            builder.Services.AddScoped<TestImportService>();

            builder.Services.AddScoped<TestImportBackgroundJobService>();
            builder.Services.AddScoped<TestAiGradingBackgroundJobService>();

            builder.Services.AddScoped<
                ITestAiGradingQueueService,
                TestAiGradingQueueService>();
            builder.Services.AddScoped<
                ITestDocumentSplitService,
                TestDocumentSplitService>();
            builder.Services.AddScoped<
    TeacherCompetitionAssignmentService>();
            builder.Services.AddScoped<TeacherDashboardService>();
            builder.Services.AddScoped<
                ICompetitionDeadlineQueueService,
                CompetitionDeadlineQueueService>();
            
            builder.Services.AddScoped<
    CompetitionAttemptBuilder>();
            builder.Services.AddScoped<
    CompetitionRankingService>();
            builder.Services.AddScoped<
    TeacherCompetitionResultService>();
            builder.Services.AddScoped<
                ICompetitionAiGradingQueueService,
                CompetitionAiGradingQueueService>();

            builder.Services.AddScoped<
                CompetitionAiGradingBackgroundJobService>();
            builder.Services.AddScoped<
                CompetitionResultMapper>();
            builder.Services.AddSignalR();
            builder.Services.AddScoped<
    TeacherStudentDashboardService>();
            builder.Services.AddScoped<
    INotificationRealtimeService,
    SignalRNotificationRealtimeService>();
            builder.Services.AddScoped<
    AdminDashboardService>();
            builder.Services.AddScoped<
                StudentCompetitionService>();
            builder.Services.AddScoped<
                CompetitionDeadlineBackgroundJobService>();
            builder.Services.AddScoped<StudentTestService>();
            builder.Services.AddScoped<IWritingTaskRepository, WritingTaskRepository>();
            builder.Services.AddScoped<ExerciseImportService>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllowFrontend",
                    policy =>
                    {
                        policy
                            .WithOrigins(
                                "https://localhost:40482"
                            )
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });
            builder.Services
    .AddAuthentication(
        JwtBearerDefaults
            .AuthenticationScheme
    )
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey =
                    true,

                ValidIssuer =
                    builder.Configuration[
                        "JWT:Issuer"
                    ],

                ValidAudience =
                    builder.Configuration[
                        "JWT:Audience"
                    ],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration[
                                "JWT:Secret"
                            ]!
                        )
                    )
            };

        options.Events =
            new JwtBearerEvents
            {
                OnMessageReceived =
                    context =>
                    {
                        var accessToken =
                            context.Request
                                .Query[
                                    "access_token"
                                ];

                        var path =
                            context.HttpContext
                                .Request
                                .Path;

                        if (
                            !string
                                .IsNullOrEmpty(
                                    accessToken
                                ) &&
                            path.StartsWithSegments(
                                "/hubs/notifications"
                            )
                        )
                        {
                            context.Token =
                                accessToken;
                        }

                        return Task
                            .CompletedTask;
                    }
            };
    });
            builder.Services.AddScoped<
    CompetitionTestImportService>();

            builder.Services.AddScoped<
                TeacherCompetitionTestService>();

            builder.Services.AddScoped<
                CompetitionTestImportBackgroundJobService>();
            builder.Services.AddHttpClient<IReadingAiParserService,
    GroqReadingAiParserService>();
            builder.Services.AddScoped<ListeningImportBackgroundJobService>();
            builder.Services.AddScoped<SpeakingExerciseImportService>();
            builder.Services.AddScoped<SpeakingCoreImportService>();
            builder.Services.AddHttpClient<ISpeechAssessmentService,
    GroqWhisperSpeechAssessmentService>();
            builder.Services.AddHttpClient<ISpeakingAiEvaluationService,
                GroqSpeakingAiEvaluationService>();
            builder.Services.AddHttpClient<IListeningAiParserService,
    GroqListeningAiParserService>();
            builder.Services.AddHttpClient<ISpeakingAiParserService,
    GroqSpeakingAiParserService>();
            builder.Services.AddScoped<
    NotificationService>();
            builder.Services.AddScoped<
    AdminNotificationScanService>();
            builder.Services.AddScoped<StudentReminderScanService>();
            builder.Services.AddScoped<IListeningTaskRepository, ListeningTaskRepository>();
            builder.Services.AddScoped<ListeningExerciseImportService>();
            builder.Services.AddScoped<ListeningService>();
            builder.Services.AddScoped<ExerciseStructureService>();
            builder.Services.AddScoped<ReadingService>();
            builder.Services.AddScoped<VideoJobService>();
            builder.Services.AddScoped<ReadingExerciseImportService>();
            builder.Services.AddScoped<IReadingTaskRepository, ReadingTaskRepository>();
            builder.Services.AddAuthorization();
            builder.Services
    .AddValidatorsFromAssemblyContaining<LoginValidator>();
            var app = builder.Build();

            app.UseHangfireDashboard(
                "/hangfire");

            RecurringJob.AddOrUpdate<
                AdminNotificationScanService>(
                    "scan-admin-notification-issues",

                    service =>
                        service.ScanAsync(
                            CancellationToken.None),

                    Cron.Minutely(),

                    new RecurringJobOptions
                    {
                        TimeZone =
                            GetVietnamTimeZone()
                    });

            RecurringJob.AddOrUpdate<
                StudentReminderScanService>(
                    "scan-student-reminders",
                    service => service.ScanAsync(CancellationToken.None),
                    Cron.Minutely(),
                    new RecurringJobOptions { TimeZone = GetVietnamTimeZone() });

            using (var scope =
                app.Services.CreateScope())
            {
                var context =
                    scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                context.Database.Migrate();

                if (!context.Users.Any())
                {
                    context.Users.Add(
                        new User
                        {
                            UserName = "admin",

                            FullName = "Administrator",

                            Email = "admin@gmail.com",

                            PasswordHash =
                                BCrypt.Net.BCrypt
                                .HashPassword("123456"),

                            PhoneNumber = "0123456789",

                            Birthday =
                                new DateTime(2000, 1, 1),

                            Role = UserRole.Admin,

                            IsActive = true,

                            IsDeleted = false,

                            CreatedAt = DateTime.UtcNow
                        });

                    context.SaveChanges();
                }
                if (!context.LessonCategories.Any())
                {
                    context.LessonCategories.AddRange(
                        new LessonCategory
                        {
                            Name = "Ngữ pháp"
                        },
                        new LessonCategory
                        {
                            Name = "Nghe"
                        },
                        new LessonCategory
                        {
                            Name = "Nói"
                        },
                        new LessonCategory
                        {
                            Name = "Đọc"
                        },
                        new LessonCategory
                        {
                            Name = "Viết"
                        });

                    context.SaveChanges();
                }
                if (!context.QuestionCategories.Any())
                {
                    context.QuestionCategories.AddRange(
                        new QuestionCategory
                        {
                            Name = "Ngữ pháp",
                            Description = "Câu hỏi bài tập ngữ pháp"
                        },
                        new QuestionCategory
                        {
                            Name = "Nghe",
                            Description = "Câu hỏi bài tập kỹ năng nghe"
                        },
                        new QuestionCategory
                        {
                            Name = "Nói",
                            Description = "Câu hỏi bài tập kỹ năng nói"
                        },
                        new QuestionCategory
                        {
                            Name = "Đọc",
                            Description = "Câu hỏi bài tập kỹ năng đọc"
                        },
                        new QuestionCategory
                        {
                            Name = "Viết",
                            Description = "Câu hỏi bài tập kỹ năng viết"
                        });

                    context.SaveChanges();
                }
                
            }
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowFrontend");
            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotificationHub>(
    "/hubs/notifications");

            app.MapFallbackToFile("/index.html");

            app.Lifetime.ApplicationStarted.Register(
                () =>
                {
                    _ = Task.Run(
                        async () =>
                        {
                            try
                            {
                                await using var scope =
                                    app.Services
                                        .CreateAsyncScope();

                                var scanService =
                                    scope.ServiceProvider
                                        .GetRequiredService<
                                            AdminNotificationScanService>();

                                await scanService
                                    .ScanAsync();
                            }
                            catch (Exception ex)
                            {
                                var logger =
                                    app.Services
                                        .GetRequiredService<
                                            ILogger<Program>>();

                                logger.LogError(
                                    ex,
                                    "Startup admin notification scan failed");
                            }
                        });
                });

            app.Run();
        }
        private static TimeZoneInfo
GetVietnamTimeZone()
        {
            var timeZoneId =
                OperatingSystem.IsWindows()
                    ? "SE Asia Standard Time"
                    : "Asia/Ho_Chi_Minh";

            return TimeZoneInfo
                .FindSystemTimeZoneById(
                    timeZoneId);
        }
    }
}
