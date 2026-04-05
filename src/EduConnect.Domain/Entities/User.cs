using EduConnect.Domain.Common;
using EduConnect.Domain.Enums;

namespace EduConnect.Domain.Entities;

public sealed class User : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime? EmailVerifiedAtUtc { get; set; }

    public string? EmailVerificationCodeHash { get; set; }

    public DateTime? EmailVerificationExpiresAtUtc { get; set; }

    public DateTime? EmailVerificationSentAtUtc { get; set; }

    public int EmailVerificationAttemptCount { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Student;

    public Guid? UniversityId { get; set; }

    public bool IsActive { get; set; } = true;

    public bool TwoFactorEnabled { get; set; }

    public University? University { get; set; }

    public StudentProfile? StudentProfile { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public ICollection<Post> Posts { get; set; } = [];

    public ICollection<PostComment> Comments { get; set; } = [];

    public ICollection<PostLike> Likes { get; set; } = [];

    public ICollection<PostBookmark> BookmarkedPosts { get; set; } = [];

    public ICollection<PostView> ViewedPosts { get; set; } = [];

    public ICollection<Group> OwnedGroups { get; set; } = [];

    public ICollection<GroupMember> GroupMemberships { get; set; } = [];

    public ICollection<Event> OwnedEvents { get; set; } = [];

    public ICollection<EventParticipant> EventParticipations { get; set; } = [];

    public ICollection<Product> Products { get; set; } = [];

    public ICollection<VisualSearchHistory> VisualSearchHistories { get; set; } = [];

    public ICollection<ChatSession> ChatSessions { get; set; } = [];

    public ICollection<Feedback> Feedbacks { get; set; } = [];

    public ICollection<Notification> Notifications { get; set; } = [];
}
