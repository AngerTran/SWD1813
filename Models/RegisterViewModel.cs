using System.ComponentModel.DataAnnotations;

namespace SWD1813.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6–100 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(255)]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng chọn vai trò (role)")]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = null!;
}
