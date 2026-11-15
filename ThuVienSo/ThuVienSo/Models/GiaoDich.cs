using System;
using System.Collections.Generic;

namespace ThuVienSo.Models
{
    /// <summary>
    /// Thực thể giao dịch mua quyền tải tài liệu - ánh xạ bảng dbo.giao_dich.
    /// Trạng thái: DANG_XU_LY | THANH_CONG | THAT_BAI.
    /// </summary>
    public class GiaoDich
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public int? TaiLieuId { get; set; }

        public int? SoTien { get; set; }

        public DateTime? NgayGiaoDich { get; set; }

        /// <summary>DANG_XU_LY | THANH_CONG | THAT_BAI</summary>
        public string TrangThai { get; set; }

        // ---- Quan hệ (navigation) ----
        public NguoiDung NguoiDung { get; set; }

        public TaiLieu TaiLieu { get; set; }

        public ICollection<QuyenTaiLieu> DanhSachQuyenTai { get; set; }

        // ---- Thuộc tính tính toán ----
        public bool DaThanhCong
        {
            get { return TrangThai == HangSoHeThong.TrangThaiGiaoDich.ThanhCong; }
        }

        public string NhanTrangThai
        {
            get
            {
                if (DaThanhCong) return "Thành công";
                if (TrangThai == HangSoHeThong.TrangThaiGiaoDich.DangXuLy) return "Đang xử lý";
                return "Thất bại";
            }
        }
    }
}
