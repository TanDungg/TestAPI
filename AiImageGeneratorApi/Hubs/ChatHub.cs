using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace AiImageGeneratorApi.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        }
        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }
        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier ?? "(null)";
            var connectionId = Context.ConnectionId;

            var logPath = Path.Combine(AppContext.BaseDirectory, "signalr-log.txt");
            File.AppendAllText(logPath, $"[{DateTime.Now}] Connected: ConnectionId={connectionId}, UserIdentifier={userId}{Environment.NewLine}");

            return base.OnConnectedAsync();
        }


    }
}
