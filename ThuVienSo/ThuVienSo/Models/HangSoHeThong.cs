namespace ThuVienSo.Models
{
    /// <summary>
    /// Hằng số nghiệp vụ dùng chung, khớp đúng giá trị lưu trong SQL Server.
    /// Tránh gõ sai chuỗi trạng thái/vai trò ở tầng xử lý.
    /// </summary>
    public static class HangSoHeThong
    {
        public static class VaiTro
        {
            public const string NguoiDoc = "NGUOI_DOC";
            public const string ThuThu = "THU_THU";
            public const string Admin = "ADMIN";
        }

        public static class TrangThaiNguoiDung
        {
            public const string HoatDong = "HOAT_DONG";
            public const string BiKhoa = "BI_KHOA";
        }

        public static class TrangThaiTaiLieu
        {
            public const string HoatDong = "HOAT_DONG";
            public const string NgungHoatDong = "NGUNG_HOAT_DONG";
            public const string ChoDuyet = "CHO_DUYET";
        }

        public static class TrangThaiGiaoDich
        {
            public const string DangXuLy = "DANG_XU_LY";
            public const string ThanhCong = "THANH_CONG";
            public const string ThatBai = "THAT_BAI";
        }

        public static class TrangThaiQuyenTai
        {
            public const string HoatDong = "HOAT_DONG";
            public const string DaThuHoi = "DA_THU_HOI";
        }

        public static class HanhDongTaiLieu
        {
            public const string Xem = "XEM";
            public const string Tai = "TAI";
        }
    }
}
