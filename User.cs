using System.ComponentModel.DataAnnotations;

namespace ItransitionAuthentication;

public class User
{
    public int Id { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsBlocked { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    
    public Guid? PasswordResetToken { get; set; }
    
    public DateTime? TokenExpiration { get; set; }
}