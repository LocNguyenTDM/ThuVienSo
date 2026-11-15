using System.ComponentModel.DataAnnotations;

namespace ThuVienSo.Models.ViewModels
{
    /// <summary>Dữ liệu biểu mẫu cập nhật tài khoản và phân quyền của quản trị viên.</summary>
    public class CapNhatNguoiDungViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^[0-9]{9,15}$", ErrorMessage = "Số điện thoại chỉ gồm 9 đến 15 chữ số.")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        [Display(Name = "Vai trò")]
        public string VaiTro { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; }
    }
}
