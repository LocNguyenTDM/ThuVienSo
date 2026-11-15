using System;

namespace ThuVienSo.Models
{
    /// <summary>
    /// Thực thể lịch sử xem/tải tài liệu - ánh xạ bảng dbo.lich_su_tai_lieu.
    /// Hành động: XEM | TAI.
    /// </summary>
    public class LichSuTaiLieu
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int TaiLieuId { get; set; }

        /// <summary>XEM | TAI</summary>
        public string HanhDong { get; set; }

        public DateTime ThoiGian { get; set; }

        // ---- Quan hệ (navigation) ----
        public NguoiDung NguoiDung { get; set; }

        public TaiLieu TaiLieu { get; set; }
    }
}
