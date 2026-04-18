using EduConnect.Domain.Common;
using EduConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<University> Universities => Set<University>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<PostBookmark> PostBookmarks => Set<PostBookmark>();
    public DbSet<PostView> PostViews => Set<PostView>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<VisualSearchHistory> VisualSearchHistories => Set<VisualSearchHistory>();
    public DbSet<VisualSearchResult> VisualSearchResults => Set<VisualSearchResult>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<ChatMessageFeedback> ChatMessageFeedbacks => Set<ChatMessageFeedback>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<DirectConversation> DirectConversations => Set<DirectConversation>();
    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditing();
        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigurePosts(modelBuilder);
        ConfigureGroups(modelBuilder);
        ConfigureEvents(modelBuilder);
        ConfigureProducts(modelBuilder);
        ConfigureVisualSearch(modelBuilder);
        ConfigureChat(modelBuilder);
        ConfigureFeedbackAndNotifications(modelBuilder);
        ConfigureDiscounts(modelBuilder);
        ConfigureDirectMessaging(modelBuilder);
    }

    private void ApplyAuditing()
    {
        var entries = ChangeTracker
            .Entries<AuditableEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EmailVerificationCodeHash).HasMaxLength(500);
            entity.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();

            entity.HasOne(x => x.University)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.UniversityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.StudentProfile)
                .WithOne(x => x.User)
                .HasForeignKey<StudentProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.ToTable("StudentProfiles");
            entity.Property(x => x.Department).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Bio).HasMaxLength(500);
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.CoverImageUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<University>(entity =>
        {
            entity.ToTable("Universities");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.City).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Domain).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasIndex(x => x.Token).IsUnique();
            entity.Property(x => x.Token).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedByIp).HasMaxLength(100);
            entity.Property(x => x.ReplacedByToken).HasMaxLength(500);

            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserFollow>(entity =>
        {
            entity.ToTable("UserFollows");
            entity.HasIndex(x => new { x.FollowerUserId, x.FollowedUserId }).IsUnique();

            entity.HasOne(x => x.FollowerUser)
                .WithMany(x => x.FollowingRelationships)
                .HasForeignKey(x => x.FollowerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.FollowedUser)
                .WithMany(x => x.FollowerRelationships)
                .HasForeignKey(x => x.FollowedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePosts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToTable("Posts");
            entity.Property(x => x.Content).HasMaxLength(1500).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(500);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Posts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Group)
                .WithMany(x => x.Posts)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PostComment>(entity =>
        {
            entity.ToTable("PostComments");
            entity.Property(x => x.Content).HasMaxLength(500).IsRequired();

            entity.HasOne(x => x.Post)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PostLike>(entity =>
        {
            entity.ToTable("PostLikes");
            entity.HasIndex(x => new { x.PostId, x.UserId }).IsUnique();

            entity.HasOne(x => x.Post)
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PostBookmark>(entity =>
        {
            entity.ToTable("PostBookmarks");
            entity.HasIndex(x => new { x.PostId, x.UserId }).IsUnique();

            entity.HasOne(x => x.Post)
                .WithMany(x => x.Bookmarks)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.BookmarkedPosts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PostView>(entity =>
        {
            entity.ToTable("PostViews");
            entity.HasIndex(x => new { x.PostId, x.UserId }).IsUnique();

            entity.HasOne(x => x.Post)
                .WithMany(x => x.Views)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.ViewedPosts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureGroups(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>(entity =>
        {
            entity.ToTable("Groups");
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ShortDescription).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.RulesJson).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.BannerUrl).HasMaxLength(500);
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();

            entity.HasOne(x => x.CreatorUser)
                .WithMany(x => x.OwnedGroups)
                .HasForeignKey(x => x.CreatorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.ToTable("GroupMembers");
            entity.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();

            entity.HasOne(x => x.Group)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.GroupMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1500).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();

            entity.HasOne(x => x.CreatorUser)
                .WithMany(x => x.OwnedEvents)
                .HasForeignKey(x => x.CreatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Group)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EventParticipant>(entity =>
        {
            entity.ToTable("EventParticipants");
            entity.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();

            entity.HasOne(x => x.Event)
                .WithMany(x => x.Participants)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.EventParticipations)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProducts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();

            entity.HasOne(x => x.ParentCategory)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(3000).IsRequired();
            entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
            entity.Property(x => x.City).HasMaxLength(120).IsRequired();

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Seller)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages");
            entity.Property(x => x.Url).HasMaxLength(500).IsRequired();
            entity.Property(x => x.EmbeddingJson).HasColumnType("nvarchar(max)");

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureVisualSearch(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VisualSearchHistory>(entity =>
        {
            entity.ToTable("VisualSearchHistories");
            entity.Property(x => x.QueryImageUrl).HasMaxLength(500).IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.VisualSearchHistories)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VisualSearchResult>(entity =>
        {
            entity.ToTable("VisualSearchResults");
            entity.Property(x => x.SimilarityScore).HasColumnType("float");

            entity.HasOne(x => x.SearchHistory)
                .WithMany(x => x.Results)
                .HasForeignKey(x => x.SearchHistoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureChat(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("ChatSessions");

            entity.HasOne(x => x.User)
                .WithMany(x => x.ChatSessions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessages");
            entity.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.IntentDetected).HasMaxLength(150);
            entity.Property(x => x.ModelUsed).HasMaxLength(100);
            entity.Property(x => x.KbScore).HasColumnType("float");

            entity.HasOne(x => x.Session)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Feedback)
                .WithOne(x => x.ChatMessage)
                .HasForeignKey<ChatMessageFeedback>(x => x.ChatMessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessageFeedback>(entity =>
        {
            entity.ToTable("ChatMessageFeedbacks");
            entity.HasIndex(x => x.ChatMessageId).IsUnique();
            entity.Property(x => x.Comment).HasMaxLength(500);

            entity.HasOne(x => x.User)
                .WithMany(x => x.ChatMessageFeedbacks)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFeedbackAndNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("Feedbacks");
            entity.Property(x => x.FeatureArea).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(1000);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Feedbacks)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.TargetPath).HasMaxLength(500);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureDiscounts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Discount>(entity =>
        {
            entity.ToTable("Discounts");
            entity.Property(x => x.BusinessName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.DiscountCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.LogoUrl).HasMaxLength(500);
            entity.Property(x => x.DiscountRate).HasColumnType("decimal(5,2)");
        });
    }

    private static void ConfigureDirectMessaging(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DirectConversation>(entity =>
        {
            entity.ToTable("DirectConversations");
            entity.HasIndex(x => new { x.UserLowerId, x.UserHigherId }).IsUnique();

            entity.HasOne(x => x.UserLower)
                .WithMany()
                .HasForeignKey(x => x.UserLowerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UserHigher)
                .WithMany()
                .HasForeignKey(x => x.UserHigherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DirectMessage>(entity =>
        {
            entity.ToTable("DirectMessages");
            entity.Property(x => x.Content).HasMaxLength(2000).IsRequired();

            entity.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
