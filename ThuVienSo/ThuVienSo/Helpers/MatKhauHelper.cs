using System;
using System.Security.Cryptography;

namespace ThuVienSo.Helpers
{
    /// <summary>
    /// Băm và kiểm tra mật khẩu bằng PBKDF2 (Rfc2898DeriveBytes).
    /// Định dạng lưu trong dbo.nguoi_dung.mat_khau: PBKDF2$soVongLap$saltBase64$hashBase64
    /// Có hỗ trợ tương thích ngược với mật khẩu lưu văn bản thuần của dữ liệu mẫu,
    /// và tự động nâng cấp sang dạng băm sau khi đăng nhập thành công.
    /// </summary>
    public static class MatKhauHelper
    {
        private const string TienTo = "PBKDF2";
        private const int SoVongLap = 100000;
        private const int KichThuocSalt = 16;
        private const int KichThuocKhoa = 32;

        /// <summary>Tạo chuỗi băm an toàn cho mật khẩu mới.</summary>
        public static string Bam(string matKhau)
        {
            if (matKhau == null) throw new ArgumentNullException("matKhau");

            var salt = new byte[KichThuocSalt];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            var khoa = TaoKhoa(matKhau, salt, SoVongLap, KichThuocKhoa);

            return string.Join("$",
                TienTo,
                SoVongLap.ToString(),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(khoa));
        }

        /// <summary>
        /// Kiểm tra mật khẩu người dùng nhập.
        /// <paramref name="laMatKhauCuChuaBam"/> = true khi mật khẩu lưu trong CSDL
        /// còn ở dạng văn bản thuần (dữ liệu mẫu) - cần nâng cấp sau khi đăng nhập.
        /// </summary>
        public static bool KiemTra(string matKhauNhap, string matKhauLuu, out bool laMatKhauCuChuaBam)
        {
            laMatKhauCuChuaBam = false;

            if (string.IsNullOrEmpty(matKhauNhap) || string.IsNullOrEmpty(matKhauLuu))
                return false;

            // Tương thích ngược: dữ liệu mẫu đang lưu '123456' dạng văn bản thuần.
            if (!matKhauLuu.StartsWith(TienTo + "$", StringComparison.Ordinal))
            {
                laMatKhauCuChuaBam = true;
                return string.Equals(matKhauNhap, matKhauLuu, StringComparison.Ordinal);
            }

            var phan = matKhauLuu.Split('$');
            if (phan.Length != 4) return false;

            int soVongLap;
            if (!int.TryParse(phan[1], out soVongLap) || soVongLap <= 0) return false;

            byte[] salt, khoa;
            try
            {
                salt = Convert.FromBase64String(phan[2]);
                khoa = Convert.FromBase64String(phan[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            var khoaNhap = TaoKhoa(matKhauNhap, salt, soVongLap, khoa.Length);
            return SoSanhAnToan(khoaNhap, khoa);
        }

        private static byte[] TaoKhoa(string matKhau, byte[] salt, int soVongLap, int kichThuoc)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(matKhau, salt, soVongLap))
            {
                return pbkdf2.GetBytes(kichThuoc);
            }
        }

        /// <summary>So sánh theo thời gian hằng định để tránh tấn công đo thời gian.</summary>
        private static bool SoSanhAnToan(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;

            int khac = 0;
            for (int i = 0; i < a.Length; i++)
            {
                khac |= a[i] ^ b[i];
            }
            return khac == 0;
        }
    }
}
