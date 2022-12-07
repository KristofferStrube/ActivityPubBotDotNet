using Microsoft.EntityFrameworkCore;

namespace KristofferStrube.ActivityPubBotDotNet.Server;

public class ActivityPubDbContext : DbContext
{
    public ActivityPubDbContext(DbContextOptions<ActivityPubDbContext> options) : base(options) { }

    public DbSet<UserInfo> Users => Set<UserInfo>();
    public DbSet<FollowRelation> FollowRelations => Set<FollowRelation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<UserInfo>()
            .HasKey(u => u.Id);

        builder.Entity<FollowRelation>()
            .HasKey(f => new { f.FollowerId, f.FollowedId });

        builder.Entity<FollowRelation>()
            .HasOne(f => f.Follower)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FollowerId);

        builder.Entity<FollowRelation>()
            .HasOne(f => f.Followed)
            .WithMany(u => u.Following)
            .HasForeignKey(f => f.FollowedId);

    }
}
