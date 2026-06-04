using FleetManager.Data;
using FleetManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace FleetManager.Pages.Files
{
    [Authorize]
    public class ServeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ServeModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var reading = await _context.OdometerReadings
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reading == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            if (!User.IsInRole("Admin") && !string.Equals(currentUserId, reading.UserId, System.StringComparison.Ordinal))
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(reading.ImagePath))
                return NotFound();

            string physicalPath;

            // If stored as relative web path (/uploads/...), map to webroot
            if (reading.ImagePath.StartsWith("/"))
            {
                physicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), reading.ImagePath.TrimStart('/'));
            }
            else
            {
                physicalPath = reading.ImagePath;
            }

            if (!System.IO.File.Exists(physicalPath))
                return NotFound();

            var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "application/octet-stream",
            };

            var stream = System.IO.File.OpenRead(physicalPath);
            return File(stream, contentType);
        }
    }
}
