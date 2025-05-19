using AiImageGeneratorApi.Models.Entities;
using AiImageGeneratorApi.Repositories;

namespace AiImageGeneratorApi.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<User> Users { get; }
        IGenericRepository<Menu> Menus { get; }
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<RoleMenu> RoleMenus { get; }
        IGenericRepository<UserRole> UserRoles { get; }
        IGenericRepository<ChatMessage> ChatMessages{ get; }
        IGenericRepository<ChatGroup> ChatGroups { get; }
        IGenericRepository<ChatGroupMember> ChatGroupMembers { get; }
        Task<int> CompleteAsync();
    }
}
