using FleetManager.Data;
using FleetManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetVips;

namespace FleetManager.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _config = config;
        }

        [BindProperty]
        public OdometerFormInput Input { get; set; } = new();

        // Readonly vehicle info for the logged-in user
        public string VehicleRegistration { get; set; } = "-- Unassigned --";

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return;

            if (user.VehicleId.HasValue)
            {
                var vehicle = await _context.Vehicles.FindAsync(user.VehicleId.Value);
                if (vehicle != null)
                    VehicleRegistration = vehicle.RegistrationNumber;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Unable to determine current user.");
                await OnGetAsync();
                return Page();
            }

            // Determine upload root from config (UploadPath) or fallback to web root wwwroot/uploads
            var uploadPathConfig = _config["UploadPath"];

            // If UploadPath is set and is an absolute path, use it. Otherwise fall back to wwwroot/uploads
            string uploadsPhysicalRoot;
            bool usingCustomPath = false;

            if (!string.IsNullOrWhiteSpace(uploadPathConfig) && Path.IsPathRooted(uploadPathConfig))
            {
                uploadsPhysicalRoot = uploadPathConfig;
                usingCustomPath = true;
            }
            else
            {
                uploadsPhysicalRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");
            }

            Directory.CreateDirectory(uploadsPhysicalRoot);

            string? savedPath = null;

            // Build a safe identifier for filename: prefer vehicle reg, fallback to username
            string identifier = user.VehicleId.HasValue
                ? (await _context.Vehicles.FindAsync(user.VehicleId.Value))?.RegistrationNumber ?? user.UserName
                : user.UserName ?? "user";

            // Remove invalid filename chars
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                identifier = identifier.Replace(c.ToString(), string.Empty);
            }

            if (Input.PhotoFile != null && Input.PhotoFile.Length > 0)
            {
                // Read file into memory first (NetVips uses buffers)
                await using var ms = new MemoryStream();
                await Input.PhotoFile.CopyToAsync(ms);
                var bytes = ms.ToArray();

                // Default to original extension
                var origExt = Path.GetExtension(Input.PhotoFile.FileName);
                if (string.IsNullOrWhiteSpace(origExt)) origExt = ".jpg";

                // Try to process with NetVips: resize and convert to WebP for smaller size
                try
                {
                    // Create image from buffer. The second parameter is an option string; empty is fine.
                    var img = Image.NewFromBuffer(bytes, "");

                    const int maxWidth = 1200;
                    Image outImg = img;

                    if (img.Width > maxWidth)
                    {
                        double scale = (double)maxWidth / img.Width;
                        outImg = img.Resize(scale);
                    }

                    // Encode to WebP with default options (quality option removed to avoid API mismatch)
                    var webpBytes = outImg.WebpsaveBuffer();

                    var fileName = $"{identifier}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}.webp";
                    var fullPath = Path.Combine(uploadsPhysicalRoot, fileName);

                    await System.IO.File.WriteAllBytesAsync(fullPath, webpBytes);

                    if (!usingCustomPath)
                    {
                        savedPath = "/uploads/" + fileName;
                    }
                    else
                    {
                        savedPath = fullPath;
                    }

                    // Clean up vips images
                    img.Dispose();
                    if (!ReferenceEquals(outImg, img)) outImg.Dispose();
                }
                catch (DllNotFoundException)
                {
                    // NetVips native lib not present. Fall back to saving original file
                    var fileName = $"{identifier}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}{origExt}";
                    var fullPath = Path.Combine(uploadsPhysicalRoot, fileName);

                    await System.IO.File.WriteAllBytesAsync(fullPath, bytes);

                    if (!usingCustomPath)
                    {
                        savedPath = "/uploads/" + fileName;
                    }
                    else
                    {
                        savedPath = fullPath;
                    }
                }
                catch (TypeInitializationException)
                {
                    // NetVips failed to initialize; fallback
                    var fileName = $"{identifier}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}{origExt}";
                    var fullPath = Path.Combine(uploadsPhysicalRoot, fileName);

                    await System.IO.File.WriteAllBytesAsync(fullPath, bytes);

                    if (!usingCustomPath)
                    {
                        savedPath = "/uploads/" + fileName;
                    }
                    else
                    {
                        savedPath = fullPath;
                    }
                }
                catch (Exception)
                {
                    // Any other processing error -> fallback to saving original
                    var fileName = $"{identifier}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}{origExt}";
                    var fullPath = Path.Combine(uploadsPhysicalRoot, fileName);

                    await System.IO.File.WriteAllBytesAsync(fullPath, bytes);

                    if (!usingCustomPath)
                    {
                        savedPath = "/uploads/" + fileName;
                    }
                    else
                    {
                        savedPath = fullPath;
                    }
                }
            }

            var reading = new OdometerReading
            {
                VehicleId = user.VehicleId ?? 0,
                UserId = user.Id,
                KilometerReading = Input.OdometerReading,
                ImagePath = savedPath ?? string.Empty,
                CapturedAt = DateTime.UtcNow
            };

            _context.OdometerReadings.Add(reading);
            await _context.SaveChangesAsync();

            // Redirect to Success page with id
            return RedirectToPage("Success", new { id = reading.Id });
        }
    }

    public class OdometerFormInput
    {
        [Required(ErrorMessage = "Odometer reading is required.")]
        [Range(0, 9999999, ErrorMessage = "Please enter a valid reading.")]
        [Display(Name = "Current Odometer Reading (kms)")]
        public int OdometerReading { get; set; }

        [Required(ErrorMessage = "Please take or upload a photo.")]
        [Display(Name = "Odometer Photo")]
        public IFormFile? PhotoFile { get; set; }
    }
}
