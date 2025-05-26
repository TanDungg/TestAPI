using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AiImageGeneratorApi.Hubs
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            // Lấy theo claim NameIdentifier
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
