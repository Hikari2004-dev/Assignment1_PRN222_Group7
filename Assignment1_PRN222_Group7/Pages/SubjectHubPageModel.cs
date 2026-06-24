using Assignment1_PRN222_Group7.Hubs;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages
{
    public abstract class SubjectHubPageModel : PageModel
    {
        protected readonly IHubContext<SubjectHub> _hubContext;

        protected SubjectHubPageModel(IHubContext<SubjectHub> hubContext)
        {
            _hubContext = hubContext;
        }

        protected async Task NotifySubjectCreated(Assignment1_PRN222_Group7_DAL.Entities.Subject subject)
        {
            await _hubContext.Clients.All.SendAsync("SubjectCreated", new
            {
                id = subject.Id,
                code = subject.Code,
                name = subject.Name,
                description = subject.Description,
                isActive = subject.IsActive,
                chaptersCount = 0
            });
        }

        protected async Task NotifySubjectUpdated(Assignment1_PRN222_Group7_DAL.Entities.Subject subject)
        {
            await _hubContext.Clients.All.SendAsync("SubjectUpdated", new
            {
                id = subject.Id,
                code = subject.Code,
                name = subject.Name,
                description = subject.Description,
                isActive = subject.IsActive
            });
        }

        protected async Task NotifySubjectDeleted(int id)
        {
            await _hubContext.Clients.All.SendAsync("SubjectDeleted", id);
        }
    }
}
