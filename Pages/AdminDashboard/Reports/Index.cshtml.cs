using FleetManager.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using ClosedXML.Excel;

namespace FleetManager.Pages.AdminDashboard.Reports
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public IndexModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public DateTime? FromDate { get; set; }

        [BindProperty]
        public DateTime? ToDate { get; set; }

        [BindProperty]
        public string ExportType { get; set; } = "WithPhotos";

        public List<OdometerReadingRow> Readings { get; set; } = new();

        public class OdometerReadingRow
        {
            public int Id { get; set; }
            public DateTime CapturedAt { get; set; }
            public string VehicleRegistration { get; set; } = "";
            public string UserEmail { get; set; } = "";
            public string UserFullName { get; set; } = "";
            public int KilometerReading { get; set; }
            public string ImagePath { get; set; } = "";
        }

        public async Task OnGetAsync()
        {
            // Default to last 30 days only on initial load (no filter params in URL)
            if (FromDate == null && ToDate == null)
            {
                ToDate = DateTime.UtcNow.Date.AddDays(1);
                FromDate = DateTime.UtcNow.Date.AddDays(-30);
            }

            await LoadReadingsAsync();
        }

        public async Task OnPostAsync()
        {
            // Filter button clicked - FromDate and ToDate are bound from form
            await LoadReadingsAsync();
        }

        public async Task OnPostClearAsync()
        {
            // Clear filter button clicked
            FromDate = null;
            ToDate = null;
            
            // Set defaults
            ToDate = DateTime.UtcNow.Date.AddDays(1);
            FromDate = DateTime.UtcNow.Date.AddDays(-30);

            await LoadReadingsAsync();
        }

        private async Task LoadReadingsAsync()
        {
            var fromDate = NormalizeDateToUtc(FromDate ?? DateTime.MinValue);
            var toDate = NormalizeDateToUtc(ToDate ?? DateTime.MaxValue);

            // If ToDate is set, add one day and subtract ticks to get end of day
            if (ToDate.HasValue)
            {
                toDate = toDate.AddDays(1).AddTicks(-1);
            }

            var readings = await _context.OdometerReadings
                .Include(r => r.Vehicle)
                .Include(r => r.User)
                .Where(r => r.CapturedAt >= fromDate && r.CapturedAt <= toDate)
                .OrderByDescending(r => r.CapturedAt)
                .ToListAsync();

            Readings = readings.Select(r => new OdometerReadingRow
            {
                Id = r.Id,
                CapturedAt = r.CapturedAt,
                VehicleRegistration = r.Vehicle?.RegistrationNumber ?? "(Unassigned)",
                UserEmail = r.User?.Email ?? r.UserId,
                UserFullName = r.User?.FullName ?? "(Unknown)",
                KilometerReading = r.KilometerReading,
                ImagePath = r.ImagePath
            }).ToList();
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            var fromDate = NormalizeDateToUtc(FromDate ?? DateTime.MinValue);
            var toDate = NormalizeDateToUtc(ToDate ?? DateTime.MaxValue);

            // If ToDate is set, add one day and subtract ticks to get end of day
            if (ToDate.HasValue)
            {
                toDate = toDate.AddDays(1).AddTicks(-1);
            }

            var readings = await _context.OdometerReadings
                .Include(r => r.Vehicle)
                .Include(r => r.User)
                .Where(r => r.CapturedAt >= fromDate && r.CapturedAt <= toDate)
                .OrderByDescending(r => r.CapturedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Odometer Readings");

            // Set up headers
            worksheet.Cell(1, 1).Value = "Captured Date";
            worksheet.Cell(1, 2).Value = "Vehicle Registration";
            worksheet.Cell(1, 3).Value = "User Email";
            worksheet.Cell(1, 4).Value = "User Full Name";
            worksheet.Cell(1, 5).Value = "Odometer Reading (km)";

            if (ExportType == "WithPhotos")
            {
                worksheet.Cell(1, 6).Value = "Photo";
            }

            // Style header row
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;

            foreach (var reading in readings)
            {
                worksheet.Cell(row, 1).Value = reading.CapturedAt.ToString("g", CultureInfo.CurrentCulture);
                worksheet.Cell(row, 2).Value = reading.Vehicle?.RegistrationNumber ?? "(Unassigned)";
                worksheet.Cell(row, 3).Value = reading.User?.Email ?? reading.UserId;
                worksheet.Cell(row, 4).Value = reading.User?.FullName ?? "(Unknown)";
                worksheet.Cell(row, 5).Value = reading.KilometerReading;

                // Embed image only if "WithPhotos" export is selected
                if (ExportType == "WithPhotos" && !string.IsNullOrEmpty(reading.ImagePath))
                {
                    string physicalPath = GetPhysicalImagePath(reading.ImagePath);

                    if (System.IO.File.Exists(physicalPath))
                    {
                        try
                        {
                            var picture = worksheet.AddPicture(physicalPath);
                            picture.MoveTo(worksheet.Cell(row, 6));
                            // Significantly reduce photo size - 10% of original
                            picture.Scale(0.1);
                            // Set minimal row height for compact display
                            worksheet.Row(row).Height = 30;
                        }
                        catch
                        {
                            worksheet.Cell(row, 6).Value = "(Could not embed)";
                        }
                    }
                    else
                    {
                        worksheet.Cell(row, 6).Value = "(Image not found)";
                    }
                }

                row++;
            }

            // Adjust column widths
            worksheet.Columns().AdjustToContents();
            worksheet.Column(1).Width = 18;
            worksheet.Column(2).Width = 20;
            worksheet.Column(3).Width = 25;
            worksheet.Column(4).Width = 20;
            worksheet.Column(5).Width = 18;
            
            if (ExportType == "WithPhotos")
            {
                worksheet.Column(6).Width = 12;
            }

            // Generate file
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Position = 0;

            string fileName = ExportType == "WithPhotos" 
                ? $"odometer-report-with-photos-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx"
                : $"odometer-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private string GetPhysicalImagePath(string imagePath)
        {
            if (imagePath.StartsWith("/"))
            {
                return Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), imagePath.TrimStart('/'));
            }
            return imagePath;
        }

        /// <summary>
        /// Converts a DateTime value (likely Unspecified from form input) to UTC.
        /// This is required for PostgreSQL which only accepts timestamp with time zone in UTC.
        /// </summary>
        private DateTime NormalizeDateToUtc(DateTime dateTime)
        {
            // If already UTC, return as is
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;

            // If Unspecified or Local, treat as UTC
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dateTime.Date, DateTimeKind.Utc);
            }

            // If Local, convert to UTC
            return dateTime.ToUniversalTime();
        }
    }
}
