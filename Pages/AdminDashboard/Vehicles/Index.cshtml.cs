using FleetManager.Data;
using FleetManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Pages.AdminDashboard.Vehicles;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Vehicle> Vehicles { get; set; } = [];

    public async Task OnGetAsync()
    {
        Vehicles = await _context.Vehicles
            .OrderBy(v => v.RegistrationNumber)
            .ToListAsync();
    }
}