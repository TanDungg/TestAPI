using AiImageGeneratorApi.Data;
using AiImageGeneratorApi.Interfaces;
using AiImageGeneratorApi.Models.DTOs;
using AiImageGeneratorApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Security.Claims;
using AiImageGeneratorApi.Hubs;
using AiImageGeneratorApi.Helpers;

namespace AiImageGeneratorApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Guid _currentUserId;
        private readonly Lazy<Task<User>> _currentUserInfo;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IChatGroupHelper _chatGroupHelper;

        public GroupController(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IHubContext<ChatHub> hubContext, ApplicationDbContext dbContext, IChatGroupHelper chatGroupHelper)
        {
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _hubContext = hubContext;
            _currentUserId = Guid.Parse(httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            _currentUserInfo = new Lazy<Task<User>>(() => _unitOfWork.Users.GetByIdAsync(_currentUserId));
            _chatGroupHelper = chatGroupHelper;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyGroups()
        {
            var groups = await (
                from m in _unitOfWork.DbContext.ChatGroupMembers
                join g in _unitOfWork.DbContext.ChatGroups on m.NhomId equals g.Id
                where m.ThanhVienId == _currentUserId && !m.IsDeleted && !g.IsDeleted
                select new { g.Id, g.TenNhom, g.HinhAnh, g.TruongNhomId }
            ).ToListAsync();

            return Ok(groups);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            var group = new ChatGroup
            {
                Id = Guid.NewGuid(),
                TenNhom = dto.TenNhom,
                HinhAnh = dto.HinhAnh,
                TruongNhomId = _currentUserId,
            };
            await _unitOfWork.ChatGroups.AddAsync(group);

            var members = dto.ThanhViens.Distinct().Append(_currentUserId).Distinct()
                .Select(uid => new ChatGroupMember { Id = Guid.NewGuid(), NhomId = group.Id, ThanhVienId = uid });

            foreach (var m in members)
                await _unitOfWork.ChatGroupMembers.AddAsync(m);

            var currentUser = await _currentUserInfo.Value;
            await _chatGroupHelper.CreateSystemMessageAsync(group.Id, "CreateGroup", $"{currentUser.HoVaTen} đã tạo nhóm.", _currentUserId);

            foreach (var uid in members.Select(x => x.ThanhVienId))
            {
                await _hubContext.Clients.User(uid.ToString()).SendAsync("NewGroupCreated", new
                {
                    group.Id,
                    group.TenNhom,
                    group.HinhAnh
                });
            }

            var response = new
            {
                id = group.Id,
                isNhom = true,
                ten = group.TenNhom,
                hinhAnh = group.HinhAnh,
                tinNhanMoiNhat = $"{currentUser.HoVaTen} đã tạo nhóm.",
                thoiGianNhan = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                soLuongChuaXem = 0,
                isGui = true,
                isThongBao = true
            };

            return Ok(response);
        }

        [HttpPut("{groupId}")]
        public async Task<IActionResult> RenameGroup(Guid groupId, [FromBody] UpdateGroupDto dto)
        {
            var group = await _unitOfWork.ChatGroups.GetByIdAsync(groupId);
            if (group == null || group.TruongNhomId != _currentUserId) return Forbid();

            group.TenNhom = dto.TenNhom;
            _unitOfWork.ChatGroups.Update(group);

            var currentUser = await _currentUserInfo.Value;
            await _chatGroupHelper.CreateSystemMessageAsync(groupId, "RenameGroup", $"{currentUser.HoVaTen} đã đổi tên nhóm thành \"{dto.TenNhom}\".", _currentUserId);

            return Ok();
        }

        [HttpPost("members/{groupId}")]
        public async Task<IActionResult> AddMembers(Guid groupId, [FromBody] List<Guid> userIds)
        {
            var group = await _unitOfWork.ChatGroups.GetByIdAsync(groupId);
            if (group == null || group.TruongNhomId != _currentUserId) return Forbid();

            var existing = await _unitOfWork.ChatGroupMembers.FindAsync(m => m.NhomId == groupId);
            var existingIds = existing.Select(m => m.ThanhVienId).ToHashSet();

            foreach (var id in userIds.Distinct().Where(id => !existingIds.Contains(id)))
            {
                await _unitOfWork.ChatGroupMembers.AddAsync(new ChatGroupMember
                {
                    Id = Guid.NewGuid(),
                    NhomId = groupId,
                    ThanhVienId = id
                });
            }

            var currentUser = await _currentUserInfo.Value;
            var allUsers = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted);
            var addedNames = allUsers.Where(u => userIds.Contains(u.Id)).Select(u => u.HoVaTen).ToList();

            await _chatGroupHelper.CreateSystemMessageAsync(groupId, "AddMembers", $"{string.Join(", ", addedNames)} được {currentUser.HoVaTen} thêm vào nhóm.", _currentUserId);

            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpGet("members/{groupId}")]
        public async Task<IActionResult> GetGroupMembers(Guid groupId, [FromQuery] string? keyword)
        {
            string sql = "EXEC sp_Chat_GetGroupMembers @GroupId, @CurrentUserId, @Keyword";

            var members = await _unitOfWork.Connection.QueryAsync<dynamic>(
                sql,
                new { GroupId = groupId, CurrentUserId = _currentUserId, Keyword = keyword },
                transaction: _unitOfWork.Transaction
            );

            return Ok(members);
        }

        [HttpGet("is-leader/{groupId}")]
        public async Task<IActionResult> CheckIsGroupLeader(Guid groupId)
        {
            var group = await _unitOfWork.ChatGroups.GetByIdAsync(groupId);
            if (group == null || group.IsDeleted)
                return NotFound("Nhóm không tồn tại.");

            return Ok(group.TruongNhomId == _currentUserId);
        }

        [HttpGet("list-user-add-group/{groupId}")]
        public async Task<IActionResult> GetAvailableUsersForGroup(Guid groupId, [FromQuery] string? keyword)
        {
            string sql = "EXEC sp_Chat_GetUsersNotInGroup @GroupId, @CurrentUserId, @Keyword";

            var users = await _unitOfWork.Connection.QueryAsync<dynamic>(
                sql,
                new { GroupId = groupId, CurrentUserId = _currentUserId, Keyword = keyword },
                transaction: _unitOfWork.Transaction
            );

            return Ok(users);
        }

        [HttpDelete("members/{groupId}")]
        public async Task<IActionResult> RemoveMembers(Guid groupId, [FromBody] List<Guid> userIds)
        {
            var group = await _unitOfWork.ChatGroups.GetByIdAsync(groupId);
            if (group == null || group.TruongNhomId != _currentUserId) return Forbid();

            var members = await _unitOfWork.ChatGroupMembers.FindAsync(m => m.NhomId == groupId && userIds.Contains(m.ThanhVienId));
            foreach (var m in members)
            {
                _unitOfWork.ChatGroupMembers.Remove(m);
            }

            var currentUser = await _currentUserInfo.Value;
            var allUsers = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted);
            var removedNames = allUsers.Where(u => userIds.Contains(u.Id)).Select(u => u.HoVaTen).ToList();

            await _chatGroupHelper.CreateSystemMessageAsync(groupId, "RemovedMembers", $"{string.Join(", ", removedNames)} được {currentUser.HoVaTen} xóa khỏi nhóm.", _currentUserId);

            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpDelete("{groupId}")]
        public async Task<IActionResult> DeleteGroup(Guid groupId)
        {
            var group = await _unitOfWork.ChatGroups.GetByIdAsync(groupId);
            if (group == null)
                return NotFound();

            if (group.TruongNhomId != _currentUserId)
                return Forbid();

            _unitOfWork.ChatGroups.Update(group);
            await _unitOfWork.CompleteAsync();

            return Ok();
        }

        [HttpGet("info/{groupId}")]
        public async Task<IActionResult> GetGroupFullInfo(Guid groupId)
        {
            string sql = "EXEC sp_Chat_GetGroupFullInfo @GroupId, @CurrentUserId";

            try
            {
                using var multi = await _unitOfWork.Connection.QueryMultipleAsync(
                    sql,
                    new { GroupId = groupId, CurrentUserId = _currentUserId },
                    transaction: _unitOfWork.Transaction);

                var info = await multi.ReadFirstOrDefaultAsync();
                var image_Videos = (await multi.ReadAsync()).ToList();
                var file = (await multi.ReadAsync()).ToList();
                var link = (await multi.ReadAsync()).ToList();

                return Ok(new
                {
                    info,
                    image_Videos,
                    file,
                    link
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
