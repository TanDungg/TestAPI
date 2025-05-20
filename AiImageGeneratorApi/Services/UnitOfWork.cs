using AiImageGeneratorApi.Data;
using AiImageGeneratorApi.Interfaces;
using AiImageGeneratorApi.Models.Entities;
using AiImageGeneratorApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AiImageGeneratorApi.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IGenericRepository<User> _users;
        private IGenericRepository<Menu> _menus;
        private IGenericRepository<Role> _roles;
        private IGenericRepository<RoleMenu> _roleMenus;
        private IGenericRepository<UserRole> _userRoles;
        private IGenericRepository<ChatMessage> _chatMessages;
        private IGenericRepository<ChatGroup> _chatGroups;
        private IGenericRepository<ChatGroupMember> _chatGroupMembers;
        private IGenericRepository<ChatMessageRead> _chatMessageReads;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }
        public DbContext Context => _context;
        public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);
        public IGenericRepository<Menu> Menus => _menus ??= new GenericRepository<Menu>(_context);
        public IGenericRepository<Role> Roles => _roles ??= new GenericRepository<Role>(_context);
        public IGenericRepository<RoleMenu> RoleMenus => _roleMenus ??= new GenericRepository<RoleMenu>(_context);
        public IGenericRepository<UserRole> UserRoles => _userRoles ??= new GenericRepository<UserRole>(_context);
        public IGenericRepository<ChatMessage> ChatMessages => _chatMessages ??= new GenericRepository<ChatMessage>(_context);
        public IGenericRepository<ChatGroup> ChatGroups => _chatGroups ??= new GenericRepository<ChatGroup>(_context);
        public IGenericRepository<ChatGroupMember> ChatGroupMembers => _chatGroupMembers ??= new GenericRepository<ChatGroupMember>(_context);
        public IGenericRepository<ChatMessageRead> ChatMessageReads => _chatMessageReads ??= new GenericRepository<ChatMessageRead>(_context);

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
