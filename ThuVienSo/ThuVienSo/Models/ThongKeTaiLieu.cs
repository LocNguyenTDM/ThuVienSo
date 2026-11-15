namespace ThuVienSo.Models
{
    /// <summary>
    /// Dòng thống kê lưu lượng của một tài liệu (dùng cho báo cáo của thủ thư).
    /// Không ánh xạ bảng nào mà là kết quả tổng hợp từ dbo.lich_su_tai_lieu.
    /// </summary>
    public class ThongKeTaiLieu
    {
        public int TaiLieuId { get; set; }

        public string TenTaiLieu { get; set; }

        public int LuotXem { get; set; }

        public int LuotTai { get; set; }

        /// <summary>Số giao dịch thành công của tài liệu trong kỳ thống kê.</summary>
        public int SoGiaoDich { get; set; }

        /// <summary>Doanh thu chỉ tính từ các giao dịch THANH_CONG.</summary>
        public long DoanhThu { get; set; }

        /// <summary>Tổng lượt xem + tải.</summary>
        public int TongLuot
        {
            get { return LuotXem + LuotTai; }
        }
    }
}
