using System;

namespace ThuVienSo.Models
{
    /// <summary>
    /// Thực thể quyền tải tài liệu được cấp cho người đọc - ánh xạ bảng dbo.quyen_tai_lieu.
    /// Bản ghi được tạo tự động bởi trigger sau khi giao dịch thành công.
    /// </summary>
    public class QuyenTaiLieu
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int TaiLieuId { get; set; }

        public int? GiaoDichId { get; set; }

        public DateTime NgayCap { get; set; }

        /// <summary>HOAT_DONG | DA_THU_HOI</summary>
        public string TrangThai { get; set; }

        // ---- Quan hệ (navigation) ----
        public NguoiDung NguoiDung { get; set; }

        public TaiLieu TaiLieu { get; set; }

        public GiaoDich GiaoDich { get; set; }

        // ---- Thuộc tính tính toán ----
        public bool ConHieuLuc
        {
            get { return TrangThai == HangSoHeThong.TrangThaiQuyenTai.HoatDong; }
        }
    }
}
