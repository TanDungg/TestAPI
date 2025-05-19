using Microsoft.AspNetCore.SignalR;

namespace AiImageGeneratorApi.Hubs
{
    public class ChatHub : Hub
    {
        // Gửi tin nhắn đến người dùng cụ thể
        public async Task SendPrivateMessage(Guid receiverId, string message)
        {
            var senderId = Context.UserIdentifier;
            if (Guid.TryParse(senderId, out var senderGuid))
            {
                await Clients.User(receiverId.ToString())
                    .SendAsync("ReceivePrivateMessage", senderGuid, message);
            }
        }

        // Gửi tin nhắn đến tất cả thành viên trong nhóm
        public async Task SendGroupMessage(Guid groupId, string message)
        {
            var senderId = Context.UserIdentifier;
            if (Guid.TryParse(senderId, out var senderGuid))
            {
                await Clients.Group(groupId.ToString())
                    .SendAsync("ReceiveGroupMessage", groupId, senderGuid, message);
            }
        }

        // Khi user join vào một nhóm
        public async Task JoinGroup(Guid groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
        }

        // Khi user rời nhóm
        public async Task LeaveGroup(Guid groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId.ToString());
        }
    }
}