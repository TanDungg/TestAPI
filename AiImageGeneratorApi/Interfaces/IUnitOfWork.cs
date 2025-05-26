using AiImageGeneratorApi.Models.Entities;
using AiImageGeneratorApi.Repositories;
using System.Data;

namespace AiImageGeneratorApi.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; }

        IGenericRepository<User> Users { get; }
        IGenericRepository<Menu> Menus { get; }
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<RoleMenu> RoleMenus { get; }
        IGenericRepository<UserRole> UserRoles { get; }
        IGenericRepository<ChatMessage> ChatMessages{ get; }
        IGenericRepository<ChatGroup> ChatGroups { get; }
        IGenericRepository<ChatGroupMember> ChatGroupMembers { get; }
        IGenericRepository<ChatMessageRead> ChatMessageReads { get; }
        Task<int> CompleteAsync();
    }
}
