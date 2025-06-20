﻿using AiImageGeneratorApi.Data;
using AiImageGeneratorApi.Models.Entities;
using AiImageGeneratorApi.Repositories;
using System.Data;

namespace AiImageGeneratorApi.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ApplicationDbContext DbContext { get; }
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
        IGenericRepository<ChatMessageKey> ChatMessageKeys { get; }
        Task<int> CompleteAsync();
    }
}
