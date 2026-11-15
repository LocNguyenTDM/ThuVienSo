using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ThuVienSo.Models;

namespace ThuVienSo.Data
{
    /// <summary>
    /// Truy cập dữ liệu giao dịch mua quyền tải (dbo.giao_dich).
    /// Việc tạo giao dịch và xác nhận thành công đều được thực hiện
    /// qua stored procedure của CSDL để giữ đúng quy trình nghiệp vụ.
    /// </summary>
    public class GiaoDichRepository
    {
        /// <summary>
        /// Tạo giao dịch ở trạng thái DANG_XU_LY bằng sp_TaoGiaoDich.
        /// Bảng giao_dich không có IDENTITY nên id sinh bằng MAX(id) + 1
        /// trong transaction có khoá bảng để tránh trùng mã giao dịch.
        /// Trả về id giao dịch vừa tạo.
        /// </summary>
        public int TaoGiaoDich(int userId, int taiLieuId)
        {
            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    int idMoi;
                    using (var lenhId = KetNoiDuLieu.TaoLenh(
                        "SELECT ISNULL(MAX(id), 0) + 1 FROM dbo.giao_dich WITH (TABLOCKX, HOLDLOCK)",
                        ketNoi, giaoDich))
                    {
                        idMoi = Convert.ToInt32(lenhId.ExecuteScalar());
                    }

                    using (var lenh = KetNoiDuLieu.TaoLenh("dbo.sp_TaoGiaoDich", ketNoi, giaoDich))
                    {
                        lenh.CommandType = CommandType.StoredProcedure;
                        KetNoiDuLieu.ThemThamSo(lenh, "@ID", idMoi);
                        KetNoiDuLieu.ThemThamSo(lenh, "@UserID", userId);
                        KetNoiDuLieu.ThemThamSo(lenh, "@TaiLieuID", taiLieuId);

                        lenh.ExecuteNonQuery();
                    }

                    giaoDich.Commit();
                    return idMoi;
                }
                catch
                {
                    try { giaoDich.Rollback(); }
                    catch (InvalidOperationException) { /* transaction đã tự rollback do XACT_ABORT */ }
                    throw;
                }
            }
        }

        /// <summary>
        /// Xác nhận giao dịch thành công bằng sp_XacNhanGiaoDichThanhCong.
        /// Chỉ được gọi từ phía máy chủ sau khi đã xác minh thanh toán.
        /// Trigger trg_GiaoDich_CapQuyenTai sẽ tự động cấp quyền tải.
        /// </summary>
        public void XacNhanThanhCong(int giaoDichId)
        {
            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh("dbo.sp_XacNhanGiaoDichThanhCong", ketNoi))
            {
                lenh.CommandType = CommandType.StoredProcedure;
                KetNoiDuLieu.ThemThamSo(lenh, "@GiaoDichID", giaoDichId);

                lenh.ExecuteNonQuery();
            }
        }

        /// <summary>Lấy một giao dịch theo id (kèm thông tin tài liệu).</summary>
        public GiaoDich LayTheoId(int id)
        {
            const string sql = @"
                SELECT gd.id, gd.user_id, gd.tai_lieu_id, gd.so_tien, gd.ngay_giao_dich, gd.trang_thai,
                       tl.ten_tai_lieu, tl.gia_ban, tl.file_url
                FROM dbo.giao_dich AS gd
                LEFT JOIN dbo.tai_lieu AS tl ON tl.id = gd.tai_lieu_id
                WHERE gd.id = @Id";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", id);

                using (var doc = lenh.ExecuteReader())
                {
                    return doc.Read() ? DocDuLieu(doc) : null;
                }
            }
        }

        /// <summary>
        /// Kiểm tra người đọc đã có giao dịch đang xử lý cho một tài liệu hay chưa,
        /// tránh tạo trùng nhiều giao dịch DANG_XU_LY cho cùng một tài liệu.
        /// </summary>
        public GiaoDich LayGiaoDichDangXuLy(int userId, int taiLieuId)
        {
            const string sql = @"
                SELECT TOP 1 gd.id, gd.user_id, gd.tai_lieu_id, gd.so_tien, gd.ngay_giao_dich, gd.trang_thai,
                       tl.ten_tai_lieu, tl.gia_ban, tl.file_url
                FROM dbo.giao_dich AS gd
                LEFT JOIN dbo.tai_lieu AS tl ON tl.id = gd.tai_lieu_id
                WHERE gd.user_id = @UserId AND gd.tai_lieu_id = @TaiLieuId
                  AND gd.trang_thai = N'DANG_XU_LY'
                ORDER BY gd.id DESC";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@UserId", userId);
                KetNoiDuLieu.ThemThamSo(lenh, "@TaiLieuId", taiLieuId);

                using (var doc = lenh.ExecuteReader())
                {
                    return doc.Read() ? DocDuLieu(doc) : null;
                }
            }
        }

        /// <summary>Lịch sử giao dịch của chính người đọc đang đăng nhập.</summary>
        public List<GiaoDich> LayTheoNguoiDung(int userId)
        {
            const string sql = @"
                SELECT gd.id, gd.user_id, gd.tai_lieu_id, gd.so_tien, gd.ngay_giao_dich, gd.trang_thai,
                       tl.ten_tai_lieu, tl.gia_ban, tl.file_url
                FROM dbo.giao_dich AS gd
                LEFT JOIN dbo.tai_lieu AS tl ON tl.id = gd.tai_lieu_id
                WHERE gd.user_id = @UserId
                ORDER BY gd.ngay_giao_dich DESC, gd.id DESC";

            var ketQua = new List<GiaoDich>();

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@UserId", userId);

                using (var doc = lenh.ExecuteReader())
                {
                    while (doc.Read())
                    {
                        ketQua.Add(DocDuLieu(doc));
                    }
                }
            }

            return ketQua;
        }

        /// <summary>Đếm số giao dịch thành công trong khoảng thời gian.</summary>
        public int DemThanhCong(DateTime tuNgay, DateTime denNgay)
        {
            const string sql = @"SELECT COUNT(1) FROM dbo.giao_dich
                                 WHERE trang_thai = N'THANH_CONG'
                                   AND ngay_giao_dich >= @TuNgay
                                   AND ngay_giao_dich < @DenNgay";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@TuNgay", tuNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@DenNgay", denNgay);
                return Convert.ToInt32(lenh.ExecuteScalar());
            }
        }

        /// <summary>
        /// Tổng doanh thu trong khoảng thời gian, tính bằng hàm fn_TongDoanhThu
        /// của CSDL để dùng chung một định nghĩa doanh thu.
        /// </summary>
        public long TongDoanhThu(DateTime tuNgay, DateTime denNgay)
        {
            const string sql = "SELECT dbo.fn_TongDoanhThu(@TuNgay, @DenNgay)";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@TuNgay", tuNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@DenNgay", denNgay);

                var ketQua = lenh.ExecuteScalar();
                return ketQua == null || ketQua == DBNull.Value ? 0 : Convert.ToInt64(ketQua);
            }
        }

        /// <summary>
        /// Danh sách giao dịch cho quản trị viên, kèm thông tin người mua và tài liệu.
        /// Lọc theo khoảng thời gian, trạng thái và từ khóa (mã giao dịch, người mua, tài liệu).
        /// </summary>
        public List<GiaoDich> LayDanhSachQuanTri(DateTime? tuNgay, DateTime? denNgay,
            string trangThai, string tuKhoa, int soLuongToiDa)
        {
            const string sql = @"
                SELECT TOP (@SoLuong) gd.id, gd.user_id, gd.tai_lieu_id, gd.so_tien,
                       gd.ngay_giao_dich, gd.trang_thai,
                       tl.ten_tai_lieu, tl.gia_ban, tl.file_url,
                       nd.ho_ten, nd.email, nd.vai_tro, nd.trang_thai AS trang_thai_nguoi_dung
                FROM dbo.giao_dich AS gd
                LEFT JOIN dbo.tai_lieu AS tl ON tl.id = gd.tai_lieu_id
                LEFT JOIN dbo.nguoi_dung AS nd ON nd.id = gd.user_id
                WHERE (@TuNgay IS NULL OR gd.ngay_giao_dich >= @TuNgay)
                  AND (@DenNgay IS NULL OR gd.ngay_giao_dich < @DenNgay)
                  AND (@TrangThai IS NULL OR gd.trang_thai = @TrangThai)
                  AND (@TuKhoa IS NULL
                       OR nd.ho_ten LIKE @MauTuKhoa
                       OR nd.email LIKE @MauTuKhoa
                       OR tl.ten_tai_lieu LIKE @MauTuKhoa
                       OR CONVERT(VARCHAR(20), gd.id) = @TuKhoa)
                ORDER BY gd.ngay_giao_dich DESC, gd.id DESC";

            var ketQua = new List<GiaoDich>();

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                var coTuKhoa = !string.IsNullOrWhiteSpace(tuKhoa);

                KetNoiDuLieu.ThemThamSo(lenh, "@SoLuong", soLuongToiDa > 0 ? soLuongToiDa : int.MaxValue);
                KetNoiDuLieu.ThemThamSo(lenh, "@TuNgay", tuNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@DenNgay", denNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai);
                KetNoiDuLieu.ThemThamSo(lenh, "@TuKhoa", coTuKhoa ? tuKhoa.Trim() : null);
                KetNoiDuLieu.ThemThamSo(lenh, "@MauTuKhoa", coTuKhoa ? "%" + tuKhoa.Trim() + "%" : null);

                using (var doc = lenh.ExecuteReader())
                {
                    while (doc.Read())
                    {
                        ketQua.Add(DocDuLieuQuanTri(doc));
                    }
                }
            }

            return ketQua;
        }

        /// <summary>Chi tiết một giao dịch kèm người mua và tài liệu cho quản trị viên.</summary>
        public GiaoDich LayChiTietQuanTri(int id)
        {
            const string sql = @"
                SELECT gd.id, gd.user_id, gd.tai_lieu_id, gd.so_tien,
                       gd.ngay_giao_dich, gd.trang_thai,
                       tl.ten_tai_lieu, tl.gia_ban, tl.file_url,
                       nd.ho_ten, nd.email, nd.vai_tro, nd.trang_thai AS trang_thai_nguoi_dung
                FROM dbo.giao_dich AS gd
                LEFT JOIN dbo.tai_lieu AS tl ON tl.id = gd.tai_lieu_id
                LEFT JOIN dbo.nguoi_dung AS nd ON nd.id = gd.user_id
                WHERE gd.id = @Id";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", id);

                using (var doc = lenh.ExecuteReader())
                {
                    return doc.Read() ? DocDuLieuQuanTri(doc) : null;
                }
            }
        }

        /// <summary>Đếm giao dịch theo trạng thái trong khoảng thời gian (cận trên không tính).</summary>
        public int DemTheoTrangThai(string trangThai, DateTime? tuNgay, DateTime? denNgay)
        {
            const string sql = @"
                SELECT COUNT(1) FROM dbo.giao_dich
                WHERE (@TrangThai IS NULL OR trang_thai = @TrangThai)
                  AND (@TuNgay IS NULL OR ngay_giao_dich >= @TuNgay)
                  AND (@DenNgay IS NULL OR ngay_giao_dich < @DenNgay)";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai);
                KetNoiDuLieu.ThemThamSo(lenh, "@TuNgay", tuNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@DenNgay", denNgay);
                return Convert.ToInt32(lenh.ExecuteScalar());
            }
        }

        /// <summary>
        /// Doanh thu theo từng tài liệu trong khoảng thời gian.
        /// Chỉ tính giao dịch THANH_CONG; giao dịch đang xử lý hoặc thất bại bị loại trừ.
        /// </summary>
        public List<ThongKeTaiLieu> ThongKeDoanhThuTheoTaiLieu(DateTime tuNgay, DateTime denNgay, int soLuongTop)
        {
            const string sql = @"
                SELECT TOP (@SoLuong) tl.id, tl.ten_tai_lieu,
                       COUNT(gd.id) AS so_giao_dich,
                       COALESCE(SUM(CAST(gd.so_tien AS BIGINT)), 0) AS doanh_thu
                FROM dbo.giao_dich AS gd
                INNER JOIN dbo.tai_lieu AS tl ON tl.id = gd.tai_lieu_id
                WHERE gd.trang_thai = N'THANH_CONG'
                  AND gd.ngay_giao_dich >= @TuNgay
                  AND gd.ngay_giao_dich < @DenNgay
                GROUP BY tl.id, tl.ten_tai_lieu
                ORDER BY doanh_thu DESC, tl.id DESC";

            var ketQua = new List<ThongKeTaiLieu>();

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@SoLuong", soLuongTop > 0 ? soLuongTop : int.MaxValue);
                KetNoiDuLieu.ThemThamSo(lenh, "@TuNgay", tuNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@DenNgay", denNgay);

                using (var doc = lenh.ExecuteReader())
                {
                    while (doc.Read())
                    {
                        ketQua.Add(new ThongKeTaiLieu
                        {
                            TaiLieuId = doc.GetInt32(doc.GetOrdinal("id")),
                            TenTaiLieu = LayChuoi(doc, "ten_tai_lieu"),
                            SoGiaoDich = Convert.ToInt32(doc["so_giao_dich"]),
                            DoanhThu = Convert.ToInt64(doc["doanh_thu"])
                        });
                    }
                }
            }

            return ketQua;
        }

        private static GiaoDich DocDuLieuQuanTri(IDataRecord dong)
        {
            var giaoDich = new GiaoDich
            {
                Id = dong.GetInt32(dong.GetOrdinal("id")),
                UserId = LaySoNguyen(dong, "user_id"),
                TaiLieuId = LaySoNguyen(dong, "tai_lieu_id"),
                SoTien = LaySoNguyen(dong, "so_tien"),
                NgayGiaoDich = LayNgay(dong, "ngay_giao_dich"),
                TrangThai = LayChuoi(dong, "trang_thai")
            };

            var tenTaiLieu = LayChuoi(dong, "ten_tai_lieu");
            if (tenTaiLieu != null)
            {
                giaoDich.TaiLieu = new TaiLieu
                {
                    Id = giaoDich.TaiLieuId ?? 0,
                    TenTaiLieu = tenTaiLieu,
                    GiaBan = LaySoNguyen(dong, "gia_ban") ?? 0,
                    FileUrl = LayChuoi(dong, "file_url")
                };
            }

            var emailNguoiDung = LayChuoi(dong, "email");
            if (emailNguoiDung != null || giaoDich.UserId.HasValue)
            {
                giaoDich.NguoiDung = new NguoiDung
                {
                    Id = giaoDich.UserId ?? 0,
                    HoTen = LayChuoi(dong, "ho_ten"),
                    Email = emailNguoiDung,
                    VaiTro = LayChuoi(dong, "vai_tro"),
                    TrangThai = LayChuoi(dong, "trang_thai_nguoi_dung")
                };
            }

            return giaoDich;
        }

        private static GiaoDich DocDuLieu(IDataRecord dong)
        {
            var giaoDich = new GiaoDich
            {
                Id = dong.GetInt32(dong.GetOrdinal("id")),
                UserId = LaySoNguyen(dong, "user_id"),
                TaiLieuId = LaySoNguyen(dong, "tai_lieu_id"),
                SoTien = LaySoNguyen(dong, "so_tien"),
                NgayGiaoDich = LayNgay(dong, "ngay_giao_dich"),
                TrangThai = LayChuoi(dong, "trang_thai")
            };

            var tenTaiLieu = LayChuoi(dong, "ten_tai_lieu");
            if (tenTaiLieu != null)
            {
                giaoDich.TaiLieu = new TaiLieu
                {
                    Id = giaoDich.TaiLieuId ?? 0,
                    TenTaiLieu = tenTaiLieu,
                    GiaBan = LaySoNguyen(dong, "gia_ban") ?? 0,
                    FileUrl = LayChuoi(dong, "file_url")
                };
            }

            return giaoDich;
        }

        private static string LayChuoi(IDataRecord dong, string tenCot)
        {
            var viTri = dong.GetOrdinal(tenCot);
            return dong.IsDBNull(viTri) ? null : dong.GetString(viTri);
        }

        private static int? LaySoNguyen(IDataRecord dong, string tenCot)
        {
            var viTri = dong.GetOrdinal(tenCot);
            return dong.IsDBNull(viTri) ? (int?)null : dong.GetInt32(viTri);
        }

        private static DateTime? LayNgay(IDataRecord dong, string tenCot)
        {
            var viTri = dong.GetOrdinal(tenCot);
            return dong.IsDBNull(viTri) ? (DateTime?)null : dong.GetDateTime(viTri);
        }
    }
}
