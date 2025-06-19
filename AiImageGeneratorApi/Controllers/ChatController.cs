using AiImageGeneratorApi.Data;
using AiImageGeneratorApi.Helpers;
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
using System.Text.RegularExpressions;

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
        private readonly IChatGroupHelper _chatGroupHelper;
        private readonly ILogger<ChatController> _logger;


        public ChatController(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IHubContext<ChatHub> hubContext,
            ApplicationDbContext dbContext,
            IChatGroupHelper chatGroupHelper,
            ILogger<ChatController> logger)
        {
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _hubContext = hubContext;
            _chatGroupHelper = chatGroupHelper;
            _currentUserId = Guid.Parse(httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            _currentUserInfo = new Lazy<Task<User>>(() => _unitOfWork.Users.GetByIdAsync(_currentUserId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TinNhan) && (dto.List_Files == null || !dto.List_Files.Any()))
                return BadRequest("Phải có nội dung hoặc file đính kèm");

            string recipientPublicKey;

            if (dto.NguoiNhanId != null)
            {
                var receiver = await _unitOfWork.Users.GetByIdAsync(dto.NguoiNhanId.Value);
                if (receiver == null || string.IsNullOrWhiteSpace(receiver.PublicKey))
                    return BadRequest("Người nhận không tồn tại hoặc thiếu khóa mã hóa.");
                recipientPublicKey = receiver.PublicKey;
            }
            else if (dto.NhomId != null)
            {
                var group = await _unitOfWork.ChatGroups.GetByIdAsync(dto.NhomId.Value);
                if (group == null) return BadRequest("Nhóm không tồn tại.");
                var groupOwner = await _unitOfWork.Users.GetByIdAsync(group.TruongNhomId);
                if (groupOwner == null || string.IsNullOrWhiteSpace(groupOwner.PublicKey))
                    return BadRequest("Trưởng nhóm thiếu khóa mã hóa.");
                recipientPublicKey = groupOwner.PublicKey;
            }
            else
            {
                return BadRequest("Thiếu thông tin người nhận hoặc nhóm.");
            }

            var sender = await _unitOfWork.Users.GetByIdAsync(_currentUserId);
            if (string.IsNullOrWhiteSpace(sender.PublicKey))
                return StatusCode(500, "Người gửi chưa có khóa công khai");

            var encrypted = HybridEncryptionHelper.EncryptForBoth(dto.TinNhan, sender.PublicKey, recipientPublicKey);

            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                NguoiGuiId = _currentUserId,
                EncryptedMessage = encrypted.EncryptedMessage,
                EncryptedKeyForSender = encrypted.EncryptedKeyForSender,
                EncryptedKeyForReceiver = encrypted.EncryptedKeyForReceiver,
                IV = encrypted.IV,
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

            string encryptedKey = (_currentUserId == message.NguoiGuiId) ? message.EncryptedKeyForSender : message.EncryptedKeyForReceiver;

            var messageDto = new ChatMessageDto
            {
                Id = message.Id,
                NguoiGuiId = message.NguoiGuiId,
                TenNguoiGui = sender.HoVaTen,
                HinhAnh = sender.HinhAnh,
                TinNhan = dto.TinNhan,
                NguoiNhanId = message.NguoiNhanId,
                ThoiGianGui = message.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                IsSend = true,
                IsRead = false,
                IsThongBao = message.IsThongBao,
                LoaiThongBao = null,
                EncryptedMessage = message.EncryptedMessage,
                EncryptedKey = encryptedKey,
                IV = message.IV,
                List_Files = message.Files.Select(f => new ChatFileDto { Id = f.Id, FileUrl = f.FileUrl }).ToList()
            };

            if (message.NhomId != null)
                await _hubContext.Clients.Group(message.NhomId.Value.ToString()).SendAsync("ReceiveNotification", messageDto);
            else
                await _hubContext.Clients.Users(message.NguoiNhanId.ToString()).SendAsync("ReceiveNotification", messageDto);

            return Ok(messageDto);
        }


        [HttpGet("list-message")]
        public async Task<IActionResult> GetListMessage()
        {
            var userId = _currentUserId;
            var currentUser = await _currentUserInfo.Value;

            // --- Tin nhắn 1-1 ---
            var messages = await _unitOfWork.ChatMessages
                .FindAsync(m => (m.NguoiGuiId == userId || m.NguoiNhanId == userId) && m.NhomId == null && !m.IsDeleted);

            var userMessages = messages
                .GroupBy(m => m.NguoiGuiId == userId ? m.NguoiNhanId : m.NguoiGuiId)
                .Select(group =>
                {
                    var lastMsg = group.OrderByDescending(m => m.CreatedAt).First();
                    string decrypted = "[Không thể giải mã]";
                    try
                    {
                        if (lastMsg.IsThongBao)
                        {
                            decrypted = lastMsg.TinNhan ?? "[Thông báo]";
                        }
                        else if (!string.IsNullOrEmpty(lastMsg.EncryptedMessage) &&
                                 !string.IsNullOrEmpty(lastMsg.EncryptedKeyForSender) &&
                                 !string.IsNullOrEmpty(lastMsg.EncryptedKeyForReceiver) &&
                                 !string.IsNullOrEmpty(lastMsg.IV))
                        {
                            var aesKeyEncrypted = (lastMsg.NguoiGuiId == userId)
                                ? lastMsg.EncryptedKeyForSender
                                : lastMsg.EncryptedKeyForReceiver;

                            decrypted = HybridEncryptionHelper.Decrypt(
                                lastMsg.EncryptedMessage,
                                aesKeyEncrypted,
                                lastMsg.IV,
                                currentUser.PrivateKey
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi giải mã tin nhắn ID: {MessageId}", lastMsg.Id);
                        decrypted = "[Không thể giải mã]";
                    }

                    return new ListChatDto
                    {
                        IsNhom = false,
                        Id = group.Key ?? Guid.Empty,
                        TinNhanMoiNhat = decrypted,
                        ThoiGianNhan = lastMsg.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                        SoLuongChuaXem = group.Count(m => m.NguoiNhanId == userId && !m.IsRead),
                        IsGui = lastMsg.NguoiGuiId == userId,
                        IsThongBao = lastMsg.IsThongBao
                    };
                }).ToList();

            // --- Tin nhắn nhóm ---
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
                    string decrypted = "[Không thể giải mã]";
                    try
                    {
                        if (lastMsg.IsThongBao)
                        {
                            decrypted = lastMsg.TinNhan ?? "[Thông báo]";
                        }
                        else if (!string.IsNullOrEmpty(lastMsg.EncryptedMessage) &&
                                 !string.IsNullOrEmpty(lastMsg.EncryptedKeyForSender) &&
                                 !string.IsNullOrEmpty(lastMsg.EncryptedKeyForReceiver) &&
                                 !string.IsNullOrEmpty(lastMsg.IV))
                        {
                            var aesKeyEncrypted = (lastMsg.NguoiGuiId == userId)
                                ? lastMsg.EncryptedKeyForSender
                                : lastMsg.EncryptedKeyForReceiver;

                            decrypted = HybridEncryptionHelper.Decrypt(
                                lastMsg.EncryptedMessage,
                                aesKeyEncrypted,
                                lastMsg.IV,
                                currentUser.PrivateKey
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi giải mã tin nhắn ID: {MessageId}", lastMsg.Id);
                        decrypted = "[Không thể giải mã]";
                    }

                    return new ListChatDto
                    {
                        IsNhom = true,
                        Id = group.Key,
                        TinNhanMoiNhat = decrypted,
                        ThoiGianNhan = lastMsg.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                        SoLuongChuaXem = group.Count(m => m.NguoiGuiId != userId && !readMessageIdSet.Contains(m.Id)),
                        IsGui = lastMsg.NguoiGuiId == userId,
                        IsThongBao = lastMsg.IsThongBao
                    };
                }).ToList();

            // --- Mapping tên + ảnh ---
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
                var isMember = await _unitOfWork.ChatGroupMembers.FindAsync(m => m.NhomId == id && m.ThanhVienId == _currentUserId);
                if (isMember == null || !isMember.Any()) return Forbid();
            }

            string sql = "EXEC sp_GetChatInfoMessages {0}, {1}, {2}";
            var result = await _unitOfWork.ChatMessages.ExecuteStoredProcedureAsync<ChatInfoMessage>(sql, isNhom, id, _currentUserId);

            if (result == null || result.Count == 0)
                return NotFound();

            var raw = result.First();
            var currentUser = await _currentUserInfo.Value;

            var parsedNgays = new List<ChatMessageGroupedByDateDto>();
            if (!string.IsNullOrWhiteSpace(raw.List_Ngays))
            {
                parsedNgays = JsonConvert.DeserializeObject<List<ChatMessageGroupedByDateDto>>(raw.List_Ngays);

                foreach (var ngay in parsedNgays)
                {
                    foreach (var msg in ngay.List_Messages)
                    {
                        try
                        {
                            if (msg.IsThongBao)
                            {
                                msg.TinNhan = msg.TinNhan ?? "[Thông báo]";
                            }
                            else if (!string.IsNullOrWhiteSpace(msg.EncryptedMessage) &&
                                     !string.IsNullOrWhiteSpace(msg.EncryptedKey) &&
                                     !string.IsNullOrWhiteSpace(msg.IV) &&
                                     !string.IsNullOrWhiteSpace(currentUser.PrivateKey))
                            {
                                msg.TinNhan = HybridEncryptionHelper.Decrypt(
                                    msg.EncryptedMessage,
                                    msg.EncryptedKey,
                                    msg.IV,
                                    currentUser.PrivateKey
                                );
                            }
                            else
                            {
                                msg.TinNhan = "[Không thể giải mã]";
                            }
                        }
                        catch
                        {
                            msg.TinNhan = "[Không thể giải mã]";
                        }
                    }
                }
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

        [HttpGet("images")]
        public async Task<IActionResult> GetAllImages([FromQuery] bool isNhom, [FromQuery] Guid id)
        {
            var query = _dbContext.ChatMessages
                .Where(m => !m.IsDeleted)
                .Include(m => m.Files)
                .AsQueryable();

            if (isNhom)
            {
                query = query.Where(m => m.NhomId == id);
            }
            else
            {
                query = query.Where(m =>
                    (m.NguoiGuiId == _currentUserId && m.NguoiNhanId == id) ||
                    (m.NguoiGuiId == id && m.NguoiNhanId == _currentUserId));
            }

            var result = await query
                .SelectMany(m => m.Files
                    .Where(f => f.FileUrl.EndsWith(".jpg") ||
                                f.FileUrl.EndsWith(".jpeg") ||
                                f.FileUrl.EndsWith(".png") ||
                                f.FileUrl.EndsWith(".gif") ||
                                f.FileUrl.EndsWith(".webp"))
                    .Select(f => new
                    {
                        f.Id,
                        f.FileUrl,
                        m.CreatedAt
                    }))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(result);
        }
    }
}