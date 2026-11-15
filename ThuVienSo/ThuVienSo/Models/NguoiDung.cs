using System.Collections.Generic;

namespace ThuVienSo.Models
{
    /// <summary>
    /// Thực thể người dùng - ánh xạ bảng dbo.nguoi_dung.
    /// Ba vai trò: NGUOI_DOC, THU_THU, ADMIN.
    /// </summary>
    public class NguoiDung
    {
        public int Id { get; set; }

        public string HoTen { get; set; }

        public string Email { get; set; }

        /// <summary>Mật khẩu đã băm (PBKDF2). Không bao giờ lưu văn bản thuần.</summary>
        public string MatKhau { get; set; }

        public string SoDienThoai { get; set; }

        /// <summary>NGUOI_DOC | THU_THU | ADMIN</summary>
        public string VaiTro { get; set; }

        /// <summary>HOAT_DONG | BI_KHOA</summary>
        public string TrangThai { get; set; }

        // ---- Quan hệ (navigation) ----
        public ICollection<TaiLieu> DanhSachTaiLieuDaDang { get; set; }

        public ICollection<GiaoDich> DanhSachGiaoDich { get; set; }

        public ICollection<QuyenTaiLieu> DanhSachQuyenTai { get; set; }

        public ICollection<LichSuTaiLieu> DanhSachLichSuTaiLieu { get; set; }

        public ICollection<NhatKyHoatDong> DanhSachNhatKy { get; set; }

        // ---- Thuộc tính tính toán ----
        public bool DangHoatDong
        {
            get { return TrangThai == HangSoHeThong.TrangThaiNguoiDung.HoatDong; }
        }

        public bool LaNguoiDoc
        {
            get { return VaiTro == HangSoHeThong.VaiTro.NguoiDoc; }
        }

        public bool LaThuThu
        {
            get { return VaiTro == HangSoHeThong.VaiTro.ThuThu; }
        }

        public bool LaAdmin
        {
            get { return VaiTro == HangSoHeThong.VaiTro.Admin; }
        }

        public string NhanVaiTro
        {
            get
            {
                if (LaAdmin) return "Quản trị viên";
                if (LaThuThu) return "Thủ thư";
                return "Người đọc";
            }
        }

        public string NhanTrangThai
        {
            get { return DangHoatDong ? "Đang hoạt động" : "Đã bị khóa"; }
        }

        public string TenHienThi
        {
            get { return string.IsNullOrWhiteSpace(HoTen) ? Email : HoTen; }
        }
    }
}
