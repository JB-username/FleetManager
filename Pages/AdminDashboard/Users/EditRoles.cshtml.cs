using FleetManager.Data;
using FleetManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace FleetManager.Pages.AdminDashboard.Users
{
    [Authorize(Roles = "Admin")]
    public class EditRolesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditRolesModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public string UserId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public List<SelectListItem> Roles { get; set; } = new();

        [BindProperty]
        public string SelectedRole { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            UserId = user.Id;
            Email = user.Email ?? string.Empty;

            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            Roles = roles.Select(r => new SelectListItem(r.Name, r.Name)).ToList();

            var userRoles = await _userManager.GetRolesAsync(user);
            SelectedRole = userRoles.FirstOrDefault() ?? "User";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove all current roles then add selected
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Failed to remove existing roles.");
                return Page();
            }

            if (!string.IsNullOrEmpty(SelectedRole))
            {
                if (!await _roleManager.RoleExistsAsync(SelectedRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(SelectedRole));
                }

                var addResult = await _userManager.AddToRoleAsync(user, SelectedRole);
                if (!addResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Failed to add role to user.");
                    return Page();
                }
            }

            return RedirectToPage("Index");
        }
    }
}
