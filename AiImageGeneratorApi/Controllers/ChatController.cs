using AiImageGeneratorApi.Hubs;
using AiImageGeneratorApi.Interfaces;
using AiImageGeneratorApi.Models.DTOs;
using AiImageGeneratorApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AiImageGeneratorApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
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

        [HttpGet("messages/{userId}")]
        public async Task<IActionResult> GetPrivateMessagesBySP(Guid userId)
        {
            var currentUserId = _currentUserId;

            string sql = "EXEC sp_GetPrivateMessages {0}, {1}";
            var result = await _unitOfWork.ChatMessages.ExecuteStoredProcedureAsync<ChatMessageDto>(sql, currentUserId, userId);

            return Ok(result);
        }




        [HttpGet("group/messages/{groupId}")]
        public async Task<IActionResult> GetGroupMessages(Guid groupId)
        {
            var isMember = await _unitOfWork.ChatGroupMembers.FindAsync(m => m.NhomId == groupId && m.ThanhVienId == _currentUserId);
            if (isMember == null) return Forbid();

            var messages = await _unitOfWork.ChatMessages.FindAsync(m => m.NhomId == groupId && !m.IsDeleted);
            return Ok(messages.OrderBy(m => m.CreatedAt));
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