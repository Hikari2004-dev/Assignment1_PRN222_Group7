using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Hubs
{
    public class SubjectHub : Hub
    {
        public async Task NotifySubjectCreated(object subject)
        {
            await Clients.All.SendAsync("SubjectCreated", subject);
        }

        public async Task NotifySubjectUpdated(object subject)
        {
            await Clients.All.SendAsync("SubjectUpdated", subject);
        }

        public async Task NotifySubjectDeleted(int id)
        {
            await Clients.All.SendAsync("SubjectDeleted", id);
        }
    }
}
