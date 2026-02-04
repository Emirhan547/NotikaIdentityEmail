using Microsoft.AspNetCore.SignalR;

namespace NotikaIdentityEmail.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinUserGroup(string userEmail)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userEmail);
            _logger.LogInformation(
                "User {UserEmail} joined SignalR group. ConnectionId: {ConnectionId}",
                userEmail,
                Context.ConnectionId
            );
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("SignalR client connected. ConnectionId: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception, "SignalR client disconnected with error. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation("SignalR client disconnected. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}