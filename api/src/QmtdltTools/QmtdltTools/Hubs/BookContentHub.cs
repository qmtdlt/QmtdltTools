using Microsoft.AspNetCore.SignalR;
using Volo.Abp.AspNetCore.SignalR;

namespace QmtdltTools.Hubs;

public class BookContentHub:AbpHub
{
    public async Task SendMessage(string message)
    {
        var currentUserName = CurrentUser.Name;
        var text = L["MyText"];
        int cnt = 0;
        while (true)
        {
            await Clients.All.SendAsync("onReceiveFromSignalRHub", message + CurrentUser.Name);
            await Task.Delay(1000);
            if (cnt++ > 10)
            {
                return;
            }
        }
    }
}