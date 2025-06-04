using Microsoft.AspNetCore.SignalR;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AiImageGeneratorApi.Hubs
{
    public class ChatHub : Hub
    {
        // Khi client gọi để tham gia 1 group
        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        }

        // Khi client rời khỏi group
        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task SendMessage(Dictionary<string, object> message)
        {
            string nhomId = message.ContainsKey("NhomId") ? message["NhomId"]?.ToString() : null;
            string nguoiNhanId = message.ContainsKey("NguoiNhanId") ? message["NguoiNhanId"]?.ToString() : null;

            if (!string.IsNullOrEmpty(nhomId))
            {
                await Clients.Group(nhomId).SendAsync("ReceiveNotification", message);
            }
            else if (!string.IsNullOrEmpty(nguoiNhanId))
            {
                await Clients.User(nguoiNhanId).SendAsync("ReceiveNotification", message);
            }

            // Ghi log
            var logPath = Path.Combine(AppContext.BaseDirectory, "signalr-messages.txt");
            await File.AppendAllTextAsync(logPath,
                $"[{DateTime.Now}] Sent message from {Context.UserIdentifier} to {(nhomId ?? nguoiNhanId)}{Environment.NewLine}");
        }


        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier ?? "(null)";
            var connectionId = Context.ConnectionId;

            var logPath = Path.Combine(AppContext.BaseDirectory, "signalr-log.txt");
            File.AppendAllText(logPath,
                $"[{DateTime.Now}] Connected: ConnectionId={connectionId}, UserIdentifier={userId}{Environment.NewLine}");

            return base.OnConnectedAsync();
        }
    }
}
