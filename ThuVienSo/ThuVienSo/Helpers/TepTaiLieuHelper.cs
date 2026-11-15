using System;
using System.IO;
using System.Text;
using System.Web.Hosting;
using ThuVienSo.Models;

namespace ThuVienSo.Helpers
{
    /// <summary>
    /// Xử lý tệp tài liệu: đổi file_url trong CSDL thành đường dẫn vật lý,
    /// kiểm tra tệp có thực sự tồn tại và xác định loại nội dung khi trả về.
    /// </summary>
    public static class TepTaiLieuHelper
    {
        /// <summary>
        /// Tiền tố lưu trong cột file_url của CSDL, ví dụ: Content/Document/ooad.pdf.
        /// Đây là đường dẫn tương đối tính từ gốc dự án nên trùng luôn với thư mục thật,
        /// giúp dễ đối chiếu giữa CSDL và thư mục Content/Document.
        /// </summary>
        public const string TienToFileUrl = "Content/Document/";

        /// <summary>
        /// Thư mục vật lý chứa tệp tài liệu, đặt trong Content/Document để tệp nằm ngay
        /// trong thư mục dự án, thuận tiện khi quản lý và kiểm tra. Thư mục này bị chặn
        /// truy cập trực tiếp từ IIS bằng hiddenSegments trong Web.config, nên mọi lượt
        /// đọc/tải vẫn phải đi qua controller (NguoiDoc/NoiDung để đọc trực tuyến,
        /// NguoiDoc/Tai để tải) nơi có kiểm tra quyền. Nhờ vậy không thể tải tài liệu
        /// trả phí chỉ bằng cách biết đường dẫn.
        /// </summary>
        public const string ThuMucTep = "~/" + TienToFileUrl;

        /// <summary>
        /// Chuẩn hoá đường dẫn người dùng nhập thành giá trị file_url lưu trong CSDL.
        /// Chấp nhận tên tệp trần, đường dẫn có/không có tiền tố Content/Document,
        /// có hoặc không có dấu "/" hay "~/" ở đầu.
        /// </summary>
        public static string TaoFileUrl(string tenTepHoacDuongDan)
        {
            var duongDan = (tenTepHoacDuongDan ?? string.Empty).Replace('\\', '/').Trim().TrimStart('~', '/');

            if (duongDan.StartsWith(TienToFileUrl, StringComparison.OrdinalIgnoreCase))
                duongDan = duongDan.Substring(TienToFileUrl.Length);

            return TienToFileUrl + duongDan.TrimStart('/');
        }

        /// <summary>
        /// Lấy đường dẫn vật lý của tệp từ file_url trong CSDL.
        /// Trả về null nếu đường dẫn rỗng hoặc không hợp lệ.
        /// </summary>
        public static string LayDuongDanVatLy(string fileUrl, string dinhDang = null)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return null;

            var duongDan = fileUrl.Replace('\\', '/').Trim().TrimStart('~', '/');

            if (duongDan.Length == 0 || duongDan.Contains("..")) return null;

            // Dữ liệu cũ có thể chỉ lưu tên tệp; bổ sung tiền tố Content/Document cho đủ.
            if (!duongDan.StartsWith(TienToFileUrl, StringComparison.OrdinalIgnoreCase))
                duongDan = TienToFileUrl + duongDan;

            // Nếu yêu cầu định dạng cụ thể (pdf/docx), thay đổi phần mở rộng.
            if (!string.IsNullOrEmpty(dinhDang))
            {
                var baseName = Path.GetFileNameWithoutExtension(duongDan);
                duongDan = TienToFileUrl + baseName + "." + dinhDang.TrimStart('.').ToLowerInvariant();
            }

            try
            {
                return HostingEnvironment.MapPath("~/" + duongDan);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Kiểm tra tệp tài liệu có tồn tại trên máy chủ hay chưa.</summary>
        public static bool TonTai(string fileUrl, string dinhDang = null)
        {
            var duongDan = LayDuongDanVatLy(fileUrl, dinhDang);
            return !string.IsNullOrEmpty(duongDan) && File.Exists(duongDan);
        }

        /// <summary>Lấy phần mở rộng của tệp, ví dụ "pdf". Trả về rỗng nếu không xác định.</summary>
        public static string LayPhanMoRong(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return string.Empty;

            var duongDan = fileUrl.Split('?')[0];
            var phanMoRong = Path.GetExtension(duongDan);
            return string.IsNullOrEmpty(phanMoRong) ? string.Empty : phanMoRong.TrimStart('.').ToLowerInvariant();
        }

        /// <summary>Nhãn định dạng hiển thị trên giao diện, ví dụ "PDF".</summary>
        public static string LayNhanDinhDang(string fileUrl)
        {
            var phanMoRong = LayPhanMoRong(fileUrl);
            return string.IsNullOrEmpty(phanMoRong) ? "TÀI LIỆU" : phanMoRong.ToUpperInvariant();
        }

        /// <summary>
        /// Trình duyệt hiện đại chỉ hiển thị trực tiếp được một số định dạng
        /// (PDF, văn bản thuần). Các định dạng còn lại (DOC/DOCX/EPUB...) phải tải về
        /// rồi mở bằng ứng dụng chuyên dụng.
        /// </summary>
        public static bool DocTrucTuyenDuoc(string fileUrl)
        {
            var phanMoRong = LayPhanMoRong(fileUrl);
            return phanMoRong == "pdf" || phanMoRong == "txt";
        }

        /// <summary>
        /// Tạo tên tệp an toàn đặt theo tên tài liệu (thay cho tên ngẫu nhiên) để thủ thư
        /// dễ nhận biết và quản lý tệp trong thư mục Content/Document.
        /// Ký tự không hợp lệ trong tên tệp bị thay bằng gạch nối, khoảng trắng gộp lại.
        /// </summary>
        public static string TaoTenTepTheoTaiLieu(string tenTaiLieu, string phanMoRong)
        {
            var ten = (tenTaiLieu ?? string.Empty).Trim();

            var kyTuKhongHopLe = Path.GetInvalidFileNameChars();
            var boDem = new StringBuilder(ten.Length);
            foreach (var kyTu in ten)
            {
                boDem.Append(Array.IndexOf(kyTuKhongHopLe, kyTu) >= 0 ? '-' : kyTu);
            }

            // Gộp khoảng trắng / gạch nối / gạch dưới liên tiếp thành một gạch nối duy nhất.
            var ketQua = new StringBuilder(ten.Length);
            var dangGapKyTuPhanCach = false;
            foreach (var kyTu in boDem.ToString())
            {
                if (char.IsWhiteSpace(kyTu) || kyTu == '-' || kyTu == '_')
                {
                    dangGapKyTuPhanCach = true;
                    continue;
                }

                if (dangGapKyTuPhanCach && ketQua.Length > 0) ketQua.Append('-');
                dangGapKyTuPhanCach = false;
                ketQua.Append(kyTu);
            }

            ten = ketQua.ToString();

            // Giới hạn độ dài để tránh vượt giới hạn đường dẫn của Windows.
            if (ten.Length > 150) ten = ten.Substring(0, 150).TrimEnd('-', '.');
            if (ten.Length == 0) ten = "tai-lieu";

            var duoi = string.IsNullOrWhiteSpace(phanMoRong) ? ".pdf" : phanMoRong;
            if (!duoi.StartsWith(".")) duoi = "." + duoi;

            return ten + duoi.ToLowerInvariant();
        }

        /// <summary>Kiểu nội dung dùng khi trả tệp về trình duyệt.</summary>
        public static string LayKieuNoiDung(string fileUrl, string dinhDang = null)
        {
            var phanMoRong = !string.IsNullOrEmpty(dinhDang) ? dinhDang.TrimStart('.').ToLowerInvariant() : LayPhanMoRong(fileUrl);
            switch (phanMoRong)
            {
                case "pdf": return "application/pdf";
                case "epub": return "application/epub+zip";
                case "txt": return "text/plain";
                case "doc": return "application/msword";
                case "docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                default: return "application/octet-stream";
            }
        }

        /// <summary>Tạo tên tệp an toàn khi người dùng tải về.</summary>
        public static string LayTenTepTaiVe(TaiLieu taiLieu, string dinhDang = null)
        {
            var ten = taiLieu.TenTaiLieu ?? ("tai-lieu-" + taiLieu.Id);

            var kq = new StringBuilder();
            foreach (var kyTu in ten)
            {
                kq.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), kyTu) >= 0 ? '_' : kyTu);
            }

            var phanMoRong = !string.IsNullOrEmpty(dinhDang) ? dinhDang.TrimStart('.').ToLowerInvariant() : LayPhanMoRong(taiLieu.FileUrl);
            return phanMoRong.Length == 0 ? kq.ToString() : kq + "." + phanMoRong;
        }
    }
}
