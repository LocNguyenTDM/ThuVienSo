using System.Collections.Generic;

namespace ThuVienSo.Models
{
    /// <summary>
    /// Thực thể tác giả - ánh xạ bảng dbo.tac_gia.
    /// </summary>
    public class TacGia
    {
        public int Id { get; set; }

        public string TenTacGia { get; set; }

        public string QuocTich { get; set; }

        /// <summary>Số tài liệu đang gắn tác giả này (dùng cho trang quản lý danh mục).</summary>
        public int SoTaiLieu { get; set; }

        // ---- Quan hệ (navigation) ----
        public ICollection<TaiLieu> DanhSachTaiLieu { get; set; }
    }
}
