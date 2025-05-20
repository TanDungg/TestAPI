using AiImageGeneratorApi.Data;
using AiImageGeneratorApi.Hubs;
using AiImageGeneratorApi.Interfaces;
using AiImageGeneratorApi.Models.DTOs;
using AiImageGeneratorApi.Models.Entities;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using System.Security.Claims;

namespace AiImageGeneratorApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Guid _currentUserId;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IHubContext<ChatHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _currentUserId = Guid.Parse(httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TinNhan)) return BadRequest("Nội dung không được để trống");

            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                NguoiGuiId = _currentUserId,
                TinNhan = dto.TinNhan,
                CreatedAt = DateTime.UtcNow,
                NguoiNhanId = dto.NguoiNhanId,
                NhomId = dto.NhomId
            };

            await _unitOfWork.ChatMessages.AddAsync(message);
            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        //[HttpGet("messages/{userId}")]
        //public async Task<IActionResult> GetPrivateMessagesBySP(Guid userId)
        //{
        //    var currentUserId = _currentUserId;

        //    string sql = "EXEC sp_GetPrivateMessages {0}, {1}";
        //    var result = await _unitOfWork.ChatMessages.ExecuteStoredProcedureAsync<ChatUserInfoDto>(sql, currentUserId, userId);

        //    if (result == null || result.Count == 0)
        //        return NotFound();

        //    var raw = result.First();
        //    var parsedMessages = JsonConvert.DeserializeObject<List<ChatMessageDto>>(raw.List_Messages);

        //    return Ok(new
        //    {
        //        raw.NguoiNhanId,
        //        raw.HoVaTen,
        //        raw.DiaChi,
        //        raw.Email,
        //        raw.Sdt,
        //        raw.HinhAnh,
        //        List_Messages = parsedMessages
        //    });
        //}

        [HttpGet("messages/{userId}")]
        public async Task<IActionResult> GetPrivateMessagesBySP(Guid userId)
        {
            var currentUserId = _currentUserId;
            var connection = _dbContext.Database.GetDbConnection();

            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<string>(
                sql: "sp_GetPrivateMessages",
                param: new { UserId1 = currentUserId, UserId2 = userId },
                commandType: CommandType.StoredProcedure
            );

            if (string.IsNullOrWhiteSpace(result))
                return NotFound();

            var userInfo = JsonConvert.DeserializeObject<ChatUserInfoDto>(result);
            var messages = JsonConvert.DeserializeObject<List<ChatMessageDto>>(userInfo.List_Messages);

            return Ok(new
            {
                userInfo.NguoiNhanId,
                userInfo.HoVaTen,
                userInfo.DiaChi,
                userInfo.Email,
                userInfo.Sdt,
                userInfo.HinhAnh,
                List_Messages = messages
            });
        }


        [HttpGet("group/messages/{groupId}")]
        public async Task<IActionResult> GetGroupMessages(Guid groupId)
        {
            var isMember = await _unitOfWork.ChatGroupMembers.FindAsync(m => m.NhomId == groupId && m.ThanhVienId == _currentUserId);
            if (isMember == null) return Forbid();

            var messages = await _unitOfWork.ChatMessages.FindAsync(m => m.NhomId == groupId && !m.IsDeleted);
            return Ok(messages.OrderBy(m => m.CreatedAt));
        }

        [HttpGet("list-message")]
        public async Task<IActionResult> GetRecentChats()
        {
            var userId = _currentUserId;

            var messages = await _unitOfWork.ChatMessages
                .FindAsync(m => (m.NguoiGuiId == userId || m.NguoiNhanId == userId) && m.NhomId == null && !m.IsDeleted);

            var userMessages = messages
                .GroupBy(m => m.NguoiGuiId == userId ? m.NguoiNhanId : m.NguoiGuiId)
                .Select(group =>
                {
                    var lastMsg = group.OrderByDescending(m => m.CreatedAt).First();
                    var unreadCount = group.Count(m => m.NguoiNhanId == userId && !m.IsRead);

                    return new ChatSummaryDto
                    {
                        IsNhom = false,
                        Id = group.Key ?? Guid.Empty,
                        TinNhanMoiNhat = lastMsg.TinNhan,
                        ThoiGianNhan = lastMsg.CreatedAt,
                        SoLuongChuaXem = unreadCount
                    };
                }).ToList();

            var joinedGroups = await _unitOfWork.ChatGroupMembers.FindAsync(m => m.ThanhVienId == userId);
            var groupIds = joinedGroups.Select(j => j.NhomId).ToList();

            var groupMessages = await _unitOfWork.ChatMessages
                .FindAsync(m => m.NhomId != null && groupIds.Contains(m.NhomId.Value) && !m.IsDeleted);

            var groupData = groupMessages
                .GroupBy(m => m.NhomId.Value)
                .Select(group =>
                {
                    var lastMsg = group.OrderByDescending(m => m.CreatedAt).First();
                    var unreadCount = group.Count(m => !m.IsRead && m.NguoiGuiId != userId);

                    return new ChatSummaryDto
                    {
                        IsNhom = true,
                        Id = group.Key,
                        TinNhanMoiNhat = lastMsg.TinNhan,
                        ThoiGianNhan = lastMsg.CreatedAt,
                        SoLuongChuaXem = unreadCount
                    };
                }).ToList();

            var users = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted);
            var userMap = users.ToDictionary(u => u.Id, u => u.HoVaTen);

            var groups = await _unitOfWork.ChatGroups.FindAsync(g => true);
            var groupMap = groups.ToDictionary(g => g.Id, g => g.TenNhom);

            var all = userMessages.Concat(groupData)
                .Select(x => {
                    x.Ten = x.IsNhom
                        ? groupMap.GetValueOrDefault(x.Id)
                        : userMap.GetValueOrDefault(x.Id);
                    return x;
                })
                .OrderByDescending(x => x.ThoiGianNhan)
                .ToList();

            return Ok(all);
        }


        [HttpPut("message/{id}")]
        public async Task<IActionResult> EditMessage(Guid id, [FromBody] EditMessageDto dto)
        {
            var msg = await _unitOfWork.ChatMessages.GetByIdAsync(id);
            if (msg == null || msg.NguoiGuiId != _currentUserId) return Forbid();

            msg.TinNhan = dto.TinNhan;
            msg.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.ChatMessages.Update(msg);
            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpPut("read/private/{userId}")]
        public async Task<IActionResult> MarkPrivateMessagesAsRead(Guid userId)
        {
            var messages = await _unitOfWork.ChatMessages.FindAsync(m =>
                m.NguoiGuiId == userId &&
                m.NguoiNhanId == _currentUserId &&
                !m.IsDeleted &&
                m.IsRead != true);

            foreach (var msg in messages)
            {
                msg.IsRead = true;
                _unitOfWork.ChatMessages.Update(msg);
            }

            await _unitOfWork.CompleteAsync();
            return Ok();
        }


        [HttpDelete("message/{id}")]
        public async Task<IActionResult> DeleteMessage(Guid id)
        {
            var msg = await _unitOfWork.ChatMessages.GetByIdAsync(id);
            if (msg == null) return NotFound();

            bool isGroupOwner = msg.NhomId != null &&
                (await _unitOfWork.ChatGroups.GetByIdAsync(msg.NhomId.Value))?.TruongNhomId == _currentUserId;

            if (msg.NguoiGuiId != _currentUserId && !isGroupOwner) return Forbid();

            msg.IsDeleted = true;
            _unitOfWork.ChatMessages.Update(msg);
            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpDelete("messages")]
        public async Task<IActionResult> DeleteMultipleMessages([FromBody] List<Guid> ids)
        {
            var messages = await _unitOfWork.ChatMessages.FindAsync(m => ids.Contains(m.Id));
            foreach (var msg in messages)
            {
                bool isGroupOwner = msg.NhomId != null &&
                    (await _unitOfWork.ChatGroups.GetByIdAsync(msg.NhomId.Value))?.TruongNhomId == _currentUserId;
                if (msg.NguoiGuiId == _currentUserId || isGroupOwner)
                {
                    msg.IsDeleted = true;
                    _unitOfWork.ChatMessages.Update(msg);
                }
            }
            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpPost("group")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            var group = new ChatGroup
            {
                Id = Guid.NewGuid(),
                TenNhom = dto.TenNhom,
                TruongNhomId = _currentUserId
            };
            await _unitOfWork.ChatGroups.AddAsync(group);

            var members = dto.ThanhViens.Distinct().Append(_currentUserId).Distinct()
                .Select(uid => new ChatGroupMember { Id = Guid.NewGuid(), NhomId = group.Id, ThanhVienId = uid });

            foreach (var m in members)
                await _unitOfWork.ChatGroupMembers.AddAsync(m);

            await _unitOfWork.CompleteAsync();
            return Ok(group);
        }

        [HttpPut("group/{groupId}")]
        public async Task<IActionResult> RenameGroup(Guid groupId, [FromBody] UpdateGroupDto dto)
        {
            var group = await _unitOfWork.ChatGroups.GetByIdAsync(groupId);
            if (group == null || group.TruongNhomId != _currentUserId) return Forbid();

            group.TenNhom = dto.TenNhom;
            _unitOfWork.ChatGroups.Update(group);
            await _unitOfWork.CompleteAsync();
            return Ok(group);
        }

        [HttpPost("group/members/{groupId}")]
        public async Task<IActionResult> AddMembers(Guid groupId, [FromBody] List<Guid> userIds)
        {
            var group = await _unitOfWork.ChatGroups.GetByIdAsync(groupId);
            if (group == null || group.TruongNhomId != _currentUserId) return Forbid();

            var existing = await _unitOfWork.ChatGroupMembers.FindAsync(m => m.NhomId == groupId);
            var existingIds = existing.Select(m => m.ThanhVienId).ToHashSet();

            foreach (var id in userIds.Distinct().Where(id => !existingIds.Contains(id)))
            {
                await _unitOfWork.ChatGroupMembers.AddAsync(new ChatGroupMember { Id = Guid.NewGuid(), NhomId = groupId, ThanhVienId = id });
            }

            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpPut("read/group/{groupId}")]
        public async Task<IActionResult> MarkGroupMessagesAsRead(Guid groupId)
        {
            var messages = await _unitOfWork.ChatMessages.FindAsync(m =>
                m.NhomId == groupId &&
                !m.IsDeleted);

            var messageIds = messages.Select(m => m.Id).ToList();

            var existingReads = await _unitOfWork.ChatMessageReads.FindAsync(r =>
                r.ThanhVienId == _currentUserId && messageIds.Contains(r.TinNhanId));
            var readMessageIds = existingReads.Select(r => r.TinNhanId).ToHashSet();

            var unreadMessages = messages.Where(m => !readMessageIds.Contains(m.Id));

            foreach (var msg in unreadMessages)
            {
                var read = new ChatMessageRead
                {
                    Id = Guid.NewGuid(),
                    ThanhVienId = _currentUserId,
                    TinNhanId = msg.Id,
                    ThoiGianXem = DateTime.UtcNow
                };
                await _unitOfWork.ChatMessageReads.AddAsync(read);
            }

            await _unitOfWork.CompleteAsync();
            return Ok();
        }


        [HttpDelete("group/members/{groupId}")]
        public async Task<IActionResult> RemoveMembers(Guid groupId, [FromBody] List<Guid> userIds)
        {
            var group = await _unitOfWork.ChatGroups.GetByIdAsync(groupId);
            if (group == null || group.TruongNhomId != _currentUserId) return Forbid();

            var members = await _unitOfWork.ChatGroupMembers.FindAsync(m => m.NhomId == groupId && userIds.Contains(m.ThanhVienId));
            foreach (var m in members)
            {
                _unitOfWork.ChatGroupMembers.Remove(m);
            }
            await _unitOfWork.CompleteAsync();
            return Ok();
        }
    }
}