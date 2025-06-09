using AiImageGeneratorApi.Hubs;
using AiImageGeneratorApi.Interfaces;
using AiImageGeneratorApi.Models.Entities;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AiImageGeneratorApi.Helpers
{
    public interface IChatGroupHelper
    {
        Task CreateSystemMessageAsync(Guid groupId, string loaiThongBao, string tinNhan, Guid currentUserId);
    }

    public class ChatGroupHelper : IChatGroupHelper
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatGroupHelper(IUnitOfWork unitOfWork, IHubContext<ChatHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        private async Task NotifyUsersInGroup(ChatMessage msg, object messageDto)
        {
            var members = await _unitOfWork.ChatGroupMembers
                .FindAsync(m => m.NhomId == msg.NhomId && !m.IsDeleted);

            foreach (var member in members)
            {
                await _hubContext.Clients.User(member.ThanhVienId.ToString())
                    .SendAsync("ReceiveNotification", messageDto);
            }
        }

        public async Task CreateSystemMessageAsync(Guid groupId, string loaiThongBao, string tinNhan, Guid currentUserId)
        {
            var systemMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                NguoiGuiId = currentUserId,
                NhomId = groupId,
                TinNhan = tinNhan,
                IsThongBao = true,
                LoaiThongBao = loaiThongBao,
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.ChatMessages.AddAsync(systemMessage);
            await _unitOfWork.CompleteAsync();

            var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);

            var messageDto = new
            {
                systemMessage.Id,
                systemMessage.TinNhan,
                systemMessage.NhomId,
                systemMessage.NguoiGuiId,
                Ten = currentUser.HoVaTen,
                currentUser.HinhAnh,
                systemMessage.CreatedAt,
                Files = new List<object>()
            };

            await NotifyUsersInGroup(systemMessage, messageDto);
        }
    }
}
