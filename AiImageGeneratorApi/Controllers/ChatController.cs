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

            var sender = await _unitOfWork.Users.GetByIdAsync(_currentUserId);
            if (string.IsNullOrWhiteSpace(sender.PublicKey))
                return StatusCode(500, "Người gửi chưa có khóa công khai");

            var encrypted = HybridEncryptionHelper.Encrypt(dto.TinNhan);

            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                NguoiGuiId = _currentUserId,
                EncryptedMessage = encrypted.EncryptedMessage,
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

            // Mã hóa khóa AES cho người nhận hoặc từng thành viên nhóm
            var encryptedKeys = new List<ChatMessageKey>();

            if (dto.NguoiNhanId != null) // Chat 1-1
            {
                var receiver = await _unitOfWork.Users.GetByIdAsync(dto.NguoiNhanId.Value);
                if (receiver == null || string.IsNullOrWhiteSpace(receiver.PublicKey))
                    return BadRequest("Người nhận không tồn tại hoặc thiếu khóa công khai");

                var keyForSender = HybridEncryptionHelper.EncryptAESKeyForUser(encrypted.AESKey, sender.PublicKey);
                var keyForReceiver = HybridEncryptionHelper.EncryptAESKeyForUser(encrypted.AESKey, receiver.PublicKey);

                encryptedKeys.Add(new ChatMessageKey { Id = Guid.NewGuid(), TinNhanId = message.Id, ThanhVienId = sender.Id, EncryptedKey = keyForSender });
                encryptedKeys.Add(new ChatMessageKey { Id = Guid.NewGuid(), TinNhanId = message.Id, ThanhVienId = receiver.Id, EncryptedKey = keyForReceiver });
            }
            else if (dto.NhomId != null) // Chat nhóm
            {
                var members = await _unitOfWork.ChatGroupMembers.FindAsync(m =>
                    m.NhomId == dto.NhomId.Value && !m.IsDeleted);

                // ✅ Nếu người gửi không còn trong nhóm → im lặng không gửi, không trả lỗi
                if (!members.Any(m => m.ThanhVienId == _currentUserId))
                    return Ok(); // Không thông báo gì cả

                var users = await _unitOfWork.Users.FindAsync(u => members.Select(m => m.ThanhVienId).Contains(u.Id));

                foreach (var user in users)
                {
                    if (string.IsNullOrWhiteSpace(user.PublicKey)) continue;

                    var key = HybridEncryptionHelper.EncryptAESKeyForUser(encrypted.AESKey, user.PublicKey);
                    encryptedKeys.Add(new ChatMessageKey
                    {
                        Id = Guid.NewGuid(),
                        TinNhanId = message.Id,
                        ThanhVienId = user.Id,
                        EncryptedKey = key
                    });
                }
            }

            message.MessageKeys = encryptedKeys;

            await _unitOfWork.ChatMessages.AddAsync(message);
            await _unitOfWork.CompleteAsync();

            string encryptedKey = (_currentUserId == message.NguoiGuiId) ? message.EncryptedKeyForSender : message.EncryptedKeyForReceiver;

            var messageDto = new ChatMessageDto
            {
                Id = message.Id,
                IsNhom = message.NhomId != null,
                NhomId = message.NhomId,
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

            var userMessages = new List<ListChatDto>();
            var oneToOneLastSenders = new HashSet<Guid>();

            foreach (var group in messages.GroupBy(m => m.NguoiGuiId == userId ? m.NguoiNhanId : m.NguoiGuiId))
            {
                var lastMsg = group.OrderByDescending(m => m.CreatedAt).First();
                string decrypted = "[Không thể giải mã]";

                try
                {
                    if (lastMsg.IsThongBao)
                    {
                        decrypted = lastMsg.TinNhan ?? "[Thông báo]";
                        var hoVaTen = currentUser.HoVaTen?.Trim();
                        if (!string.IsNullOrEmpty(hoVaTen) && decrypted.Contains(hoVaTen))
                        {
                            decrypted = decrypted.Replace(hoVaTen, "Bạn");
                        }
                    }
                    else if (!string.IsNullOrEmpty(lastMsg.EncryptedMessage) && !string.IsNullOrEmpty(lastMsg.IV))
                    {
                        var keyEntry = await _unitOfWork.ChatMessageKeys
                            .FindAsync(k => k.TinNhanId == lastMsg.Id && k.ThanhVienId == userId);
                        var key = keyEntry.FirstOrDefault()?.EncryptedKey;

                        if (!string.IsNullOrEmpty(key))
                        {
                            decrypted = HybridEncryptionHelper.Decrypt(
                                lastMsg.EncryptedMessage,
                                key,
                                lastMsg.IV,
                                currentUser.PrivateKey
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi giải mã tin nhắn ID: {MessageId}", lastMsg.Id);
                }

                oneToOneLastSenders.Add(lastMsg.NguoiGuiId);

                userMessages.Add(new ListChatDto
                {
                    IsNhom = false,
                    Id = group.Key ?? Guid.Empty,
                    TinNhanMoiNhat = decrypted,
                    ThoiGianNhan = lastMsg.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    SoLuongChuaXem = group.Count(m => m.NguoiNhanId == userId && !m.IsRead),
                    IsGui = lastMsg.NguoiGuiId == userId,
                    IsThongBao = lastMsg.IsThongBao,
                    TenNguoiGui = lastMsg.NguoiGuiId == userId ? "Bạn" : null // sẽ gán sau nếu khác user
                });
            }

            // --- Tin nhắn nhóm ---
            var groupMemberships = await _unitOfWork.ChatGroupMembers.FindAsync(m => m.ThanhVienId == userId);
            var activeGroupIds = groupMemberships.Select(m => m.NhomId).ToList();
            var groupDeletedMap = groupMemberships.ToDictionary(m => m.NhomId, m => m.DeletedAt);

            var groupMessages = await _unitOfWork.ChatMessages
                .FindAsync(m => m.NhomId != null && activeGroupIds.Contains(m.NhomId.Value) && !m.IsDeleted);

            var allGroupMessageIds = groupMessages.Select(m => m.Id).ToList();
            var groupReads = await _unitOfWork.ChatMessageReads
                .FindAsync(r => r.ThanhVienId == userId && allGroupMessageIds.Contains(r.TinNhanId));

            var readMessageIdSet = new HashSet<Guid>(groupReads.Select(r => r.TinNhanId));
            var groupData = new List<ListChatDto>();
            var groupLastSenders = new HashSet<Guid>();

            foreach (var group in groupMessages.GroupBy(m => m.NhomId.Value))
            {
                var deletedAt = groupDeletedMap.GetValueOrDefault(group.Key);
                var filteredGroup = group
                    .Where(m => !deletedAt.HasValue || m.CreatedAt <= deletedAt.Value)
                    .ToList();

                if (!filteredGroup.Any()) continue;

                var lastMsg = filteredGroup.OrderByDescending(m => m.CreatedAt).First();
                string decrypted = "[Không thể giải mã]";

                try
                {
                    if (lastMsg.IsThongBao)
                    {
                        decrypted = lastMsg.TinNhan ?? "[Thông báo]";
                        var hoVaTen = currentUser.HoVaTen?.Trim();
                        if (!string.IsNullOrEmpty(hoVaTen) && decrypted.Contains(hoVaTen))
                        {
                            decrypted = decrypted.Replace(hoVaTen, "Bạn");
                        }
                    }
                    else if (!string.IsNullOrEmpty(lastMsg.EncryptedMessage) && !string.IsNullOrEmpty(lastMsg.IV))
                    {
                        var keyEntry = await _unitOfWork.ChatMessageKeys
                            .FindAsync(k => k.TinNhanId == lastMsg.Id && k.ThanhVienId == userId);
                        var key = keyEntry.FirstOrDefault()?.EncryptedKey;

                        if (!string.IsNullOrEmpty(key))
                        {
                            decrypted = HybridEncryptionHelper.Decrypt(
                                lastMsg.EncryptedMessage,
                                key,
                                lastMsg.IV,
                                currentUser.PrivateKey
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi giải mã tin nhắn nhóm ID: {MessageId}", lastMsg.Id);
                }

                groupLastSenders.Add(lastMsg.NguoiGuiId);

                groupData.Add(new ListChatDto
                {
                    IsNhom = true,
                    Id = group.Key,
                    TinNhanMoiNhat = decrypted,
                    ThoiGianNhan = lastMsg.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    SoLuongChuaXem = filteredGroup.Count(m => m.NguoiGuiId != userId && !readMessageIdSet.Contains(m.Id)),
                    IsGui = lastMsg.NguoiGuiId == userId,
                    IsThongBao = lastMsg.IsThongBao,
                    TenNguoiGui = lastMsg.NguoiGuiId == userId ? "Bạn" : null // sẽ gán sau nếu khác user
                });
            }

            // --- Mapping tên người nhận/gửi + ảnh ---
            var userIds = userMessages.Select(x => x.Id).Distinct().ToList();
            var groupIdsToFetch = groupData.Select(x => x.Id).Distinct().ToList();

            var users = await _unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id) && !u.IsDeleted);
            var groups = await _unitOfWork.ChatGroups.FindAsync(g => groupIdsToFetch.Contains(g.Id));

            var userMap = users.ToDictionary(u => u.Id, u => u.HoVaTen);
            var userAvatarMap = users.ToDictionary(u => u.Id, u => u.HinhAnh);

            var groupMap = groups.ToDictionary(g => g.Id, g => g.TenNhom);
            var groupAvatarMap = groups.ToDictionary(g => g.Id, g => g.HinhAnh);

            // --- Lấy thêm tên người gửi nếu không phải "Bạn"
            var allSenderIds = oneToOneLastSenders.Union(groupLastSenders).ToList();
            var senders = await _unitOfWork.Users.FindAsync(u => allSenderIds.Contains(u.Id));
            var senderMap = senders.ToDictionary(u => u.Id, u => u.HoVaTen);

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

                    if (x.TenNguoiGui == null && senderMap.TryGetValue(x.IsGui ? userId : allSenderIds.FirstOrDefault(sid => sid != userId), out var tenNguoiGui))
                    {
                        x.TenNguoiGui = tenNguoiGui;
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
            var currentUser = await _currentUserInfo.Value;
            bool isRemovedFromGroup = false;

            if (isNhom)
            {
                var isMember = await _unitOfWork.ChatGroupMembers
                    .FindAsync(m => m.NhomId == id && m.ThanhVienId == _currentUserId);

                var member = isMember.FirstOrDefault();
                if (member == null)
                {
                    return NotFound("Nhóm không tồn tại hoặc bạn chưa từng tham gia.");
                }

                if (member.IsDeleted)
                {
                    isRemovedFromGroup = true;
                }
            }

            string sql = "EXEC sp_GetChatInfoMessages {0}, {1}, {2}";
            var result = await _unitOfWork.ChatMessages.ExecuteStoredProcedureAsync<ChatInfoMessage>(sql, isNhom, id, _currentUserId);
            if (result == null || result.Count == 0) return NotFound();

            var raw = result.First();
            var parsedNgays = JsonConvert.DeserializeObject<List<ChatMessageGroupedByDateDto>>(raw.List_Ngays ?? "[]");

            foreach (var group in parsedNgays)
            {
                foreach (var msg in group.List_Messages)
                {
                    try
                    {
                        if (msg.IsThongBao)
                        {
                            // Nếu là tin hệ thống, và có tên người dùng hiện tại trong tin → thay thành "Bạn"
                            var hoVaTen = currentUser.HoVaTen?.Trim();

                            msg.TinNhan ??= "[Thông báo]";
                            if (!string.IsNullOrEmpty(hoVaTen) && msg.TinNhan.Contains(hoVaTen))
                            {
                                msg.TinNhan = msg.TinNhan.Replace(hoVaTen, "Bạn");
                            }

                            continue;
                        }

                        var keyRecordList = await _unitOfWork.ChatMessageKeys.FindAsync(k =>
                            k.TinNhanId == msg.Id && k.ThanhVienId == _currentUserId);
                        var keyRecord = keyRecordList.FirstOrDefault();

                        if (keyRecord != null && !string.IsNullOrWhiteSpace(keyRecord.EncryptedKey))
                        {
                            msg.TinNhan = HybridEncryptionHelper.Decrypt(
                                msg.EncryptedMessage,
                                keyRecord.EncryptedKey,
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

            return Ok(new
            {
                raw.Id,
                raw.Ten,
                raw.HinhAnh,
                raw.IsNhom,
                raw.SoLuongThanhVien,
                List_Ngays = parsedNgays,
                IsRemovedFromGroup = isRemovedFromGroup // ✅ Biến bổ sung
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