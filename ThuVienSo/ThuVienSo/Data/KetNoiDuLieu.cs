using System;
using System.Configuration;
using System.Data.SqlClient;

namespace ThuVienSo.Data
{
    /// <summary>
    /// Lớp kết nối cơ sở dữ liệu SQL Server bằng ADO.NET.
    /// Chuỗi kết nối đọc từ Web.config (connectionStrings/QuanLyThuVien).
    /// </summary>
    public static class KetNoiDuLieu
    {
        private const string TenChuoiKetNoi = "QuanLyThuVien";

        /// <summary>Lấy chuỗi kết nối đang cấu hình trong Web.config.</summary>
        public static string ChuoiKetNoi
        {
            get
            {
                var cauHinh = ConfigurationManager.ConnectionStrings[TenChuoiKetNoi];
                if (cauHinh == null || string.IsNullOrWhiteSpace(cauHinh.ConnectionString))
                {
                    throw new InvalidOperationException(
                        "Chưa cấu hình chuỗi kết nối '" + TenChuoiKetNoi + "' trong Web.config.");
                }
                return cauHinh.ConnectionString;
            }
        }

        /// <summary>Mở một kết nối mới tới database QuanLyThuVien.</summary>
        public static SqlConnection TaoKetNoi()
        {
            var ketNoi = new SqlConnection(ChuoiKetNoi);
            ketNoi.Open();
            return ketNoi;
        }

        /// <summary>
        /// Tạo SqlCommand dùng truy vấn/tham số hoá, gắn sẵn kết nối và transaction.
        /// </summary>
        public static SqlCommand TaoLenh(string cauLenhSql, SqlConnection ketNoi, SqlTransaction giaoDich = null)
        {
            var lenh = new SqlCommand(cauLenhSql, ketNoi);
            if (giaoDich != null) lenh.Transaction = giaoDich;
            return lenh;
        }

        /// <summary>Thêm tham số an toàn; giá trị null được chuyển thành DBNull.</summary>
        public static void ThemThamSo(SqlCommand lenh, string ten, object giaTri)
        {
            lenh.Parameters.AddWithValue(ten, giaTri ?? DBNull.Value);
        }
    }
}
