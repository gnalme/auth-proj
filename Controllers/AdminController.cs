using ItransitionAuthentication.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItransitionAuthentication.Controllers;

[Authorize]
[Route("admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("users")]
    public IActionResult Users()
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == User.Identity!.Name);
        if (user == null || user.IsBlocked || user.IsDeleted)
        {
            HttpContext.SignOutAsync();
            return Redirect("/account/login");
        }

        var users = _context.Users
            .Where(u => !u.IsDeleted)
            .OrderByDescending(u => u.LastLogin ?? u.RegisteredAt)
            .ToList();
        return View(users);
    }

    [HttpPost("block")]
    public async Task<IActionResult> Block([FromForm] int[] ids)
    {
        var users = _context.Users.Where(u => ids.Contains(u.Id)).ToList();
        users.ForEach(u => u.IsBlocked = true);
        await _context.SaveChangesAsync();
        return RedirectToAction("Users");
    }

    [HttpPost("unblock")]
    public async Task<IActionResult> Unblock([FromForm] int[] ids)
    {
        var users = _context.Users.Where(u => ids.Contains(u.Id)).ToList();
        users.ForEach(u => u.IsBlocked = false);
        await _context.SaveChangesAsync();
        return RedirectToAction("Users");
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromForm] int[] ids)
    {
        var users = _context.Users.Where(u => ids.Contains(u.Id)).ToList();
        users.ForEach(u => u.IsDeleted = true);
        _context.Users.RemoveRange(users);
        await _context.SaveChangesAsync();
        return RedirectToAction("Users");
    }
}