using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EnglishCenterSystem.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.UserIdentifier == null)
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }
}