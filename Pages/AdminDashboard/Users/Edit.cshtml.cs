using FleetManager.Data;
using FleetManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Pages.AdminDashboard.Users
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public ApplicationUser ApplicationUser { get; set; } = new();

        [BindProperty]
        public string? Password { get; set; }

        public List<SelectListItem> Vehicles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _context.Users
                .Include(u => u.Vehicle)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            ApplicationUser = user;

            Vehicles = await _context.Vehicles
                .Where(v => v.IsActive)
                .OrderBy(v => v.RegistrationNumber)
                .Select(v => new SelectListItem(v.RegistrationNumber, v.Id.ToString()))
                .ToListAsync();

            Vehicles.Insert(0, new SelectListItem("-- Unassigned --", ""));

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(ApplicationUser.Id);
                return Page();
            }

            var user = await _userManager.FindByIdAsync(ApplicationUser.Id);

            if (user == null)
                return NotFound();

            user.FullName = ApplicationUser.FullName;
            user.Email = ApplicationUser.Email;
            user.UserName = ApplicationUser.Email;
            user.VehicleId = ApplicationUser.VehicleId;
            user.IsActive = ApplicationUser.IsActive;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                await OnGetAsync(ApplicationUser.Id);
                return Page();
            }

            if (!string.IsNullOrEmpty(Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var pwResult = await _userManager.ResetPasswordAsync(user, token, Password);

                if (!pwResult.Succeeded)
                {
                    foreach (var error in pwResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    await OnGetAsync(ApplicationUser.Id);
                    return Page();
                }
            }

            return RedirectToPage("Index");
        }
    }
}
