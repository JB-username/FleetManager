using FleetManager.Data;
using FleetManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Pages.AdminDashboard.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<UserRow> Users { get; set; } = new();

        public async Task OnGetAsync()
        {
            var users = await _context.Users
                .Include(u => u.Vehicle)
                .OrderBy(u => u.UserName)
                .ToListAsync();

            Users = new List<UserRow>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                Users.Add(new UserRow
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Vehicle = u.Vehicle,
                    RoleName = roles.FirstOrDefault() ?? "User",
                    IsActive = u.IsActive
                });
            }
        }

        public class UserRow
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public Vehicle? Vehicle { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }
    }
}
