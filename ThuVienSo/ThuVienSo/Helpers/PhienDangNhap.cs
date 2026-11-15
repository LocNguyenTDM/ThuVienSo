using System.Web;
using System.Web.SessionState;
using ThuVienSo.Models;

namespace ThuVienSo.Helpers
{
    /// <summary>
    /// Quản lý phiên đăng nhập của người dùng trong Session.
    /// Mọi thông tin định danh (id, vai trò) luôn lấy từ đây, không nhận từ trình duyệt.
    /// </summary>
    public static class PhienDangNhap
    {
        private const string KhoaId = "DangNhap_Id";
        private const string KhoaHoTen = "DangNhap_HoTen";
        private const string KhoaEmail = "DangNhap_Email";
        private const string KhoaVaiTro = "DangNhap_VaiTro";

        private static HttpSessionState Phien
        {
            get { return HttpContext.Current.Session; }
        }

        public static void GhiPhien(NguoiDung nguoiDung)
        {
            Phien[KhoaId] = nguoiDung.Id;
            Phien[KhoaHoTen] = nguoiDung.HoTen;
            Phien[KhoaEmail] = nguoiDung.Email;
            Phien[KhoaVaiTro] = nguoiDung.VaiTro;
        }

        public static void XoaPhien()
        {
            Phien.Remove(KhoaId);
            Phien.Remove(KhoaHoTen);
            Phien.Remove(KhoaEmail);
            Phien.Remove(KhoaVaiTro);
        }

        public static bool DaDangNhap
        {
            get { return LayId() != null; }
        }

        public static int? LayId()
        {
            var giaTri = Phien[KhoaId];
            return giaTri == null ? (int?)null : (int)giaTri;
        }

        public static string LayHoTen()
        {
            var giaTri = Phien[KhoaHoTen] as string;
            return string.IsNullOrWhiteSpace(giaTri) ? LayEmail() : giaTri;
        }

        public static string LayEmail()
        {
            return Phien[KhoaEmail] as string;
        }

        public static string LayVaiTro()
        {
            return Phien[KhoaVaiTro] as string;
        }

        public static bool LaNguoiDoc
        {
            get { return LayVaiTro() == HangSoHeThong.VaiTro.NguoiDoc; }
        }

        public static bool LaThuThu
        {
            get { return LayVaiTro() == HangSoHeThong.VaiTro.ThuThu; }
        }

        public static bool LaAdmin
        {
            get { return LayVaiTro() == HangSoHeThong.VaiTro.Admin; }
        }
    }
}
