using System.ComponentModel.DataAnnotations;

namespace ThuVienSo.Models.ViewModels
{
    /// <summary>Dữ liệu biểu mẫu cập nhật thông tin cá nhân của người đọc.</summary>
    public class CapNhatHoSoViewModel
    {
        /// <summary>Email chỉ hiển thị, không cho thay đổi (là tài khoản đăng nhập).</summary>
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^[0-9]{9,15}$", ErrorMessage = "Số điện thoại chỉ gồm 9 đến 15 chữ số.")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }
    }
}
