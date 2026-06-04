using FleetManager.Data;
using FleetManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Pages
{
    [Authorize]
    public class SuccessModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SuccessModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public OdometerReading? Reading { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Reading = await _context.OdometerReadings
                .Include(r => r.Vehicle)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (Reading == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            // Allow admins or the user who created the reading
            if (!User.IsInRole("Admin") && !string.Equals(currentUserId, Reading.UserId, System.StringComparison.Ordinal))
            {
                // Hide existence of the record to unauthorized users
                return NotFound();
            }

            return Page();
        }
    }
}
