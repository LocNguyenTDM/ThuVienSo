using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ThuVienSo.Models;

namespace ThuVienSo.Data
{
    /// <summary>
    /// Truy cập dữ liệu tài liệu số (dbo.tai_lieu) kèm thể loại và tác giả.
    /// Chỉ trả về tài liệu đang HOAT_DONG cho các chức năng phía người đọc.
    /// </summary>
    public class TaiLieuRepository
    {
        private const string CotChon = @"
            tl.id, tl.ten_tai_lieu, tl.mo_ta, tl.file_url, tl.gia_ban,
            tl.the_loai_id, tl.tac_gia_id, tl.nguoi_dang_id,
            tl.quyen_doc_online, tl.quyen_tai, tl.trang_thai, tl.ly_do_tu_choi,
            thl.ten_the_loai, tg.ten_tac_gia";

        private const string TuBang = @"
            FROM dbo.tai_lieu AS tl
            LEFT JOIN dbo.the_loai AS thl ON thl.id = tl.the_loai_id
            LEFT JOIN dbo.tac_gia AS tg ON tg.id = tl.tac_gia_id";

        /// <summary>
        /// Tìm kiếm tài liệu theo từ khóa, có thể giới hạn phạm vi tìm theo tên tài liệu
        /// hoặc tác giả (phamVi = "ten" | "tacgia"; để trống là tìm mọi trường),
        /// lọc theo thể loại và định dạng tệp. Truy vấn có tham số hoá.
        /// Bổ sung lọc theo giá bán (laMienPhi = true/false).
        /// </summary>
        public List<TaiLieu> LayDanhSach(string tuKhoa, int? theLoaiId, string dinhDang, string phamVi, int soLuongToiDa, bool? laMienPhi)
        {
            var sql = @"SELECT TOP (@SoLuong) " + CotChon + TuBang + @"
                WHERE tl.trang_thai = N'HOAT_DONG'
                  AND (@TuKhoa IS NULL
                       OR (@PhamVi = N'ten' AND tl.ten_tai_lieu LIKE @MauTuKhoa)
                       OR (@PhamVi = N'tacgia' AND tg.ten_tac_gia LIKE @MauTuKhoa)
                       OR (@PhamVi IS NULL AND (tl.ten_tai_lieu LIKE @MauTuKhoa
                                                OR tl.mo_ta LIKE @MauTuKhoa
                                                OR thl.ten_the_loai LIKE @MauTuKhoa
                                                OR tg.ten_tac_gia LIKE @MauTuKhoa)))
                  AND (@TheLoaiId IS NULL OR tl.the_loai_id = @TheLoaiId)
                  AND (@DinhDang IS NULL OR tl.file_url LIKE @MauDinhDang)
                  AND (@LaMienPhi IS NULL OR (@LaMienPhi = 1 AND tl.gia_ban = 0) OR (@LaMienPhi = 0 AND tl.gia_ban > 0))
                ORDER BY tl.id DESC";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                var coTuKhoa = !string.IsNullOrWhiteSpace(tuKhoa);
                var dungDinhDang = !string.IsNullOrWhiteSpace(dinhDang);
                var phamViHopLe = ChuanHoaPhamVi(phamVi);

                KetNoiDuLieu.ThemThamSo(lenh, "@SoLuong", soLuongToiDa > 0 ? soLuongToiDa : int.MaxValue);
                KetNoiDuLieu.ThemThamSo(lenh, "@TuKhoa", coTuKhoa ? tuKhoa.Trim() : null);
                KetNoiDuLieu.ThemThamSo(lenh, "@MauTuKhoa", coTuKhoa ? "%" + tuKhoa.Trim() + "%" : null);
                KetNoiDuLieu.ThemThamSo(lenh, "@PhamVi", coTuKhoa ? phamViHopLe : null);
                KetNoiDuLieu.ThemThamSo(lenh, "@TheLoaiId", theLoaiId);
                KetNoiDuLieu.ThemThamSo(lenh, "@DinhDang", dungDinhDang ? dinhDang.Trim() : null);
                KetNoiDuLieu.ThemThamSo(lenh, "@MauDinhDang", dungDinhDang ? "%." + dinhDang.Trim() : null);
                KetNoiDuLieu.ThemThamSo(lenh, "@LaMienPhi", laMienPhi);

                return DocDanhSach(lenh);
            }
        }

        /// <summary>
        /// Chuẩn hoá phạm vi tìm kiếm: chỉ nhận "ten" hoặc "tacgia",
        /// mọi giá trị khác (kể cả rỗng) được coi là tìm trên toàn bộ trường.
        /// </summary>
        public static string ChuanHoaPhamVi(string phamVi)
        {
            if (string.IsNullOrWhiteSpace(phamVi)) return null;

            var giaTri = phamVi.Trim().ToLowerInvariant();
            if (giaTri == "ten" || giaTri == "tacgia") return giaTri;

            return null;
        }

        /// <summary>Lấy chi tiết một tài liệu theo id.</summary>
        public TaiLieu LayTheoId(int id)
        {
            var sql = "SELECT " + CotChon + TuBang + " WHERE tl.id = @Id";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", id);

                var danhSach = DocDanhSach(lenh);
                return danhSach.Count > 0 ? danhSach[0] : null;
            }
        }

        /// <summary>Lấy tài liệu mới nhất (dùng khi mở trình đọc mà không chỉ định tài liệu).</summary>
        public TaiLieu LayMoiNhat()
        {
            var danhSach = LayDanhSach(null, null, null, null, 1, null);
            return danhSach.Count > 0 ? danhSach[0] : null;
        }

        /// <summary>Lấy danh sách tài liệu đang chờ duyệt (CHO_DUYET).</summary>
        public List<TaiLieu> LayDanhSachChoDuyet()
        {
            var sql = "SELECT " + CotChon + TuBang + " WHERE tl.trang_thai = N'CHO_DUYET' ORDER BY tl.id ASC";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                return DocDanhSach(lenh);
            }
        }

        /// <summary>Phê duyệt tài liệu: chuyển trạng thái từ CHO_DUYET sang HOAT_DONG.</summary>
        public bool DuyetTaiLieu(int id)
        {
            const string sql = "UPDATE dbo.tai_lieu SET trang_thai = N'HOAT_DONG' WHERE id = @Id AND trang_thai = N'CHO_DUYET'";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", id);
                return lenh.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>Đếm tổng số tài liệu theo một trạng thái (HOAT_DONG | CHO_DUYET | NGUNG_HOAT_DONG).</summary>
        public int DemTheoTrangThai(string trangThai)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.tai_lieu WHERE trang_thai = @TrangThai";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@TrangThai", trangThai);
                return Convert.ToInt32(lenh.ExecuteScalar());
            }
        }

        /// <summary>
        /// Danh sách tài liệu cho màn quản lý của thủ thư: trả về MỌI trạng thái
        /// (kể cả chờ duyệt / ngừng hoạt động) để thủ thư theo dõi và biên tập.
        /// </summary>
        public List<TaiLieu> LayDanhSachQuanLy(string tuKhoa, int? theLoaiId, string trangThai, int soLuongToiDa)
        {
            var sql = @"SELECT TOP (@SoLuong) " + CotChon + TuBang + @"
                WHERE (@TuKhoa IS NULL OR tl.ten_tai_lieu LIKE @MauTuKhoa OR tg.ten_tac_gia LIKE @MauTuKhoa)
                  AND (@TheLoaiId IS NULL OR tl.the_loai_id = @TheLoaiId)
                  AND (@TrangThai IS NULL OR tl.trang_thai = @TrangThai)
                ORDER BY tl.id DESC";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                var coTuKhoa = !string.IsNullOrWhiteSpace(tuKhoa);

                KetNoiDuLieu.ThemThamSo(lenh, "@SoLuong", soLuongToiDa > 0 ? soLuongToiDa : int.MaxValue);
                KetNoiDuLieu.ThemThamSo(lenh, "@TuKhoa", coTuKhoa ? tuKhoa.Trim() : null);
                KetNoiDuLieu.ThemThamSo(lenh, "@MauTuKhoa", coTuKhoa ? "%" + tuKhoa.Trim() + "%" : null);
                KetNoiDuLieu.ThemThamSo(lenh, "@TheLoaiId", theLoaiId);
                KetNoiDuLieu.ThemThamSo(lenh, "@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai);

                return DocDanhSach(lenh);
            }
        }

        /// <summary>
        /// Từ chối tài liệu đóng góp: chuyển trạng thái sang NGUNG_HOAT_DONG
        /// và lưu lại lý do để người đóng góp biết.
        /// </summary>
        public bool TuChoiTaiLieu(int id, string lyDo)
        {
            const string sql = @"UPDATE dbo.tai_lieu
                                 SET trang_thai = N'NGUNG_HOAT_DONG', ly_do_tu_choi = @LyDo
                                 WHERE id = @Id AND trang_thai = N'CHO_DUYET'";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", id);
                KetNoiDuLieu.ThemThamSo(lenh, "@LyDo", string.IsNullOrWhiteSpace(lyDo) ? null : lyDo.Trim());
                return lenh.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>Cập nhật thông tin biên tập của tài liệu, bao gồm cả đường dẫn tệp (file_url).</summary>
        public bool CapNhatTaiLieu(TaiLieu taiLieu)
        {
            const string sql = @"UPDATE dbo.tai_lieu
                                 SET ten_tai_lieu = @TenTaiLieu,
                                     mo_ta = @MoTa,
                                     file_url = @FileUrl,
                                     gia_ban = @GiaBan,
                                     the_loai_id = @TheLoaiId,
                                     tac_gia_id = @TacGiaId,
                                     quyen_doc_online = @QuyenDocOnline,
                                     quyen_tai = @QuyenTai,
                                     trang_thai = @TrangThai,
                                     ly_do_tu_choi = CASE WHEN @TrangThai = N'HOAT_DONG' THEN NULL ELSE ly_do_tu_choi END
                                 WHERE id = @Id";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", taiLieu.Id);
                KetNoiDuLieu.ThemThamSo(lenh, "@TenTaiLieu", taiLieu.TenTaiLieu);
                KetNoiDuLieu.ThemThamSo(lenh, "@MoTa", taiLieu.MoTa);
                KetNoiDuLieu.ThemThamSo(lenh, "@FileUrl", taiLieu.FileUrl);
                KetNoiDuLieu.ThemThamSo(lenh, "@GiaBan", taiLieu.GiaBan);
                KetNoiDuLieu.ThemThamSo(lenh, "@TheLoaiId", taiLieu.TheLoaiId);
                KetNoiDuLieu.ThemThamSo(lenh, "@TacGiaId", taiLieu.TacGiaId);
                KetNoiDuLieu.ThemThamSo(lenh, "@QuyenDocOnline", taiLieu.QuyenDocOnline);
                KetNoiDuLieu.ThemThamSo(lenh, "@QuyenTai", taiLieu.QuyenTai);
                KetNoiDuLieu.ThemThamSo(lenh, "@TrangThai", taiLieu.TrangThai);

                return lenh.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Cập nhật riêng cột file_url, dùng khi đổi tên tệp trong Content/Document
        /// mà không muốn đụng tới các thông tin biên tập khác.
        /// </summary>
        public bool CapNhatFileUrl(int id, string fileUrl)
        {
            const string sql = "UPDATE dbo.tai_lieu SET file_url = @FileUrl WHERE id = @Id";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", id);
                KetNoiDuLieu.ThemThamSo(lenh, "@FileUrl", fileUrl);
                return lenh.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Xoá hẳn một tài liệu. Chỉ thành công khi tài liệu chưa phát sinh
        /// giao dịch / quyền tải / lịch sử đọc (nếu vướng khoá ngoại, trả về false
        /// để giao diện hướng dẫn chuyển sang "Ngừng hoạt động").
        /// </summary>
        public bool XoaTaiLieu(int id)
        {
            const string sql = "DELETE FROM dbo.tai_lieu WHERE id = @Id";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", id);
                return lenh.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>Đếm tổng số tài liệu đang hoạt động.</summary>
        public int DemTaiLieuHoatDong()
        {
            const string sql = "SELECT COUNT(1) FROM dbo.tai_lieu WHERE trang_thai = N'HOAT_DONG'";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                return Convert.ToInt32(lenh.ExecuteScalar());
            }
        }

        /// <summary>Đếm tổng số tài liệu trong kho (mọi trạng thái).</summary>
        public int DemTongTaiLieu()
        {
            const string sql = "SELECT COUNT(1) FROM dbo.tai_lieu";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                return Convert.ToInt32(lenh.ExecuteScalar());
            }
        }

        /// <summary>
        /// Thêm mới một tài liệu (người dùng đóng góp).
        /// Id sinh bằng MAX(id) + 1 trong transaction kèm khoá bảng.
        /// </summary>
        public bool ThemTaiLieu(TaiLieu taiLieu)
        {
            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    int idMoi;
                    using (var lenhId = KetNoiDuLieu.TaoLenh(
                        "SELECT ISNULL(MAX(id), 0) + 1 FROM dbo.tai_lieu WITH (TABLOCKX, HOLDLOCK)",
                        ketNoi, giaoDich))
                    {
                        idMoi = Convert.ToInt32(lenhId.ExecuteScalar());
                    }

                    const string sql = @"
                        INSERT INTO dbo.tai_lieu (
                            id, ten_tai_lieu, mo_ta, file_url, gia_ban, 
                            the_loai_id, tac_gia_id, nguoi_dang_id, 
                            quyen_doc_online, quyen_tai, trang_thai
                        ) 
                        VALUES (
                            @Id, @TenTaiLieu, @MoTa, @FileUrl, @GiaBan, 
                            @TheLoaiId, @TacGiaId, @NguoiDangId, 
                            @QuyenDocOnline, @QuyenTai, @TrangThai
                        )";

                    using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi, giaoDich))
                    {
                        KetNoiDuLieu.ThemThamSo(lenh, "@Id", idMoi);
                        KetNoiDuLieu.ThemThamSo(lenh, "@TenTaiLieu", taiLieu.TenTaiLieu);
                        KetNoiDuLieu.ThemThamSo(lenh, "@MoTa", taiLieu.MoTa);
                        KetNoiDuLieu.ThemThamSo(lenh, "@FileUrl", taiLieu.FileUrl);
                        KetNoiDuLieu.ThemThamSo(lenh, "@GiaBan", taiLieu.GiaBan);
                        KetNoiDuLieu.ThemThamSo(lenh, "@TheLoaiId", taiLieu.TheLoaiId);
                        KetNoiDuLieu.ThemThamSo(lenh, "@TacGiaId", taiLieu.TacGiaId);
                        KetNoiDuLieu.ThemThamSo(lenh, "@NguoiDangId", taiLieu.NguoiDangId);
                        KetNoiDuLieu.ThemThamSo(lenh, "@QuyenDocOnline", taiLieu.QuyenDocOnline);
                        KetNoiDuLieu.ThemThamSo(lenh, "@QuyenTai", taiLieu.QuyenTai);
                        KetNoiDuLieu.ThemThamSo(lenh, "@TrangThai", taiLieu.TrangThai);

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

        /// <summary>Danh sách thể loại để đổ vào bộ lọc tìm kiếm.</summary>
        public List<TheLoai> LayDanhSachTheLoai()
        {
            const string sql = "SELECT id, ten_the_loai, mo_ta FROM dbo.the_loai ORDER BY ten_the_loai";

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
                        MoTa = LayChuoi(doc, "mo_ta")
                    });
                }
            }
            return ketQua;
        }

        /// <summary>Danh sách tác giả để đổ vào combobox.</summary>
        public List<TacGia> LayDanhSachTacGia()
        {
            const string sql = "SELECT id, ten_tac_gia, quoc_tich FROM dbo.tac_gia ORDER BY ten_tac_gia";

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
                        QuocTich = LayChuoi(doc, "quoc_tich")
                    });
                }
            }
            return ketQua;
        }

        /// <summary>Lấy danh sách tài liệu do một người dùng đóng góp.</summary>
        public List<TaiLieu> LayTheoNguoiDang(int nguoiDangId)
        {
            var sql = "SELECT " + CotChon + TuBang + " WHERE tl.nguoi_dang_id = @NguoiDangId ORDER BY tl.id DESC";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@NguoiDangId", nguoiDangId);
                return DocDanhSach(lenh);
            }
        }

        private static List<TaiLieu> DocDanhSach(System.Data.SqlClient.SqlCommand lenh)
        {
            var ketQua = new List<TaiLieu>();

            using (var doc = lenh.ExecuteReader())
            {
                while (doc.Read())
                {
                    var taiLieu = new TaiLieu
                    {
                        Id = doc.GetInt32(doc.GetOrdinal("id")),
                        TenTaiLieu = LayChuoi(doc, "ten_tai_lieu"),
                        MoTa = LayChuoi(doc, "mo_ta"),
                        FileUrl = LayChuoi(doc, "file_url"),
                        GiaBan = doc.GetInt32(doc.GetOrdinal("gia_ban")),
                        TheLoaiId = LaySoNguyen(doc, "the_loai_id"),
                        TacGiaId = LaySoNguyen(doc, "tac_gia_id"),
                        NguoiDangId = LaySoNguyen(doc, "nguoi_dang_id"),
                        QuyenDocOnline = doc.GetBoolean(doc.GetOrdinal("quyen_doc_online")),
                        QuyenTai = doc.GetBoolean(doc.GetOrdinal("quyen_tai")),
                        TrangThai = LayChuoi(doc, "trang_thai"),
                        LyDoTuChoi = LayChuoi(doc, "ly_do_tu_choi")
                    };

                    var tenTheLoai = LayChuoi(doc, "ten_the_loai");
                    if (tenTheLoai != null)
                        taiLieu.TheLoai = new TheLoai { Id = taiLieu.TheLoaiId ?? 0, TenTheLoai = tenTheLoai };

                    var tenTacGia = LayChuoi(doc, "ten_tac_gia");
                    if (tenTacGia != null)
                        taiLieu.TacGia = new TacGia { Id = taiLieu.TacGiaId ?? 0, TenTacGia = tenTacGia };

                    ketQua.Add(taiLieu);
                }
            }

            return ketQua;
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
