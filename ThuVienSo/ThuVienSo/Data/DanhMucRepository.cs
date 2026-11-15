using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ThuVienSo.Models;

namespace ThuVienSo.Data
{
    /// <summary>
    /// Truy cập dữ liệu danh mục tác giả (dbo.tac_gia) và thể loại (dbo.the_loai).
    /// Hai bảng dùng khoá chính INT không tự tăng nên id mới được sinh bằng MAX(id) + 1
    /// trong transaction có khoá bảng, giống cách thêm tài liệu.
    /// </summary>
    public class DanhMucRepository
    {
        // ============================ THỂ LOẠI ============================

        /// <summary>Danh sách thể loại kèm số tài liệu đang dùng, dùng cho trang quản lý danh mục.</summary>
        public List<TheLoai> LayDanhSachTheLoai()
        {
            const string sql = @"
                SELECT thl.id, thl.ten_the_loai, thl.mo_ta,
                       (SELECT COUNT(*) FROM dbo.tai_lieu AS tl WHERE tl.the_loai_id = thl.id) AS so_tai_lieu
                FROM dbo.the_loai AS thl
                ORDER BY thl.ten_the_loai";

            var ketQua = new List<TheLoai>();
            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            using (var doc = lenh.ExecuteReader())
            {
                while (doc.Read())
                {
                    ketQua.Add(new TheLoai
                    {
                        Id = doc.GetInt32(doc.GetOrdinal("id")),
                        TenTheLoai = LayChuoi(doc, "ten_the_loai"),
                        MoTa = LayChuoi(doc, "mo_ta"),
                        SoTaiLieu = doc.GetInt32(doc.GetOrdinal("so_tai_lieu"))
                    });
                }
            }
            return ketQua;
        }

        /// <summary>Kiểm tra trùng tên thể loại; bỏ qua một thể loại khi đang sửa.</summary>
        public bool TenTheLoaiDaTonTai(string tenTheLoai, int? boQuaId)
        {
            const string sql = @"
                SELECT COUNT(*) FROM dbo.the_loai
                WHERE ten_the_loai = @Ten AND (@BoQuaId IS NULL OR id <> @BoQuaId)";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Ten", tenTheLoai);
                KetNoiDuLieu.ThemThamSo(lenh, "@BoQuaId", boQuaId.HasValue ? (object)boQuaId.Value : DBNull.Value);
                return Convert.ToInt32(lenh.ExecuteScalar()) > 0;
            }
        }

        public bool ThemTheLoai(TheLoai theLoai)
        {
            const string sql = "INSERT INTO dbo.the_loai (id, ten_the_loai, mo_ta) VALUES (@Id, @Ten, @MoTa)";
            return ThemVaoDanhMuc("dbo.the_loai", sql, theLoai, lenh =>
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Ten", theLoai.TenTheLoai);
                KetNoiDuLieu.ThemThamSo(lenh, "@MoTa", theLoai.MoTa);
            });
        }

        public bool CapNhatTheLoai(TheLoai theLoai)
        {
            const string sql = "UPDATE dbo.the_loai SET ten_the_loai = @Ten, mo_ta = @MoTa WHERE id = @Id";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", theLoai.Id);
                KetNoiDuLieu.ThemThamSo(lenh, "@Ten", theLoai.TenTheLoai);
                KetNoiDuLieu.ThemThamSo(lenh, "@MoTa", theLoai.MoTa);
                return lenh.ExecuteNonQuery() > 0;
            }
        }

        public bool XoaTheLoai(int id)
        {
            return XoaKhoiDanhMuc("dbo.the_loai", id);
        }

        // ============================ TÁC GIẢ ============================

        /// <summary>Danh sách tác giả kèm số tài liệu đang dùng, dùng cho trang quản lý danh mục.</summary>
        public List<TacGia> LayDanhSachTacGia()
        {
            const string sql = @"
                SELECT tg.id, tg.ten_tac_gia, tg.quoc_tich,
                       (SELECT COUNT(*) FROM dbo.tai_lieu AS tl WHERE tl.tac_gia_id = tg.id) AS so_tai_lieu
                FROM dbo.tac_gia AS tg
                ORDER BY tg.ten_tac_gia";

            var ketQua = new List<TacGia>();
            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            using (var doc = lenh.ExecuteReader())
            {
                while (doc.Read())
                {
                    ketQua.Add(new TacGia
                    {
                        Id = doc.GetInt32(doc.GetOrdinal("id")),
                        TenTacGia = LayChuoi(doc, "ten_tac_gia"),
                        QuocTich = LayChuoi(doc, "quoc_tich"),
                        SoTaiLieu = doc.GetInt32(doc.GetOrdinal("so_tai_lieu"))
                    });
                }
            }
            return ketQua;
        }

        /// <summary>Kiểm tra trùng tên tác giả; bỏ qua một tác giả khi đang sửa.</summary>
        public bool TenTacGiaDaTonTai(string tenTacGia, int? boQuaId)
        {
            const string sql = @"
                SELECT COUNT(*) FROM dbo.tac_gia
                WHERE ten_tac_gia = @Ten AND (@BoQuaId IS NULL OR id <> @BoQuaId)";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Ten", tenTacGia);
                KetNoiDuLieu.ThemThamSo(lenh, "@BoQuaId", boQuaId.HasValue ? (object)boQuaId.Value : DBNull.Value);
                return Convert.ToInt32(lenh.ExecuteScalar()) > 0;
            }
        }

        public bool ThemTacGia(TacGia tacGia)
        {
            const string sql = "INSERT INTO dbo.tac_gia (id, ten_tac_gia, quoc_tich) VALUES (@Id, @Ten, @QuocTich)";
            return ThemVaoDanhMuc("dbo.tac_gia", sql, tacGia, lenh =>
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Ten", tacGia.TenTacGia);
                KetNoiDuLieu.ThemThamSo(lenh, "@QuocTich", tacGia.QuocTich);
            });
        }

        public bool CapNhatTacGia(TacGia tacGia)
        {
            const string sql = "UPDATE dbo.tac_gia SET ten_tac_gia = @Ten, quoc_tich = @QuocTich WHERE id = @Id";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", tacGia.Id);
                KetNoiDuLieu.ThemThamSo(lenh, "@Ten", tacGia.TenTacGia);
                KetNoiDuLieu.ThemThamSo(lenh, "@QuocTich", tacGia.QuocTich);
                return lenh.ExecuteNonQuery() > 0;
            }
        }

        public bool XoaTacGia(int id)
        {
            return XoaKhoiDanhMuc("dbo.tac_gia", id);
        }

        // ============================ DÙNG CHUNG ============================

        /// <summary>
        /// Thêm một bản ghi danh mục với id sinh bằng MAX(id) + 1 trong transaction khoá bảng,
        /// tránh trùng khoá khi nhiều thủ thư thao tác cùng lúc.
        /// </summary>
        private static bool ThemVaoDanhMuc(string tenBang, string sqlThem, object doiTuong,
            Action<SqlCommand> ganThamSo)
        {
            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    int idMoi;
                    using (var lenhId = KetNoiDuLieu.TaoLenh(
                        "SELECT ISNULL(MAX(id), 0) + 1 FROM " + tenBang + " WITH (TABLOCKX, HOLDLOCK)",
                        ketNoi, giaoDich))
                    {
                        idMoi = Convert.ToInt32(lenhId.ExecuteScalar());
                    }

                    using (var lenh = KetNoiDuLieu.TaoLenh(sqlThem, ketNoi, giaoDich))
                    {
                        KetNoiDuLieu.ThemThamSo(lenh, "@Id", idMoi);
                        ganThamSo(lenh);
                        lenh.ExecuteNonQuery();
                    }

                    giaoDich.Commit();
                    return true;
                }
                catch
                {
                    giaoDich.Rollback();
                    return false;
                }
            }
        }

        /// <summary>Xoá một bản ghi danh mục; ném SqlException nếu đang bị tài liệu tham chiếu.</summary>
        private static bool XoaKhoiDanhMuc(string tenBang, int id)
        {
            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh("DELETE FROM " + tenBang + " WHERE id = @Id", ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", id);
                return lenh.ExecuteNonQuery() > 0;
            }
        }

        private static string LayChuoi(IDataRecord dong, string tenCot)
        {
            var viTri = dong.GetOrdinal(tenCot);
            return dong.IsDBNull(viTri) ? null : dong.GetString(viTri);
        }
    }
}
