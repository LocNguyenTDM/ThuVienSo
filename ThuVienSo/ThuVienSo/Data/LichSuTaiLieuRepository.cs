using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ThuVienSo.Models;

namespace ThuVienSo.Data
{
    /// <summary>
    /// Truy cập lịch sử xem/tải tài liệu (dbo.lich_su_tai_lieu).
    /// Việc ghi nhận lượt xem/tải dùng lại stored procedure sp_GhiNhanLuotTaiLieu
    /// của CSDL để tận dụng kiểm tra quyền và ghi nhật ký hoạt động.
    /// </summary>
    public class LichSuTaiLieuRepository
    {
        /// <summary>
        /// Ghi nhận lượt xem hoặc tải. Procedure sẽ tự kiểm tra quyền,
        /// ném SqlException với thông báo tiếng Việt nếu không hợp lệ.
        /// </summary>
        public void GhiNhanLuot(int userId, int taiLieuId, string hanhDong)
        {
            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh("dbo.sp_GhiNhanLuotTaiLieu", ketNoi))
            {
                lenh.CommandType = CommandType.StoredProcedure;
                KetNoiDuLieu.ThemThamSo(lenh, "@UserID", userId);
                KetNoiDuLieu.ThemThamSo(lenh, "@TaiLieuID", taiLieuId);
                KetNoiDuLieu.ThemThamSo(lenh, "@HanhDong", hanhDong);

                lenh.ExecuteNonQuery();
            }
        }

        /// <summary>Lịch sử xem/tải gần đây của một người đọc.</summary>
        public List<LichSuTaiLieu> LayTheoNguoiDung(int userId, int soDongToiDa)
        {
            const string sql = @"
                SELECT TOP (@SoDong) ls.id, ls.user_id, ls.tai_lieu_id, ls.hanh_dong, ls.thoi_gian,
                       tl.ten_tai_lieu, tl.file_url, thl.ten_the_loai, tg.ten_tac_gia
                FROM dbo.lich_su_tai_lieu AS ls
                INNER JOIN dbo.tai_lieu AS tl ON tl.id = ls.tai_lieu_id
                LEFT JOIN dbo.the_loai AS thl ON thl.id = tl.the_loai_id
                LEFT JOIN dbo.tac_gia AS tg ON tg.id = tl.tac_gia_id
                WHERE ls.user_id = @UserId
                ORDER BY ls.thoi_gian DESC, ls.id DESC";

            var ketQua = new List<LichSuTaiLieu>();

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@UserId", userId);
                KetNoiDuLieu.ThemThamSo(lenh, "@SoDong", soDongToiDa > 0 ? soDongToiDa : int.MaxValue);

                using (var doc = lenh.ExecuteReader())
                {
                    while (doc.Read())
                    {
                        ketQua.Add(new LichSuTaiLieu
                        {
                            Id = doc.GetInt32(doc.GetOrdinal("id")),
                            UserId = doc.GetInt32(doc.GetOrdinal("user_id")),
                            TaiLieuId = doc.GetInt32(doc.GetOrdinal("tai_lieu_id")),
                            HanhDong = doc.GetString(doc.GetOrdinal("hanh_dong")),
                            ThoiGian = doc.GetDateTime(doc.GetOrdinal("thoi_gian")),
                            TaiLieu = new TaiLieu
                            {
                                Id = doc.GetInt32(doc.GetOrdinal("tai_lieu_id")),
                                TenTaiLieu = LayChuoi(doc, "ten_tai_lieu"),
                                FileUrl = LayChuoi(doc, "file_url"),
                                TheLoai = LayChuoi(doc, "ten_the_loai") == null
                                    ? null
                                    : new TheLoai { TenTheLoai = LayChuoi(doc, "ten_the_loai") },
                                TacGia = LayChuoi(doc, "ten_tac_gia") == null
                                    ? null
                                    : new TacGia { TenTacGia = LayChuoi(doc, "ten_tac_gia") }
                            }
                        });
                    }
                }
            }

            return ketQua;
        }

        /// <summary>Lượt xem/tải gần nhất theo hành động (dùng cho mục "đang đọc gần đây").</summary>
        public LichSuTaiLieu LayGanNhat(int userId, string hanhDong)
        {
            const string sql = @"
                SELECT TOP 1 ls.id, ls.user_id, ls.tai_lieu_id, ls.hanh_dong, ls.thoi_gian,
                       tl.ten_tai_lieu, tl.file_url, thl.ten_the_loai, tg.ten_tac_gia
                FROM dbo.lich_su_tai_lieu AS ls
                INNER JOIN dbo.tai_lieu AS tl ON tl.id = ls.tai_lieu_id
                LEFT JOIN dbo.the_loai AS thl ON thl.id = tl.the_loai_id
                LEFT JOIN dbo.tac_gia AS tg ON tg.id = tl.tac_gia_id
                WHERE ls.user_id = @UserId AND ls.hanh_dong = @HanhDong
                ORDER BY ls.thoi_gian DESC, ls.id DESC";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@UserId", userId);
                KetNoiDuLieu.ThemThamSo(lenh, "@HanhDong", hanhDong);

                using (var doc = lenh.ExecuteReader())
                {
                    if (!doc.Read()) return null;

                    return new LichSuTaiLieu
                    {
                        Id = doc.GetInt32(doc.GetOrdinal("id")),
                        UserId = doc.GetInt32(doc.GetOrdinal("user_id")),
                        TaiLieuId = doc.GetInt32(doc.GetOrdinal("tai_lieu_id")),
                        HanhDong = doc.GetString(doc.GetOrdinal("hanh_dong")),
                        ThoiGian = doc.GetDateTime(doc.GetOrdinal("thoi_gian")),
                        TaiLieu = new TaiLieu
                        {
                            Id = doc.GetInt32(doc.GetOrdinal("tai_lieu_id")),
                            TenTaiLieu = LayChuoi(doc, "ten_tai_lieu"),
                            FileUrl = LayChuoi(doc, "file_url"),
                            TheLoai = LayChuoi(doc, "ten_the_loai") == null
                                ? null
                                : new TheLoai { TenTheLoai = LayChuoi(doc, "ten_the_loai") },
                            TacGia = LayChuoi(doc, "ten_tac_gia") == null
                                ? null
                                : new TacGia { TenTacGia = LayChuoi(doc, "ten_tac_gia") }
                        }
                    };
                }
            }
        }

        /// <summary>Đếm số tài liệu khác nhau mà người đọc đã xem trực tuyến.</summary>
        public int DemTaiLieuDaXem(int userId)
        {
            const string sql = @"SELECT COUNT(DISTINCT tai_lieu_id) FROM dbo.lich_su_tai_lieu
                                 WHERE user_id = @UserId AND hanh_dong = N'XEM'";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@UserId", userId);
                return Convert.ToInt32(lenh.ExecuteScalar());
            }
        }

        /// <summary>
        /// Đếm số lượt theo hành động (XEM | TAI) trong một khoảng thời gian.
        /// Truyền hanhDong = null để đếm tất cả hành động.
        /// </summary>
        public int DemTheoHanhDong(string hanhDong, DateTime? tuNgay, DateTime? denNgay)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.lich_su_tai_lieu
                WHERE (@HanhDong IS NULL OR hanh_dong = @HanhDong)
                  AND (@TuNgay IS NULL OR thoi_gian >= @TuNgay)
                  AND (@DenNgay IS NULL OR thoi_gian < @DenNgay)";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@HanhDong", string.IsNullOrWhiteSpace(hanhDong) ? null : hanhDong);
                KetNoiDuLieu.ThemThamSo(lenh, "@TuNgay", tuNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@DenNgay", denNgay);
                return Convert.ToInt32(lenh.ExecuteScalar());
            }
        }

        /// <summary>
        /// Nhật ký đọc/tải trong khoảng thời gian kèm tên độc giả, dùng cho
        /// báo cáo lưu lượng của thủ thư.
        /// </summary>
        public List<LichSuTaiLieu> LayTheoKhoangThoiGian(DateTime tuNgay, DateTime denNgay, string hanhDong, int soDongToiDa)
        {
            const string sql = @"
                SELECT TOP (@SoDong) ls.id, ls.user_id, ls.tai_lieu_id, ls.hanh_dong, ls.thoi_gian,
                       tl.ten_tai_lieu, thl.ten_the_loai, tg.ten_tac_gia,
                       nd.ho_ten, nd.email
                FROM dbo.lich_su_tai_lieu AS ls
                INNER JOIN dbo.tai_lieu AS tl ON tl.id = ls.tai_lieu_id
                LEFT JOIN dbo.the_loai AS thl ON thl.id = tl.the_loai_id
                LEFT JOIN dbo.tac_gia AS tg ON tg.id = tl.tac_gia_id
                LEFT JOIN dbo.nguoi_dung AS nd ON nd.id = ls.user_id
                WHERE ls.thoi_gian >= @TuNgay AND ls.thoi_gian < @DenNgay
                  AND (@HanhDong IS NULL OR ls.hanh_dong = @HanhDong)
                ORDER BY ls.thoi_gian DESC, ls.id DESC";

            var ketQua = new List<LichSuTaiLieu>();

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@TuNgay", tuNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@DenNgay", denNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@HanhDong", string.IsNullOrWhiteSpace(hanhDong) ? null : hanhDong);
                KetNoiDuLieu.ThemThamSo(lenh, "@SoDong", soDongToiDa > 0 ? soDongToiDa : int.MaxValue);

                using (var doc = lenh.ExecuteReader())
                {
                    while (doc.Read())
                    {
                        ketQua.Add(new LichSuTaiLieu
                        {
                            Id = doc.GetInt32(doc.GetOrdinal("id")),
                            UserId = doc.GetInt32(doc.GetOrdinal("user_id")),
                            TaiLieuId = doc.GetInt32(doc.GetOrdinal("tai_lieu_id")),
                            HanhDong = doc.GetString(doc.GetOrdinal("hanh_dong")),
                            ThoiGian = doc.GetDateTime(doc.GetOrdinal("thoi_gian")),
                            TaiLieu = new TaiLieu
                            {
                                Id = doc.GetInt32(doc.GetOrdinal("tai_lieu_id")),
                                TenTaiLieu = LayChuoi(doc, "ten_tai_lieu"),
                                TheLoai = LayChuoi(doc, "ten_the_loai") == null
                                    ? null
                                    : new TheLoai { TenTheLoai = LayChuoi(doc, "ten_the_loai") },
                                TacGia = LayChuoi(doc, "ten_tac_gia") == null
                                    ? null
                                    : new TacGia { TenTacGia = LayChuoi(doc, "ten_tac_gia") }
                            },
                            NguoiDung = new NguoiDung
                            {
                                Id = doc.GetInt32(doc.GetOrdinal("user_id")),
                                HoTen = LayChuoi(doc, "ho_ten"),
                                Email = LayChuoi(doc, "email")
                            }
                        });
                    }
                }
            }

            return ketQua;
        }

        /// <summary>
        /// Bảng xếp hạng tài liệu theo tổng lượt xem + tải trong khoảng thời gian.
        /// </summary>
        public List<ThongKeTaiLieu> ThongKeTheoTaiLieu(DateTime tuNgay, DateTime denNgay, int soLuongTop)
        {
            const string sql = @"
                SELECT TOP (@SoLuong) tl.id, tl.ten_tai_lieu,
                       SUM(CASE WHEN ls.hanh_dong = N'XEM' THEN 1 ELSE 0 END) AS luot_xem,
                       SUM(CASE WHEN ls.hanh_dong = N'TAI' THEN 1 ELSE 0 END) AS luot_tai
                FROM dbo.lich_su_tai_lieu AS ls
                INNER JOIN dbo.tai_lieu AS tl ON tl.id = ls.tai_lieu_id
                WHERE ls.thoi_gian >= @TuNgay AND ls.thoi_gian < @DenNgay
                GROUP BY tl.id, tl.ten_tai_lieu
                ORDER BY (SUM(CASE WHEN ls.hanh_dong = N'XEM' THEN 1 ELSE 0 END)
                          + SUM(CASE WHEN ls.hanh_dong = N'TAI' THEN 1 ELSE 0 END)) DESC, tl.id DESC";

            var ketQua = new List<ThongKeTaiLieu>();

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@TuNgay", tuNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@DenNgay", denNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@SoLuong", soLuongTop > 0 ? soLuongTop : int.MaxValue);

                using (var doc = lenh.ExecuteReader())
                {
                    while (doc.Read())
                    {
                        ketQua.Add(new ThongKeTaiLieu
                        {
                            TaiLieuId = doc.GetInt32(doc.GetOrdinal("id")),
                            TenTaiLieu = LayChuoi(doc, "ten_tai_lieu"),
                            LuotXem = Convert.ToInt32(doc["luot_xem"]),
                            LuotTai = Convert.ToInt32(doc["luot_tai"])
                        });
                    }
                }
            }

            return ketQua;
        }

        private static string LayChuoi(IDataRecord dong, string tenCot)
        {
            var viTri = dong.GetOrdinal(tenCot);
            return dong.IsDBNull(viTri) ? null : dong.GetString(viTri);
        }
    }
}
