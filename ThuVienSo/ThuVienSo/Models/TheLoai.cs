using System.Collections.Generic;

namespace ThuVienSo.Models
{
    /// <summary>
    /// Thực thể thể loại - ánh xạ bảng dbo.the_loai.
    /// </summary>
    public class TheLoai
    {
        public int Id { get; set; }

        public string TenTheLoai { get; set; }

        public string MoTa { get; set; }

        /// <summary>Số tài liệu đang gắn thể loại này (dùng cho trang quản lý danh mục).</summary>
        public int SoTaiLieu { get; set; }

        // ---- Quan hệ (navigation) ----
        public ICollection<TaiLieu> DanhSachTaiLieu { get; set; }
    }
}
