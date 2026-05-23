using Microsoft.EntityFrameworkCore;
using RedPandaFlow.Domain.Entities;

namespace RedPandaFlow.Infrastructure.Data
{
    public class RedPandaFlowDbContext : DbContext
    {
        public RedPandaFlowDbContext(DbContextOptions<RedPandaFlowDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<WorkspaceUser> WorkspaceUsers { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<BoardUser> BoardUser { get; set; }
        public DbSet<Column> Columns { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Label> Labels { get; set; }
        public DbSet<CardLabel> CardLabels { get; set; }
        public DbSet<CardUser> CardUsers { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Checklist> Checklists { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Username).IsRequired().HasMaxLength(25);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Biography).HasColumnType("text");
                entity.Property(e => e.AvatarUrl).HasMaxLength(512);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(256);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Token).IsUnique();

                entity.HasOne(e => e.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Workspace>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(25);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Owner)
                      .WithMany()
                      .HasForeignKey(e => e.OwnerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkspaceUser>(entity =>
            {
                entity.HasKey(e => new { e.WorkspaceId, e.UserId });
                entity.Property(e => e.Role)
                      .HasConversion<string>()
                      .HasMaxLength(25)
                      .IsRequired();

                entity.HasOne(e => e.Workspace)
                      .WithMany(w => w.Members)
                      .HasForeignKey(e => e.WorkspaceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Board>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(25);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Workspace)
                      .WithMany()
                      .HasForeignKey(e => e.WorkspaceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Column>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(25);
                entity.Property(e => e.Order).IsRequired();
                entity.Property(e => e.IsArchived).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Board)
                      .WithMany(b => b.Columns)
                      .HasForeignKey(e => e.BoardId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<BoardUser>(entity =>
            {
                entity.HasKey(e => new { e.BoardId, e.UserId });

                entity.Property(e => e.Role)
                      .HasConversion<string>()
                      .HasMaxLength(25)
                      .IsRequired();

                entity.HasOne(e => e.Board)
                    .WithMany(b => b.Members)
                    .HasForeignKey(e => e.BoardId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Card>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(25);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.DueDate);
                entity.Property(e => e.Order).IsRequired();
                entity.Property(e => e.IsArchived).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Column)
                      .WithMany(c => c.Cards)
                      .HasForeignKey(e => e.ColumnId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Label>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Color).IsRequired().HasMaxLength(7);

                entity.HasOne(e => e.Board)
                    .WithMany(b => b.Labels)
                    .HasForeignKey(e => e.BoardId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<CardLabel>(entity =>
            {
                entity.HasKey(e => new { e.CardId, e.LabelId });

                entity.HasOne(e => e.Card)
                      .WithMany(c => c.CardLabels)
                      .HasForeignKey(e => e.CardId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Label)
                      .WithMany(l => l.CardLabels)
                      .HasForeignKey(e => e.LabelId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
