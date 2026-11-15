using System;
using System.Collections.Generic;
using System.Data;
using ThuVienSo.Models;

namespace ThuVienSo.Data
{
    /// <summary>
    /// Ghi nhật ký hoạt động vào dbo.nhat_ky_hoat_dong.
    /// Bảng này không có IDENTITY nên id sinh bằng MAX(id) + 1 trong transaction,
    /// cùng cách với procedure sp_GhiNhanLuotTaiLieu của CSDL.
    /// </summary>
    public class NhatKyHoatDongRepository
    {
        public void GhiNhatKy(int? userId, string hanhDong)
        {
            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    int idMoi;
                    using (var lenhId = KetNoiDuLieu.TaoLenh(
                        "SELECT ISNULL(MAX(id), 0) + 1 FROM dbo.nhat_ky_hoat_dong WITH (TABLOCKX, HOLDLOCK)",
                        ketNoi, giaoDich))
                    {
                        idMoi = Convert.ToInt32(lenhId.ExecuteScalar());
                    }

                    const string sql = @"INSERT INTO dbo.nhat_ky_hoat_dong (id, user_id, hanh_dong, thoi_gian)
                                         VALUES (@Id, @UserId, @HanhDong, GETDATE())";

                    using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi, giaoDich))
                    {
                        KetNoiDuLieu.ThemThamSo(lenh, "@Id", idMoi);
                        KetNoiDuLieu.ThemThamSo(lenh, "@UserId", userId);
                        KetNoiDuLieu.ThemThamSo(lenh, "@HanhDong", hanhDong);

                        lenh.ExecuteNonQuery();
                    }

                    giaoDich.Commit();
                }
                catch
                {
                    giaoDich.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Danh sách nhật ký hoạt động cho quản trị viên, lọc theo người dùng,
        /// khoảng thời gian và từ khóa trong nội dung hành động.
        /// </summary>
        public List<NhatKyHoatDong> LayDanhSach(int? userId, DateTime? tuNgay, DateTime? denNgay,
            string tuKhoa, int soDongToiDa)
        {
            const string sql = @"
                SELECT TOP (@SoDong) nk.id, nk.user_id, nk.hanh_dong, nk.thoi_gian,
                       nd.ho_ten, nd.email, nd.vai_tro
                FROM dbo.nhat_ky_hoat_dong AS nk
                LEFT JOIN dbo.nguoi_dung AS nd ON nd.id = nk.user_id
                WHERE (@UserId IS NULL OR nk.user_id = @UserId)
                  AND (@TuNgay IS NULL OR nk.thoi_gian >= @TuNgay)
                  AND (@DenNgay IS NULL OR nk.thoi_gian < @DenNgay)
                  AND (@TuKhoa IS NULL OR nk.hanh_dong LIKE @MauTuKhoa)
                ORDER BY nk.thoi_gian DESC, nk.id DESC";

            var ketQua = new List<NhatKyHoatDong>();

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                var coTuKhoa = !string.IsNullOrWhiteSpace(tuKhoa);

                KetNoiDuLieu.ThemThamSo(lenh, "@SoDong", soDongToiDa > 0 ? soDongToiDa : int.MaxValue);
                KetNoiDuLieu.ThemThamSo(lenh, "@UserId", userId);
                KetNoiDuLieu.ThemThamSo(lenh, "@TuNgay", tuNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@DenNgay", denNgay);
                KetNoiDuLieu.ThemThamSo(lenh, "@TuKhoa", coTuKhoa ? tuKhoa.Trim() : null);
                KetNoiDuLieu.ThemThamSo(lenh, "@MauTuKhoa", coTuKhoa ? "%" + tuKhoa.Trim() + "%" : null);

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

        /// <summary>Nhật ký hoạt động gần nhất dùng cho bảng điều khiển quản trị.</summary>
        public List<NhatKyHoatDong> LayGanDay(int soDong)
        {
            return LayDanhSach(null, null, null, null, soDong);
        }

        private static NhatKyHoatDong DocDuLieu(IDataRecord dong)
        {
            var nhatKy = new NhatKyHoatDong
            {
                Id = dong.GetInt32(dong.GetOrdinal("id")),
                UserId = dong.IsDBNull(dong.GetOrdinal("user_id")) ? (int?)null : dong.GetInt32(dong.GetOrdinal("user_id")),
                HanhDong = dong.IsDBNull(dong.GetOrdinal("hanh_dong")) ? null : dong.GetString(dong.GetOrdinal("hanh_dong")),
                ThoiGian = dong.IsDBNull(dong.GetOrdinal("thoi_gian")) ? (DateTime?)null : dong.GetDateTime(dong.GetOrdinal("thoi_gian"))
            };

            var email = LayChuoi(dong, "email");
            if (email != null || nhatKy.UserId.HasValue)
            {
                nhatKy.NguoiDung = new NguoiDung
                {
                    Id = nhatKy.UserId ?? 0,
                    HoTen = LayChuoi(dong, "ho_ten"),
                    Email = email,
                    VaiTro = LayChuoi(dong, "vai_tro")
                };
            }

            return nhatKy;
        }

        private static string LayChuoi(IDataRecord dong, string tenCot)
        {
            var viTri = dong.GetOrdinal(tenCot);
            return dong.IsDBNull(viTri) ? null : dong.GetString(viTri);
        }
    }
}
