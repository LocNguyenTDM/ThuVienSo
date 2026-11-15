using System.Collections.Generic;
using System.Globalization;

namespace ThuVienSo.Models
{
    /// <summary>
    /// Thực thể tài liệu số - ánh xạ bảng dbo.tai_lieu.
    /// Gồm quyền đọc trực tuyến, quyền tải và giá bán.
    /// </summary>
    public class TaiLieu
    {
        public int Id { get; set; }

        public string TenTaiLieu { get; set; }

        public string MoTa { get; set; }

        /// <summary>Đường dẫn tệp tài liệu, tính từ gốc dự án (ví dụ Content/Document/ooad.pdf).</summary>
        public string FileUrl { get; set; }

        /// <summary>Giá bán (VNĐ). Bằng 0 là tài liệu miễn phí.</summary>
        public int GiaBan { get; set; }

        public int? TheLoaiId { get; set; }

        public int? TacGiaId { get; set; }

        public int? NguoiDangId { get; set; }

        /// <summary>Cho phép đọc trực tuyến.</summary>
        public bool QuyenDocOnline { get; set; }

        /// <summary>Cho phép tải xuống (miễn phí hoặc sau khi mua).</summary>
        public bool QuyenTai { get; set; }

        /// <summary>HOAT_DONG | NGUNG_HOAT_DONG | CHO_DUYET</summary>
        public string TrangThai { get; set; }

        /// <summary>Lý do thủ thư từ chối tài liệu đóng góp (nếu có).</summary>
        public string LyDoTuChoi { get; set; }

        // ---- Quan hệ (navigation) ----
        public TheLoai TheLoai { get; set; }

        public TacGia TacGia { get; set; }

        public NguoiDung NguoiDang { get; set; }

        public ICollection<GiaoDich> DanhSachGiaoDich { get; set; }

        public ICollection<QuyenTaiLieu> DanhSachQuyenTai { get; set; }

        public ICollection<LichSuTaiLieu> DanhSachLichSuTaiLieu { get; set; }

        // ---- Thuộc tính tính toán ----
        public bool MienPhi
        {
            get { return GiaBan <= 0; }
        }

        public bool DangHoatDong
        {
            get { return TrangThai == HangSoHeThong.TrangThaiTaiLieu.HoatDong; }
        }

        public string GiaHienThi
        {
            get
            {
                return MienPhi
                    ? "Miễn phí"
                    : GiaBan.ToString("#,##0", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
            }
        }
    }
}
