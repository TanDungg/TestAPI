using AiImageGeneratorApi.Interfaces;
using AiImageGeneratorApi.Models.DTOs;
using AiImageGeneratorApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AiImageGeneratorApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoleController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // CRUD Role
        [HttpGet()]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _unitOfWork.Roles.FindAsync(r => !r.IsDeleted);
            var result = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                MaQuyen = r.MaQuyen,
                TenQuyen = r.TenQuyen
            });

            return Ok(result);
        }

        [HttpPost()]
        public async Task<IActionResult> CreateRole([FromBody] RoleDto roleDto)
        {
            var role = new Role
            {
                Id = Guid.NewGuid(),
                MaQuyen = roleDto.MaQuyen,
                TenQuyen = roleDto.TenQuyen,
                CreatedAt = DateTime.Now,
                CreatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
            };

            await _unitOfWork.Roles.AddAsync(role);
            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] RoleDto updatedDto)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(id);
            if (role == null || role.IsDeleted) return NotFound();

            role.MaQuyen = updatedDto.MaQuyen;
            role.TenQuyen = updatedDto.TenQuyen;
            role.UpdatedAt = DateTime.Now;
            role.UpdatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            _unitOfWork.Roles.Update(role);
            await _unitOfWork.CompleteAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(id);
            if (role == null || role.IsDeleted) return NotFound();

            role.IsDeleted = true;
            role.DeletedAt = DateTime.Now;
            role.DeletedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            _unitOfWork.Roles.Update(role);
            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsersWithRoles()
        {
            var users = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted);
            var userRoles = await _unitOfWork.UserRoles.FindAsync(ur => true);
            var roles = await _unitOfWork.Roles.FindAsync(r => !r.IsDeleted);

            var roleMap = roles.ToDictionary(r => r.Id, r => r);

            var result = users.Select(user =>
            {
                var assignedRoles = userRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Select(ur => ur.RoleId)
                    .Where(roleMap.ContainsKey)
                    .Select(roleId => new
                    {
                        RoleId = roleId,
                        roleMap[roleId].MaQuyen,
                        roleMap[roleId].TenQuyen
                    })
                    .ToList();

                return new
                {
                    UserId = user.Id,
                    user.HoVaTen,
                    user.DiaChi,
                    user.Email,
                    user.Sdt,
                    user.HinhAnh,
                    ListRoles = assignedRoles
                };
            });

            return Ok(result);
        }

        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserWithRoles(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || user.IsDeleted) return NotFound("User không tồn tại");

            var userRoles = await _unitOfWork.UserRoles.FindAsync(ur => ur.UserId == userId);
            var roles = await _unitOfWork.Roles.FindAsync(r => !r.IsDeleted);

            var roleMap = roles.ToDictionary(r => r.Id, r => r);

            var assignedRoles = userRoles
                .Select(ur => ur.RoleId)
                .Where(roleMap.ContainsKey)
                .Select(roleId => new
                {
                    RoleId = roleId,
                    roleMap[roleId].MaQuyen,
                    roleMap[roleId].TenQuyen
                })
                .ToList();

            var result = new
            {
                UserId = user.Id,
                user.HoVaTen,
                user.DiaChi,
                user.Email,
                user.Sdt,
                user.HinhAnh,
                Roles = assignedRoles
            };

            return Ok(result);
        }

        [HttpGet("users/list-user-chua-co-quyen")]
        public async Task<IActionResult> GetUsersWithoutRoles()
        {
            var allUsers = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted);
            var userRoles = await _unitOfWork.UserRoles.FindAsync(ur => true);

            var usersWithRoleIds = userRoles.Select(ur => ur.UserId).ToHashSet();

            var result = allUsers
                .Where(user => !usersWithRoleIds.Contains(user.Id))
                .Select(user => new
                {
                    UserId = user.Id,
                    user.HoVaTen,
                    user.DiaChi,
                    user.Email,
                    user.Sdt,
                    user.HinhAnh,
                });

            return Ok(result);
        }

        [HttpPost("user/{userId}")]
        public async Task<IActionResult> AssignRolesToUser(Guid userId, [FromBody] List<Guid> roleIds)
        {
            if (roleIds == null || !roleIds.Any())
                return BadRequest("Danh sách role không hợp lệ.");

            var userIdClaim = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var existingRoles = await _unitOfWork.UserRoles.FindAsync(ur => ur.UserId == userId);
            foreach (var ur in existingRoles)
            {
                _unitOfWork.UserRoles.Remove(ur);
            }

            foreach (var roleId in roleIds.Distinct())
            {
                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    CreatedBy = userIdClaim
                };
                await _unitOfWork.UserRoles.AddAsync(userRole);
            }

            await _unitOfWork.CompleteAsync();
            return Ok("Cập nhật vai trò người dùng thành công.");
        }

        [HttpDelete("user/{userId}")]
        public async Task<IActionResult> RemoveRoleFromUser([FromBody] UserRoleDto dto)
        {
            var userRoles = await _unitOfWork.UserRoles.FindAsync(ur => ur.UserId == dto.UserId && dto.RoleIds.Contains(ur.RoleId));
            foreach (var ur in userRoles)
            {
                _unitOfWork.UserRoles.Remove(ur);
            }

            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpGet("menu/{roleId}")]
        public async Task<IActionResult> GetMenusWithPermissionsByRole(Guid roleId)
        {
            var allMenus = await _unitOfWork.Menus.FindAsync(m => !m.IsDeleted);
            var rolePermissions = await _unitOfWork.RoleMenus.FindAsync(rm => rm.RoleId == roleId);
            var permissionMap = rolePermissions.ToDictionary(rm => rm.MenuId, rm => rm);

            List<object> BuildMenuTree(IEnumerable<Menu> menus, Guid? parentId)
            {
                return menus.Where(m => m.ParentId == parentId)
                    .OrderBy(m => m.ThuTu)
                    .Select(menu => new
                    {
                        MenuId = menu.Id,
                        menu.MaMenu,
                        menu.TenMenu,
                        menu.Icon,
                        menu.DuongDan,
                        menu.ThuTu,

                        View = permissionMap.TryGetValue(menu.Id, out var perm) ? perm.View : false,
                        Add = perm?.Add ?? false,
                        Edit = perm?.Edit ?? false,
                        Delete = perm?.Delete ?? false,
                        Confirm = perm?.Confirm ?? false,

                        Children = BuildMenuTree(menus, menu.Id)
                    }).ToList<object>();
            }

            var tree = BuildMenuTree(allMenus, null);
            return Ok(tree);
        }

        [HttpPut("menu/{roleId}")]
        public async Task<IActionResult> UpdateRolePermissions(Guid roleId, [FromBody] List<RoleMenuDto> listMenus)
        {
            if (listMenus == null || !listMenus.Any())
                return BadRequest("Dữ liệu không hợp lệ.");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var existingPermissions = await _unitOfWork.RoleMenus.FindAsync(rm => rm.RoleId == roleId);
            var permissionMap = existingPermissions.ToDictionary(rm => rm.MenuId, rm => rm);

            foreach (var item in listMenus)
            {
                if (permissionMap.TryGetValue(item.MenuId, out var existing))
                {
                    existing.View = item.View;
                    existing.Add = item.Add;
                    existing.Edit = item.Edit;
                    existing.Delete = item.Delete;
                    existing.Confirm = item.Confirm;
                    existing.UpdatedAt = DateTime.Now;
                    existing.UpdatedBy = userId;

                    _unitOfWork.RoleMenus.Update(existing);
                }
                else
                {
                    var newPermission = new RoleMenu
                    {
                        Id = Guid.NewGuid(),
                        RoleId = roleId,
                        MenuId = item.MenuId,
                        View = item.View,
                        Add = item.Add,
                        Edit = item.Edit,
                        Delete = item.Delete,
                        Confirm = item.Confirm,
                        CreatedAt = DateTime.Now,
                        CreatedBy = userId
                    };

                    await _unitOfWork.RoleMenus.AddAsync(newPermission);
                }
            }

            await _unitOfWork.CompleteAsync();
            return Ok("Cập nhật quyền thành công.");
        }
    }
}