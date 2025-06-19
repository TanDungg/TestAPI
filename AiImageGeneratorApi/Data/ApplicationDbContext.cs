using AiImageGeneratorApi.Models.DTOs;
using AiImageGeneratorApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiImageGeneratorApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        // Khai báo DbSet cho các entity
        public DbSet<User> Users => Set<User>();
        public DbSet<Menu> Menus => Set<Menu>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<GeneratedImage> GeneratedImages => Set<GeneratedImage>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<ChatGroup> ChatGroups => Set<ChatGroup>();
        public DbSet<ChatGroupMember> ChatGroupMembers => Set<ChatGroupMember>();
        public DbSet<ChatMessageRead> ChatMessageReads => Set<ChatMessageRead>();

        // Các DTO dùng cho FROM SQL hoặc ViewModel
        public DbSet<ChatMessageDto> ChatMessageDto => Set<ChatMessageDto>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tự động lọc dữ liệu bị xóa mềm
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<GeneratedImage>().HasQueryFilter(g => !g.IsDeleted);

            // Cấu hình DTOs không có khóa chính
            modelBuilder.Entity<ChatMessageDto>().HasNoKey();
            modelBuilder.Entity<ChatInfoMessage>().HasNoKey();
            modelBuilder.Entity<ListChatDto>().HasNoKey();

            // Cấu hình ChatMessageRead
            modelBuilder.Entity<ChatMessageRead>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Message)
                      .WithMany(m => m.Reads)
                      .HasForeignKey(e => e.TinNhanId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.ThanhVienId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình ChatMessage
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.NguoiGui)
                .WithMany(u => u.MessagesSent)
                .HasForeignKey(m => m.NguoiGuiId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.NguoiNhan)
                .WithMany(u => u.MessagesReceived)
                .HasForeignKey(m => m.NguoiNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.EncryptedMessage)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.EncryptedKeyForSender)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.EncryptedKeyForReceiver)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.IV)
                .HasColumnType("nvarchar(max)");

            // Cấu hình User chứa key RSA
            modelBuilder.Entity<User>()
                .Property(u => u.PublicKey)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<User>()
                .Property(u => u.PrivateKey)
                .HasColumnType("nvarchar(max)");
        }

        // Audit: Tự động cập nhật ngày giờ tạo/sửa/xóa
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var now = DateTime.Now;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = now;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.DeletedAt = now;
                        entry.Entity.IsDeleted = true;
                        break;
                }
            }
        }
    }
}
