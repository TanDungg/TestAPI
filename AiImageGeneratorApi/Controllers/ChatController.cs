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
        private readonly Lazy<Task<User>> _currentUserInfo;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IHubContext<ChatHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _currentUserId = Guid.Parse(httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            _currentUserInfo = new Lazy<Task<User>>(() => _unitOfWork.Users.GetByIdAsync(_currentUserId));
        }

        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TinNhan) && (dto.List_Files == null || !dto.List_Files.Any()))
                return BadRequest("Phải có nội dung hoặc file đính kèm");

            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                NguoiGuiId = _currentUserId,
                TinNhan = dto.TinNhan,
                CreatedAt = DateTime.Now,
                NguoiNhanId = dto.NguoiNhanId,
                NhomId = dto.NhomId
            };

            if (dto.List_Files != null)
            {
                foreach (var f in dto.List_Files)
                {
                    message.Files.Add(new ChatMessageFile
                    {
                        Id = Guid.NewGuid(),
                        FileUrl = f.FileUrl,
                        ChatMessageId = message.Id,
                        CreatedAt = DateTime.Now,
                        CreatedBy = _currentUserId
                    });
                }
            }

            await _unitOfWork.ChatMessages.AddAsync(message);
            await _unitOfWork.CompleteAsync();

            var currentUser = await _currentUserInfo.Value;

            string tenNhom = null;
            string hinhAnh = null;
            if (message.NhomId.HasValue)
            {
                var nhom = await _unitOfWork.ChatGroups.GetByIdAsync(message.NhomId.Value);
                tenNhom = nhom?.TenNhom;
                hinhAnh = nhom?.HinhAnh;
            }

            var messageDto = new
            {
                message.Id,
                message.TinNhan,
                message.NhomId,
                message.NguoiGuiId,
                Ten = message.NhomId.HasValue ? tenNhom : currentUser.HoVaTen,
                HinhAnh = message.NhomId.HasValue ? hinhAnh : currentUser.HinhAnh,
                message.NguoiNhanId,
                message.CreatedAt,
                Files = message.Files.Select(f => new
                {
                    f.Id,
                    f.FileUrl
                }).ToList()
            };

            if (message.NhomId != null)
            {
                await _hubContext.Clients.Group(message.NhomId.ToString())
                    .SendAsync("ReceiveNotification", messageDto);
            }
            else
            {
                var nguoiNhanId = message.NguoiNhanId.ToString();
                //var nguoiGuiId = _currentUserId.ToString();

                await _hubContext.Clients.Users(nguoiNhanId)
                    .SendAsync("ReceiveNotification", messageDto);
            }

            return Ok(messageDto);
        }

        [HttpGet("list-message")]
        public async Task<IActionResult> GetListMessage()
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

                    return new ListChatDto
                    {
                        IsNhom = false,
                        Id = group.Key ?? Guid.Empty,
                        TinNhanMoiNhat = lastMsg.TinNhan,
                        ThoiGianNhan = lastMsg.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                        SoLuongChuaXem = unreadCount,
                        IsGui = lastMsg.NguoiGuiId == userId,
                        IsThongBao = lastMsg.IsThongBao
                    };
                }).ToList();

            var joinedGroups = await _unitOfWork.ChatGroupMembers.FindAsync(m => m.ThanhVienId == userId);
            var groupIds = joinedGroups.Select(j => j.NhomId).ToList();

            var groupMessages = await _unitOfWork.ChatMessages
                .FindAsync(m => m.NhomId != null && groupIds.Contains(m.NhomId.Value) && !m.IsDeleted);

            var allGroupMessageIds = groupMessages.Select(m => m.Id).ToList();

            var groupReads = await _unitOfWork.ChatMessageReads
                .FindAsync(r => r.ThanhVienId == userId && allGroupMessageIds.Contains(r.TinNhanId));

            var readMessageIdSet = new HashSet<Guid>(groupReads.Select(r => r.TinNhanId));

            var groupData = groupMessages
                .GroupBy(m => m.NhomId.Value)
                .Select(group =>
                {
                    var lastMsg = group.OrderByDescending(m => m.CreatedAt).First();
                    var unreadCount = group.Count(m => m.NguoiGuiId != userId && !readMessageIdSet.Contains(m.Id));

                    return new ListChatDto
                    {
                        IsNhom = true,
                        Id = group.Key,
                        TinNhanMoiNhat = lastMsg.TinNhan,
                        ThoiGianNhan = lastMsg.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                        SoLuongChuaXem = unreadCount,
                        IsGui = lastMsg.NguoiGuiId == userId,
                        IsThongBao = lastMsg.IsThongBao
                    };
                }).ToList();

            var userIds = userMessages.Select(x => x.Id).Distinct().ToList();
            var groupIdsToFetch = groupData.Select(x => x.Id).Distinct().ToList();

            var users = await _unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id) && !u.IsDeleted);
            var groups = await _unitOfWork.ChatGroups.FindAsync(g => groupIdsToFetch.Contains(g.Id));

            var userMap = users.ToDictionary(u => u.Id, u => u.HoVaTen);
            var userAvatarMap = users.ToDictionary(u => u.Id, u => u.HinhAnh);

            var groupMap = groups.ToDictionary(g => g.Id, g => g.TenNhom);
            var groupAvatarMap = groups.ToDictionary(g => g.Id, g => g.HinhAnh);

            var all = userMessages.Concat(groupData)
                .Select(x =>
                {
                    if (x.IsNhom)
                    {
                        x.Ten = groupMap.GetValueOrDefault(x.Id);
                        x.HinhAnh = groupAvatarMap.GetValueOrDefault(x.Id);
                    }
                    else
                    {
                        x.Ten = userMap.GetValueOrDefault(x.Id);
                        x.HinhAnh = userAvatarMap.GetValueOrDefault(x.Id);
                    }

                    return x;
                })
                .OrderByDescending(x => x.ThoiGianNhan)
                .ToList();

            return Ok(all);
        }

        [HttpGet("messages")]
        public async Task<IActionResult> GetChatMessages([FromQuery] bool isNhom, [FromQuery] Guid id)
        {
            if (isNhom)
            {
                var isMember = await _unitOfWork.ChatGroupMembers.FindAsync(m =>
                    m.NhomId == id && m.ThanhVienId == _currentUserId);
                if (isMember == null || !isMember.Any()) return Forbid();
            }

            string sql = "EXEC sp_GetChatInfoMessages {0}, {1}, {2}";
            var result = await _unitOfWork.ChatMessages
                .ExecuteStoredProcedureAsync<ChatInfoMessage>(sql, isNhom, id, _currentUserId);

            if (result == null || result.Count == 0)
                return NotFound();

            var raw = result.First();

            var parsedNgays = new List<ChatMessageGroupedByDateDto>();
            if (!string.IsNullOrWhiteSpace(raw.List_Ngays))
            {
                parsedNgays = JsonConvert.DeserializeObject<List<ChatMessageGroupedByDateDto>>(raw.List_Ngays);
            }

            return Ok(new
            {
                raw.Id,
                raw.Ten,
                raw.HinhAnh,
                raw.IsNhom,
                raw.SoLuongThanhVien,
                List_Ngays = parsedNgays
            });
        }

        [HttpGet("group")]
        public async Task<IActionResult> GetMyGroups()
        {
            var groups = await (
                from m in _unitOfWork.DbContext.ChatGroupMembers
                join g in _unitOfWork.DbContext.ChatGroups on m.NhomId equals g.Id
                where m.ThanhVienId == _currentUserId && !m.IsDeleted && !g.IsDeleted
                select new
                {
                    g.Id,
                    g.TenNhom,
                    g.HinhAnh,
                    g.TruongNhomId
                }
            ).ToListAsync();

            return Ok(groups);
        }


        [HttpPost("group")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            var group = new ChatGroup
            {
                Id = Guid.NewGuid(),
                TenNhom = dto.TenNhom,
                HinhAnh = dto.HinhAnh,
                TruongNhomId = _currentUserId,
                CreatedAt = DateTime.Now,
                CreatedBy = _currentUserId,
            };
            await _unitOfWork.ChatGroups.AddAsync(group);

            var members = dto.ThanhViens.Distinct().Append(_currentUserId).Distinct()
                .Select(uid => new ChatGroupMember { Id = Guid.NewGuid(), NhomId = group.Id, ThanhVienId = uid });

            foreach (var m in members)
                await _unitOfWork.ChatGroupMembers.AddAsync(m);

            var currentUser = await _currentUserInfo.Value;
            var systemMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                NguoiGuiId = _currentUserId,
                NhomId = group.Id,
                TinNhan = $"{currentUser?.HoVaTen ?? "Một thành viên"} đã tạo nhóm.",
                IsThongBao = true,
                LoaiThongBao = "CreateGroup",
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.ChatMessages.AddAsync(systemMessage);
            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpPut("group/{groupId}")]
        public async Task<IActionResult> RenameGroup(Guid groupId, [FromBody] UpdateGroupDto dto)
        {
            var group = await _unitOfWork.ChatGroups.GetByIdAsync(groupId);
            if (group == null || group.TruongNhomId != _currentUserId) return Forbid();

            group.TenNhom = dto.TenNhom;
            _unitOfWork.ChatGroups.Update(group);
            await _unitOfWork.CompleteAsync();
            return Ok();
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

            var allUsers = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted);
            var addedNames = allUsers.Where(u => userIds.Contains(u.Id)).Select(u => u.HoVaTen).ToList();
            var notifyMsg = new ChatMessage
            {
                Id = Guid.NewGuid(),
                NguoiGuiId = _currentUserId,
                NhomId = groupId,
                TinNhan = $"{string.Join(", ", addedNames)} đã được thêm vào nhóm.",
                IsThongBao = true,
                LoaiThongBao = "AddMembers",
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.ChatMessages.AddAsync(notifyMsg);

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

            var allUsers = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted);
            var removedNames = allUsers.Where(u => userIds.Contains(u.Id)).Select(u => u.HoVaTen).ToList();
            var notifyMsg = new ChatMessage
            {
                Id = Guid.NewGuid(),
                NguoiGuiId = _currentUserId,
                NhomId = groupId,
                TinNhan = $"{string.Join(", ", removedNames)} đã bị xoá khỏi nhóm.",
                IsThongBao = true,
                LoaiThongBao = "RemovedMembers",
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.ChatMessages.AddAsync(notifyMsg);

            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpDelete("group/{groupId}")]
        public async Task<IActionResult> DeleteGroup(Guid groupId)
        {
            var group = await _unitOfWork.ChatGroups.GetByIdAsync(groupId);
            if (group == null)
                return NotFound();

            if (group.TruongNhomId != _currentUserId)
                return Forbid();

            group.IsDeleted = true;
            group.DeletedAt = DateTime.Now;
            group.DeletedBy = _currentUserId;

            _unitOfWork.ChatGroups.Update(group);
            await _unitOfWork.CompleteAsync();

            return Ok();
        }

        [HttpPut("read/{id}")]
        public async Task<IActionResult> MarkMessagesAsRead(Guid id, [FromQuery] bool isNhom)
        {
            if (id == Guid.Empty)
                return BadRequest("Thiếu thông tin id");

            if (isNhom)
            {
                // Lấy tất cả tin nhắn nhóm chưa bị xóa
                var list_messages = await _unitOfWork.ChatMessages.FindAsync(m =>
                    m.NhomId == id &&
                    !m.IsDeleted);

                var messageIds = list_messages.Select(m => m.Id).ToList();

                // Lấy các bản ghi đã đọc của user hiện tại
                var existingReads = await _unitOfWork.ChatMessageReads.FindAsync(r =>
                    r.ThanhVienId == _currentUserId && messageIds.Contains(r.TinNhanId));

                var readMessageIds = new HashSet<Guid>(existingReads.Select(r => r.TinNhanId));

                // Chỉ chọn những tin chưa đọc
                var unreadMessages = list_messages.Where(m => !readMessageIds.Contains(m.Id)).ToList();

                foreach (var msg in unreadMessages)
                {
                    var read = new ChatMessageRead
                    {
                        Id = Guid.NewGuid(),
                        ThanhVienId = _currentUserId,
                        TinNhanId = msg.Id,
                        ThoiGianXem = DateTime.Now
                    };

                    await _unitOfWork.ChatMessageReads.AddAsync(read);
                }
            }

            else
            {
                // Đánh dấu đã đọc tin nhắn 1-1
                var messages = await _unitOfWork.ChatMessages.FindAsync(m =>
                    m.NguoiGuiId == id &&
                    m.NguoiNhanId == _currentUserId &&
                    !m.IsDeleted &&
                    m.IsRead != true);

                foreach (var msg in messages)
                {
                    msg.IsRead = true;
                    _unitOfWork.ChatMessages.Update(msg);
                }
            }

            await _unitOfWork.CompleteAsync();
            return Ok();
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages([FromQuery] Guid? userId, [FromQuery] Guid? groupId, [FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest("Từ khóa không được để trống.");

            if (userId == null && groupId == null)
                return BadRequest("Phải cung cấp userId hoặc groupId.");

            IQueryable<ChatMessage> query = _unitOfWork.ChatMessages.AsQueryable();

            if (groupId != null)
            {
                // Kiểm tra người dùng có trong nhóm không
                var isMember = await _unitOfWork.ChatGroupMembers.FindAsync(m => m.NhomId == groupId && m.ThanhVienId == _currentUserId);
                if (isMember == null || !isMember.Any()) return Forbid();

                query = query.Where(m => m.NhomId == groupId && !m.IsDeleted && m.TinNhan.Contains(keyword));
            }
            else if (userId != null)
            {
                query = query.Where(m =>
                    m.NhomId == null &&
                    !m.IsDeleted &&
                    (
                        (m.NguoiGuiId == _currentUserId && m.NguoiNhanId == userId) ||
                        (m.NguoiGuiId == userId && m.NguoiNhanId == _currentUserId)
                    ) &&
                    m.TinNhan.Contains(keyword));
            }

            var results = await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(100) // Giới hạn kết quả
                .Select(m => new
                {
                    m.Id,
                    m.TinNhan,
                    m.CreatedAt,
                    m.NguoiGuiId,
                    m.NguoiNhanId,
                    m.NhomId,
                    Files = m.Files.Select(f => new { f.Id, f.FileUrl })
                })
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("search-all")]
        public async Task<IActionResult> SearchAll([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest("Từ khóa không được để trống.");

            var userId = _currentUserId;

            string sql = "EXEC sp_SearchUsersAndMessages @UserId, @Keyword";

            using var multi = await _unitOfWork.Connection.QueryMultipleAsync(
                sql,
                new { UserId = userId, Keyword = keyword },
                transaction: _unitOfWork.Transaction);

            var users = (await multi.ReadAsync()).ToList();
            var messages = (await multi.ReadAsync()).ToList();

            return Ok(new
            {
                Users = users,
                Messages = messages
            });
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
    }
}