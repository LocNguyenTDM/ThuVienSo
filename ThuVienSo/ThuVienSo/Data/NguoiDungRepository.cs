using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ThuVienSo.Models;

namespace ThuVienSo.Data
{
    /// <summary>
    /// Truy cập dữ liệu bảng dbo.nguoi_dung bằng ADO.NET.
    /// Lưu ý: bảng nguoi_dung không có cột IDENTITY nên id mới
    /// phải sinh thủ công bằng MAX(id) + 1 trong transaction kèm khoá bảng.
    /// </summary>
    public class NguoiDungRepository
    {
        private const string CotChon =
            "id, ho_ten, email, mat_khau, so_dien_thoai, vai_tro, trang_thai";

        /// <summary>Tìm người dùng theo email (dùng khi đăng nhập).</summary>
        public NguoiDung LayTheoEmail(string email)
        {
            const string sql = "SELECT " + CotChon + " FROM dbo.nguoi_dung WHERE email = @Email";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Email", email);

                using (var doc = lenh.ExecuteReader())
                {
                    return doc.Read() ? DocDuLieu(doc) : null;
                }
            }
        }

        /// <summary>Tìm người dùng theo id.</summary>
        public NguoiDung LayTheoId(int id)
        {
            const string sql = "SELECT " + CotChon + " FROM dbo.nguoi_dung WHERE id = @Id";

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

        /// <summary>Kiểm tra email đã tồn tại trong hệ thống hay chưa.</summary>
        public bool KiemTraEmailTonTai(string email)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.nguoi_dung WHERE email = @Email";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Email", email);
                return Convert.ToInt32(lenh.ExecuteScalar()) > 0;
            }
        }

        /// <summary>
        /// Thêm tài khoản người đọc mới. Id sinh bằng MAX(id) + 1 trong transaction
        /// có khoá bảng để tránh trùng id khi nhiều người đăng ký cùng lúc.
        /// Trả về id vừa tạo.
        /// </summary>
        public int ThemNguoiDoc(NguoiDung nguoiDung)
        {
            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    int idMoi;
                    using (var lenhId = KetNoiDuLieu.TaoLenh(
                        "SELECT ISNULL(MAX(id), 0) + 1 FROM dbo.nguoi_dung WITH (TABLOCKX, HOLDLOCK)",
                        ketNoi, giaoDich))
                    {
                        idMoi = Convert.ToInt32(lenhId.ExecuteScalar());
                    }

                    const string sql = @"INSERT INTO dbo.nguoi_dung
                            (id, ho_ten, email, mat_khau, so_dien_thoai, vai_tro, trang_thai)
                        VALUES
                            (@Id, @HoTen, @Email, @MatKhau, @SoDienThoai, @VaiTro, @TrangThai)";

                    using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi, giaoDich))
                    {
                        KetNoiDuLieu.ThemThamSo(lenh, "@Id", idMoi);
                        KetNoiDuLieu.ThemThamSo(lenh, "@HoTen", nguoiDung.HoTen);
                        KetNoiDuLieu.ThemThamSo(lenh, "@Email", nguoiDung.Email);
                        KetNoiDuLieu.ThemThamSo(lenh, "@MatKhau", nguoiDung.MatKhau);
                        KetNoiDuLieu.ThemThamSo(lenh, "@SoDienThoai", nguoiDung.SoDienThoai);
                        KetNoiDuLieu.ThemThamSo(lenh, "@VaiTro", nguoiDung.VaiTro);
                        KetNoiDuLieu.ThemThamSo(lenh, "@TrangThai", nguoiDung.TrangThai);

                        lenh.ExecuteNonQuery();
                    }

                    giaoDich.Commit();
                    return idMoi;
                }
                catch
                {
                    giaoDich.Rollback();
                    throw;
                }
            }
        }

        /// <summary>Cập nhật họ tên và số điện thoại của chính người dùng.</summary>
        public bool CapNhatThongTin(int id, string hoTen, string soDienThoai)
        {
            const string sql = @"UPDATE dbo.nguoi_dung
                                 SET ho_ten = @HoTen, so_dien_thoai = @SoDienThoai
                                 WHERE id = @Id";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", id);
                KetNoiDuLieu.ThemThamSo(lenh, "@HoTen", hoTen);
                KetNoiDuLieu.ThemThamSo(lenh, "@SoDienThoai", soDienThoai);

                return lenh.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>Cập nhật mật khẩu (đã băm) của người dùng.</summary>
        public bool CapNhatMatKhau(int id, string matKhauDaBam)
        {
            const string sql = "UPDATE dbo.nguoi_dung SET mat_khau = @MatKhau WHERE id = @Id";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", id);
                KetNoiDuLieu.ThemThamSo(lenh, "@MatKhau", matKhauDaBam);

                return lenh.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Danh sách tài khoản cho màn quản trị, lọc theo từ khóa (họ tên/email/số điện thoại),
        /// vai trò và trạng thái. Mọi tham số đều được truyền an toàn qua SqlParameter.
        /// </summary>
        public List<NguoiDung> LayDanhSach(string tuKhoa, string vaiTro, string trangThai, int soLuongToiDa)
        {
            const string sql = @"
                SELECT TOP (@SoLuong) " + CotChon + @"
                FROM dbo.nguoi_dung
                WHERE (@TuKhoa IS NULL
                       OR ho_ten LIKE @MauTuKhoa
                       OR email LIKE @MauTuKhoa
                       OR so_dien_thoai LIKE @MauTuKhoa)
                  AND (@VaiTro IS NULL OR vai_tro = @VaiTro)
                  AND (@TrangThai IS NULL OR trang_thai = @TrangThai)
                ORDER BY id ASC";

            var ketQua = new List<NguoiDung>();

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                var coTuKhoa = !string.IsNullOrWhiteSpace(tuKhoa);

                KetNoiDuLieu.ThemThamSo(lenh, "@SoLuong", soLuongToiDa > 0 ? soLuongToiDa : int.MaxValue);
                KetNoiDuLieu.ThemThamSo(lenh, "@TuKhoa", coTuKhoa ? tuKhoa.Trim() : null);
                KetNoiDuLieu.ThemThamSo(lenh, "@MauTuKhoa", coTuKhoa ? "%" + tuKhoa.Trim() + "%" : null);
                KetNoiDuLieu.ThemThamSo(lenh, "@VaiTro", string.IsNullOrWhiteSpace(vaiTro) ? null : vaiTro);
                KetNoiDuLieu.ThemThamSo(lenh, "@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai);

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

        /// <summary>
        /// Kiểm tra email đã thuộc về tài khoản khác hay chưa.
        /// Truyền boQuaId khi cập nhật để bỏ qua chính tài khoản đang sửa.
        /// </summary>
        public bool KiemTraEmailTonTai(string email, int? boQuaId)
        {
            const string sql = @"SELECT COUNT(1) FROM dbo.nguoi_dung
                                 WHERE email = @Email AND (@BoQuaId IS NULL OR id <> @BoQuaId)";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Email", email);
                KetNoiDuLieu.ThemThamSo(lenh, "@BoQuaId", boQuaId);
                return Convert.ToInt32(lenh.ExecuteScalar()) > 0;
            }
        }

        /// <summary>Cập nhật thông tin, vai trò và trạng thái của tài khoản bởi quản trị viên.</summary>
        public bool CapNhatQuanTri(NguoiDung nguoiDung)
        {
            const string sql = @"
                UPDATE dbo.nguoi_dung
                SET ho_ten = @HoTen,
                    email = @Email,
                    so_dien_thoai = @SoDienThoai,
                    vai_tro = @VaiTro,
                    trang_thai = @TrangThai
                WHERE id = @Id
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.nguoi_dung
                      WHERE email = @Email AND id <> @Id
                  )";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", nguoiDung.Id);
                KetNoiDuLieu.ThemThamSo(lenh, "@HoTen", nguoiDung.HoTen);
                KetNoiDuLieu.ThemThamSo(lenh, "@Email", nguoiDung.Email);
                KetNoiDuLieu.ThemThamSo(lenh, "@SoDienThoai", nguoiDung.SoDienThoai);
                KetNoiDuLieu.ThemThamSo(lenh, "@VaiTro", nguoiDung.VaiTro);
                KetNoiDuLieu.ThemThamSo(lenh, "@TrangThai", nguoiDung.TrangThai);

                return lenh.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>Khóa hoặc mở khóa tài khoản.</summary>
        public bool CapNhatTrangThai(int id, string trangThai)
        {
            const string sql = "UPDATE dbo.nguoi_dung SET trang_thai = @TrangThai WHERE id = @Id";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@Id", id);
                KetNoiDuLieu.ThemThamSo(lenh, "@TrangThai", trangThai);
                return lenh.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>Đếm số tài khoản theo vai trò; truyền null để đếm tất cả.</summary>
        public int DemTheoVaiTro(string vaiTro)
        {
            const string sql = @"SELECT COUNT(1) FROM dbo.nguoi_dung
                                 WHERE (@VaiTro IS NULL OR vai_tro = @VaiTro)";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@VaiTro", string.IsNullOrWhiteSpace(vaiTro) ? null : vaiTro);
                return Convert.ToInt32(lenh.ExecuteScalar());
            }
        }

        /// <summary>Đếm số tài khoản theo trạng thái (HOAT_DONG | BI_KHOA).</summary>
        public int DemTheoTrangThai(string trangThai)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.nguoi_dung WHERE trang_thai = @TrangThai";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                KetNoiDuLieu.ThemThamSo(lenh, "@TrangThai", trangThai);
                return Convert.ToInt32(lenh.ExecuteScalar());
            }
        }

        /// <summary>
        /// Đếm số quản trị viên đang hoạt động, dùng để chặn thao tác làm hệ thống
        /// mất hết quản trị viên (ví dụ hạ quyền hoặc khóa quản trị viên cuối cùng).
        /// </summary>
        public int DemQuanTriHoatDong()
        {
            const string sql = @"SELECT COUNT(1) FROM dbo.nguoi_dung
                                 WHERE vai_tro = N'ADMIN' AND trang_thai = N'HOAT_DONG'";

            using (var ketNoi = KetNoiDuLieu.TaoKetNoi())
            using (var lenh = KetNoiDuLieu.TaoLenh(sql, ketNoi))
            {
                return Convert.ToInt32(lenh.ExecuteScalar());
            }
        }

        private static NguoiDung DocDuLieu(IDataRecord dong)
        {
            return new NguoiDung
            {
                Id = dong.GetInt32(dong.GetOrdinal("id")),
                HoTen = LayChuoi(dong, "ho_ten"),
                Email = LayChuoi(dong, "email"),
                MatKhau = LayChuoi(dong, "mat_khau"),
                SoDienThoai = LayChuoi(dong, "so_dien_thoai"),
                VaiTro = LayChuoi(dong, "vai_tro"),
                TrangThai = LayChuoi(dong, "trang_thai")
            };
        }

        private static string LayChuoi(IDataRecord dong, string tenCot)
        {
            var viTri = dong.GetOrdinal(tenCot);
            return dong.IsDBNull(viTri) ? null : dong.GetString(viTri);
        }
    }
}
