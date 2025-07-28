using System.Security.Claims;
using ItransitionAuthentication.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace ItransitionAuthentication.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("login")]
    public IActionResult Login() => View();

    [HttpPost("login")]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
        if (user == null || user.Password != password)
        {
            TempData["Error"] = "Incorrect email or password.";
            return RedirectToAction("Login");
        }
        if (user.IsBlocked || user.IsDeleted)
        {
            await HttpContext.SignOutAsync();
            TempData["error"] = "User is blocked or deleted!";
            return RedirectToAction("Login");
        }
        

        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new (ClaimTypes.Name, user.Email),
            new ("FullName", user.Name)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Redirect("/admin/users");
    }

    [HttpGet("register")]
    public IActionResult Register() => View();

    [HttpPost("register")]
    public async Task<IActionResult> Register(string name, string email, string password)
    {
        if (_context.Users.Any(u => u.Email == email))
        {
            TempData["error"] = "Email address already exists";
            return RedirectToAction("Register");
        }
        
        var exists = _context.Users.FirstOrDefault(u => u.Email == email);

        if (exists != null)
        {
            if (!exists.IsDeleted)
            {
                ViewBag.Error = "User with this email already exists";
                return View();
            }
            
            exists.Name = name;
            exists.Password = password;
            exists.IsDeleted = false;
            await _context.SaveChangesAsync();
            
            return RedirectToAction("Login");
        }

        _context.Users.Add(new User { Name = name, Email = email, Password = password });
        await _context.SaveChangesAsync();

        return Redirect("/account/login");
    }
    
    [HttpGet("forgotpassword")]
    public IActionResult ForgotPassword() => View();

    [HttpPost("forgotpassword")]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email && !u.IsDeleted);
        if (user == null)
        {
            TempData["Error"] = "Email address not found!";
            return RedirectToAction("Login");
        }
        
        user.PasswordResetToken = Guid.NewGuid();
        user.TokenExpiration = DateTime.UtcNow.AddMinutes(30);
        await _context.SaveChangesAsync();
        
        var resetLink = Url.Action("ResetPassword", "Account", new { token = user.PasswordResetToken }, Request.Scheme);
        
        TempData["Success"] = resetLink;
        return View();
    }

    [HttpGet("resetpassword")]
    public IActionResult ResetPassword(Guid token)
    {
        var user = _context.Users.FirstOrDefault(u => u.PasswordResetToken == token &&
                                                      u.TokenExpiration > DateTime.UtcNow);

        if (user == null)
        {
            TempData["Error"] = "The link is invalid or outdated";
            return RedirectToAction("ForgotPassword");
        }

        ViewBag.Token = token;
        return View();
    }
    
    [HttpPost("resetpassword")]
    public IActionResult ResetPassword(Guid token, string newPassword)
    {
        var user = _context.Users.FirstOrDefault(u =>
            u.PasswordResetToken == token &&
            u.TokenExpiration > DateTime.UtcNow);

        if (user == null)
        {
            TempData["Error"] = "The link is invalid or outdated";
            return RedirectToAction("ForgotPassword");
        }

        user.Password = newPassword; 
        user.PasswordResetToken = null;
        user.TokenExpiration = null;

        _context.SaveChanges();

        TempData["Success"] = "Password successfully changed!";
        return RedirectToAction("Login");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Redirect("/account/login");
    }
}