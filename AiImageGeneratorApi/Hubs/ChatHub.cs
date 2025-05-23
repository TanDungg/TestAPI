using Microsoft.AspNetCore.SignalR;

namespace AiImageGeneratorApi.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessageToUser(string receiverId, object message)
        {
            await Clients.User(receiverId).SendAsync("ReceiveMessage", message);
        }

        public async Task SendMessageToGroup(string groupId, object message)
        {
            await Clients.Group(groupId).SendAsync("ReceiveGroupMessage", message);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId); 
            }
            await base.OnConnectedAsync();
        }
    }
}