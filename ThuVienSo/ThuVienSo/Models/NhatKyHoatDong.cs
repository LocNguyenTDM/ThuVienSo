using System;

namespace ThuVienSo.Models
{
    /// <summary>
    /// Thực thể nhật ký hoạt động hệ thống - ánh xạ bảng dbo.nhat_ky_hoat_dong.
    /// </summary>
    public class NhatKyHoatDong
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public string HanhDong { get; set; }

        public DateTime? ThoiGian { get; set; }

        // ---- Quan hệ (navigation) ----
        public NguoiDung NguoiDung { get; set; }
    }
}
