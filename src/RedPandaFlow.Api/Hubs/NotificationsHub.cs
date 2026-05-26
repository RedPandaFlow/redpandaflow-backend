using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RedPandaFlow.Api.Hubs
{
    [Authorize]
    public class NotificationsHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
            }
            await base.OnConnectedAsync();
        }

        public static string GroupName(string userId) => $"user-{userId}";
        public static string GroupName(Guid userId) => $"user-{userId}";
    }
}
