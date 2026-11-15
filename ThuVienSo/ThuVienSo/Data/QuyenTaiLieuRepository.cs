using System;
using System.Collections.Generic;
using System.Data;
using ThuVienSo.Models;

namespace ThuVienSo.Data
{
    /// <summary>
    /// Truy cập dữ liệu quyền tải tài liệu (dbo.quyen_tai_lieu).
    /// Quyền chỉ được cấp tự động bởi trigger sau khi giao dịch thành công.
    /// </summary>
    public class QuyenTaiLieuRepository
    {
        /// <summary>
        /// Kiểm tra quyền tải/truy cập tài liệu của người đọc bằng function
        /// fn_CoQuyenTaiTaiLieu của CSDL (đây là nguồn xác thực quyền duy nhất).
        /// </summary>
        public bool CoQuyenTai(int userId, int taiLieuId)
        {
            const string sql = "SELECT dbo.fn_CoQuyenTaiTaiLieu(@UserId, @TaiLieuId)";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@UserId", userId);
                KetNoiDuLieu.ThemThamSo(lenh, "@TaiLieuId", taiLieuId);

                var ketQua = lenh.ExecuteScalar();
                return ketQua != null && ketQua != DBNull.Value && Convert.ToBoolean(ketQua);
            }
        }

        /// <summary>Danh sách quyền tải còn hiệu lực của một người đọc (Tủ sách cá nhân).</summary>
        public List<QuyenTaiLieu> LayTheoNguoiDung(int userId)
        {
            const string sql = @"
                SELECT q.id, q.user_id, q.tai_lieu_id, q.giao_dich_id, q.ngay_cap, q.trang_thai,
                       tl.ten_tai_lieu, tl.file_url, tl.gia_ban, tl.trang_thai AS trang_thai_tai_lieu,
                       thl.ten_the_loai, tg.ten_tac_gia
                FROM dbo.quyen_tai_lieu AS q
                INNER JOIN dbo.tai_lieu AS tl ON tl.id = q.tai_lieu_id
                LEFT JOIN dbo.the_loai AS thl ON thl.id = tl.the_loai_id
                LEFT JOIN dbo.tac_gia AS tg ON tg.id = tl.tac_gia_id
                WHERE q.user_id = @UserId
                ORDER BY q.ngay_cap DESC, q.id DESC";

            var ketQua = new List<QuyenTaiLieu>();

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@UserId", userId);

                using (var doc = lenh.ExecuteReader())
                {
                    while (doc.Read())
                    {
                        var quyen = new QuyenTaiLieu
                        {
                            Id = doc.GetInt32(doc.GetOrdinal("id")),
                            UserId = doc.GetInt32(doc.GetOrdinal("user_id")),
                            TaiLieuId = doc.GetInt32(doc.GetOrdinal("tai_lieu_id")),
                            GiaoDichId = LaySoNguyen(doc, "giao_dich_id"),
                            NgayCap = doc.GetDateTime(doc.GetOrdinal("ngay_cap")),
                            TrangThai = doc.GetString(doc.GetOrdinal("trang_thai")),
                            TaiLieu = new TaiLieu
                            {
                                Id = doc.GetInt32(doc.GetOrdinal("tai_lieu_id")),
                                TenTaiLieu = LayChuoi(doc, "ten_tai_lieu"),
                                FileUrl = LayChuoi(doc, "file_url"),
                                GiaBan = doc.GetInt32(doc.GetOrdinal("gia_ban")),
                                TrangThai = LayChuoi(doc, "trang_thai_tai_lieu"),
                                TheLoai = LayChuoi(doc, "ten_the_loai") == null
                                    ? null
                                    : new TheLoai { TenTheLoai = LayChuoi(doc, "ten_the_loai") },
                                TacGia = LayChuoi(doc, "ten_tac_gia") == null
                                    ? null
                                    : new TacGia { TenTacGia = LayChuoi(doc, "ten_tac_gia") }
                            }
                        };

                        ketQua.Add(quyen);
                    }
                }
            }

            return ketQua;
        }

        /// <summary>Đếm số quyền tải còn hiệu lực của người đọc.</summary>
        public int DemQuyenHieuLuc(int userId)
        {
            const string sql = @"SELECT COUNT(1) FROM dbo.quyen_tai_lieu
                                 WHERE user_id = @UserId AND trang_thai = N'HOAT_DONG'";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@UserId", userId);
                return Convert.ToInt32(lenh.ExecuteScalar());
            }
        }

        /// <summary>Lấy bản ghi quyền tải được cấp bởi một giao dịch (dùng cho chi tiết giao dịch).</summary>
        public QuyenTaiLieu LayTheoGiaoDich(int giaoDichId)
        {
            const string sql = @"
                SELECT id, user_id, tai_lieu_id, giao_dich_id, ngay_cap, trang_thai
                FROM dbo.quyen_tai_lieu
                WHERE giao_dich_id = @GiaoDichId";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@GiaoDichId", giaoDichId);

                using (var doc = lenh.ExecuteReader())
                {
                    if (!doc.Read()) return null;

                    return new QuyenTaiLieu
                    {
                        Id = doc.GetInt32(doc.GetOrdinal("id")),
                        UserId = doc.GetInt32(doc.GetOrdinal("user_id")),
                        TaiLieuId = doc.GetInt32(doc.GetOrdinal("tai_lieu_id")),
                        GiaoDichId = LaySoNguyen(doc, "giao_dich_id"),
                        NgayCap = doc.GetDateTime(doc.GetOrdinal("ngay_cap")),
                        TrangThai = doc.GetString(doc.GetOrdinal("trang_thai"))
                    };
                }
            }
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
    }
}
