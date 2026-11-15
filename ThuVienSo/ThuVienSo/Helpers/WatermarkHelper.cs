using System;
using System.IO;
using System.Text;

namespace ThuVienSo.Helpers
{
    /// <summary>
    /// Sinh watermark đánh dấu bản quyền cho tài liệu số.
    ///
    /// - Khi ĐỌC TRỰC TUYẾN: giao diện phủ một lớp watermark chéo (tên người đọc,
    ///   mã tài liệu, thời điểm) lên trên khung đọc (xem Views/NguoiDoc/TaiLieuSo.cshtml).
    /// - Khi TẢI VỀ: chèn khối watermark vào đầu nội dung tệp trước khi trả về.
    ///
    /// GIỚI HẠN: chỉ chèn được vào tệp văn bản thuần (không có byte 0, giải mã UTF-8 hợp lệ).
    /// Các tệp nhị phân thật (PDF, DOCX/EPUB dạng ZIP) không bị sửa nội dung vì cần thư viện
    /// chuyên dụng để ghi watermark mà không làm hỏng cấu trúc tệp.
    /// </summary>
    public static class WatermarkHelper
    {
        /// <summary>
        /// Tạo khối watermark dạng văn bản để chèn lên đầu tệp khi người đọc tải về.
        /// </summary>
        public static string TaoKhoiWatermark(string hoTen, int? userId, string email, DateTime thoiDiem)
        {
            var nguoiTai = string.IsNullOrWhiteSpace(hoTen) ? "(không xác định)" : hoTen.Trim();
            if (userId.HasValue) nguoiTai += " (#" + userId.Value + ")";

            var khoi = new StringBuilder();
            khoi.AppendLine("================================================================");
            khoi.AppendLine("  BẢN QUYỀN THUỘC THƯ VIỆN SỐ - TÀI LIỆU ĐÃ ĐƯỢC ĐÁNH DẤU (WATERMARK)");
            khoi.AppendLine("  Người tải : " + nguoiTai);
            if (!string.IsNullOrWhiteSpace(email))
                khoi.AppendLine("  Email     : " + email.Trim());
            khoi.AppendLine("  Thời điểm : " + thoiDiem.ToString("dd/MM/yyyy HH:mm:ss"));
            khoi.AppendLine("  Nghiêm cấm sao chép, phát tán trái phép. Vui lòng báo cáo hành vi vi phạm.");
            khoi.AppendLine("================================================================");
            khoi.AppendLine();
            return khoi.ToString();
        }

        /// <summary>
        /// Trả về nội dung tệp đã chèn khối watermark lên đầu.
        /// Trả về null nếu tệp là định dạng nhị phân không thể chèn (khi đó nên trả tệp gốc).
        /// </summary>
        public static byte[] ChenWatermark(string duongDan, string khoiWatermark)
        {
            byte[] goc;
            try
            {
                goc = File.ReadAllBytes(duongDan);
            }
            catch (Exception)
            {
                return null;
            }

            if (!LaVanBanThuan(goc)) return null;

            // Bỏ BOM (nếu có) rồi ghi lại bằng UTF-8 không BOM để watermark luôn ở đầu tệp.
            if (goc.Length >= 3 && goc[0] == 0xEF && goc[1] == 0xBB && goc[2] == 0xBF)
                goc = LayPhanSauBom(goc);

            var utf8KhongBom = new UTF8Encoding(false);
            string noiDung;
            try
            {
                noiDung = utf8KhongBom.GetString(goc);
            }
            catch (Exception)
            {
                return null;
            }

            return utf8KhongBom.GetBytes(khoiWatermark + noiDung);
        }

        /// <summary>Xác định nội dung tệp có phải văn bản thuần để có thể chèn watermark.</summary>
        private static bool LaVanBanThuan(byte[] bytes)
        {
            if (bytes.Length == 0) return false;

            // PDF thật (%PDF) và gói ZIP/OPC (DOCX, EPUB - "PK") không sửa trực tiếp được.
            if (bytes.Length >= 2 && bytes[0] == 0x25 && bytes[1] == 0x50) return false;
            if (bytes.Length >= 2 && bytes[0] == 0x50 && bytes[1] == 0x4B) return false;

            // Byte 0 là dấu hiệu chắc chắn của tệp nhị phân.
            var gioiHan = Math.Min(bytes.Length, 8192);
            for (var i = 0; i < gioiHan; i++)
            {
                if (bytes[i] == 0) return false;
            }

            // Phải giải mã được UTF-8 nghiêm ngặt, nếu không coi là nhị phân.
            try
            {
                new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private static byte[] LayPhanSauBom(byte[] bytes)
        {
            var ketQua = new byte[bytes.Length - 3];
            Array.Copy(bytes, 3, ketQua, 0, ketQua.Length);
            return ketQua;
        }
    }
}
