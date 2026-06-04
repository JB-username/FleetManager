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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public ApplicationUser ApplicationUser { get; set; } = new();

        [BindProperty]
        public string Password { get; set; } = "";

        public List<SelectListItem> Vehicles { get; set; } = new();

        public async Task OnGetAsync()
        {
            Vehicles = await _context.Vehicles
                .Where(v => v.IsActive)
                .OrderBy(v => v.RegistrationNumber)
                .Select(v => new SelectListItem(v.RegistrationNumber, v.Id.ToString()))
                .ToListAsync();

            Vehicles.Insert(0, new SelectListItem("-- Unassigned --", ""));
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = ApplicationUser.Email,
                Email = ApplicationUser.Email,
                FullName = ApplicationUser.FullName,
                VehicleId = ApplicationUser.VehicleId,
                IsActive = ApplicationUser.IsActive
            };

            var result = await _userManager.CreateAsync(user, Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await OnGetAsync();
                return Page();
            }

            return RedirectToPage("Index");
        }
    }
}
