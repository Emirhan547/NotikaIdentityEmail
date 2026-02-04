using Microsoft.AspNetCore.SignalR;

namespace NotikaIdentityEmail.Hubs
{
    public class NotificationHub : Hub
    {
        public Task JoinUserGroup(string userEmail)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, userEmail);
        }
    }
}
