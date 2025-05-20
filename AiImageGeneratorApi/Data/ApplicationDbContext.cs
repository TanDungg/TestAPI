using AiImageGeneratorApi.Models.DTOs;
using AiImageGeneratorApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiImageGeneratorApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<ChatMessageDto> ChatMessageDtos { get; set; }

        public ApplicationDbContext(DbContextOptions options) : base(options) { }
        public DbSet<ChatMessageDto> ChatMessageDto { get; set; }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<GeneratedImage>().HasQueryFilter(g => !g.IsDeleted);
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ChatMessageDto>().HasNoKey();
        }
    }

}
