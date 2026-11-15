using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using ThuVienSo.Data;
using ThuVienSo.Helpers;
using ThuVienSo.Models;
using ThuVienSo.Models.ViewModels;

namespace ThuVienSo.Controllers
{
    /// <summary>
    /// Chức năng dành cho vai trò Quản trị viên: quản lý tài khoản và phân quyền,
    /// theo dõi giao dịch mua tài liệu, thống kê lượt xem/tải - doanh thu
    /// và tra cứu nhật ký hoạt động của hệ thống.
    /// Mọi action đều yêu cầu vai trò ADMIN qua YeuCauAdminAttribute.
    /// </summary>
    [YeuCauAdmin]
    public class AdminController : Controller
    {
        private const int SoNgayMacDinh = 30;
        private const int SoDongToiDa = 500;

        private const string LoiKetNoi =
            "Không thể kết nối cơ sở dữ liệu. Vui lòng kiểm tra lại chuỗi kết nối trong Web.config.";

        private readonly NguoiDungRepository _nguoiDungRepository = new NguoiDungRepository();
        private readonly TaiLieuRepository _taiLieuRepository = new TaiLieuRepository();
        private readonly GiaoDichRepository _giaoDichRepository = new GiaoDichRepository();
        private readonly LichSuTaiLieuRepository _lichSuRepository = new LichSuTaiLieuRepository();
        private readonly NhatKyHoatDongRepository _nhatKyRepository = new NhatKyHoatDongRepository();
        private readonly QuyenTaiLieuRepository _quyenTaiLieuRepository = new QuyenTaiLieuRepository();

        // ============================ TỔNG QUAN ============================

        // GET: Admin
        public ActionResult Index()
        {
            ViewBag.Title = "Bảng điều khiển Quản trị viên";

            var homNay = DateTime.Today;
            var tuNgay = homNay.AddDays(-(SoNgayMacDinh - 1));
            var denNgay = homNay.AddDays(1);

            try
            {
                ViewBag.TongNguoiDung = _nguoiDungRepository.DemTheoVaiTro(null);
                ViewBag.SoNguoiDoc = _nguoiDungRepository.DemTheoVaiTro(HangSoHeThong.VaiTro.NguoiDoc);
                ViewBag.SoThuThu = _nguoiDungRepository.DemTheoVaiTro(HangSoHeThong.VaiTro.ThuThu);
                ViewBag.SoAdmin = _nguoiDungRepository.DemTheoVaiTro(HangSoHeThong.VaiTro.Admin);
                ViewBag.SoBiKhoa = _nguoiDungRepository.DemTheoTrangThai(HangSoHeThong.TrangThaiNguoiDung.BiKhoa);

                ViewBag.TongTaiLieu = _taiLieuRepository.DemTongTaiLieu();
                ViewBag.TaiLieuHoatDong = _taiLieuRepository.DemTaiLieuHoatDong();

                ViewBag.LuotXem = _lichSuRepository.DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Xem, tuNgay, denNgay);
                ViewBag.LuotTai = _lichSuRepository.DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Tai, tuNgay, denNgay);
                ViewBag.SoGiaoDichThanhCong = _giaoDichRepository.DemThanhCong(tuNgay, denNgay);
                ViewBag.DoanhThu = _giaoDichRepository.TongDoanhThu(tuNgay, denNgay);
                ViewBag.GiaoDichGanDay = _giaoDichRepository.LayDanhSachQuanTri(null, null, null, null, 6);
                ViewBag.NhatKyGanDay = _nhatKyRepository.LayGanDay(8);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.TongNguoiDung = 0;
                ViewBag.SoNguoiDoc = 0;
                ViewBag.SoThuThu = 0;
                ViewBag.SoAdmin = 0;
                ViewBag.SoBiKhoa = 0;
                ViewBag.TongTaiLieu = 0;
                ViewBag.TaiLieuHoatDong = 0;
                ViewBag.LuotXem = 0;
                ViewBag.LuotTai = 0;
                ViewBag.SoGiaoDichThanhCong = 0;
                ViewBag.DoanhThu = 0L;
                ViewBag.GiaoDichGanDay = new List<GiaoDich>();
                ViewBag.NhatKyGanDay = new List<NhatKyHoatDong>();
            }

            return View();
        }

        // ============================ QUẢN LÝ NGƯỜI DÙNG & PHÂN QUYỀN ============================

        // GET: Admin/QuanLyNguoiDung
        public ActionResult QuanLyNguoiDung(string tuKhoa, string vaiTro, string trangThai)
        {
            ViewBag.Title = "Quản lý người dùng & phân quyền";

            var vaiTroLoc = ChuanHoaVaiTro(vaiTro);
            var trangThaiLoc = ChuanHoaTrangThaiNguoiDung(trangThai);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.VaiTro = vaiTroLoc;
            ViewBag.TrangThai = trangThaiLoc;

            try
            {
                ViewBag.DanhSachNguoiDung = _nguoiDungRepository.LayDanhSach(tuKhoa, vaiTroLoc, trangThaiLoc, SoDongToiDa);
                ViewBag.TongNguoiDung = _nguoiDungRepository.DemTheoVaiTro(null);
                ViewBag.SoHoatDong = _nguoiDungRepository.DemTheoTrangThai(HangSoHeThong.TrangThaiNguoiDung.HoatDong);
                ViewBag.SoBiKhoa = _nguoiDungRepository.DemTheoTrangThai(HangSoHeThong.TrangThaiNguoiDung.BiKhoa);
                ViewBag.SoQuanTri = _nguoiDungRepository.DemTheoVaiTro(HangSoHeThong.VaiTro.Admin);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.DanhSachNguoiDung = new List<NguoiDung>();
                ViewBag.TongNguoiDung = 0;
                ViewBag.SoHoatDong = 0;
                ViewBag.SoBiKhoa = 0;
                ViewBag.SoQuanTri = 0;
            }

            return View();
        }

        // POST: Admin/CapNhatNguoiDung
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatNguoiDung(CapNhatNguoiDungViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Loi"] = LayLoiModelState();
                return RedirectToAction("QuanLyNguoiDung");
            }

            var vaiTroMoi = ChuanHoaVaiTro(model.VaiTro);
            var trangThaiMoi = ChuanHoaTrangThaiNguoiDung(model.TrangThai);
            if (vaiTroMoi == null || trangThaiMoi == null)
            {
                TempData["Loi"] = "Vai trò hoặc trạng thái không hợp lệ.";
                return RedirectToAction("QuanLyNguoiDung");
            }

            try
            {
                var hienTai = _nguoiDungRepository.LayTheoId(model.Id);
                if (hienTai == null)
                {
                    TempData["Loi"] = "Không tìm thấy tài khoản cần cập nhật.";
                    return RedirectToAction("QuanLyNguoiDung");
                }

                // Kiểm tra quyền hạn trước khi lưu thay đổi.
                var loiQuyen = KiemTraQuyenCapNhat(hienTai, vaiTroMoi, trangThaiMoi);
                if (loiQuyen != null)
                {
                    TempData["Loi"] = loiQuyen;
                    return RedirectToAction("QuanLyNguoiDung");
                }

                var email = model.Email.Trim();
                if (_nguoiDungRepository.KiemTraEmailTonTai(email, model.Id))
                {
                    TempData["Loi"] = "Email này đã được sử dụng bởi tài khoản khác.";
                    return RedirectToAction("QuanLyNguoiDung");
                }

                var capNhat = new NguoiDung
                {
                    Id = hienTai.Id,
                    HoTen = model.HoTen.Trim(),
                    Email = email,
                    SoDienThoai = model.SoDienThoai.Trim(),
                    VaiTro = vaiTroMoi,
                    TrangThai = trangThaiMoi
                };

                if (!_nguoiDungRepository.CapNhatQuanTri(capNhat))
                {
                    TempData["Loi"] = "Không thể cập nhật tài khoản. Vui lòng thử lại.";
                    return RedirectToAction("QuanLyNguoiDung");
                }

                // Nếu quản trị viên tự sửa hồ sơ, làm mới phiên đăng nhập ngay.
                if (hienTai.Id == PhienDangNhap.LayId())
                {
                    PhienDangNhap.GhiPhien(capNhat);
                }

                GhiNhatKy("Cập nhật tài khoản #" + hienTai.Id + " (" + email + "): " +
                          "vai trò " + hienTai.VaiTro + " → " + vaiTroMoi +
                          ", trạng thái " + hienTai.TrangThai + " → " + trangThaiMoi);

                TempData["ThongBao"] = "Đã cập nhật thông tin và quyền hạn của tài khoản.";
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }

            return RedirectToAction("QuanLyNguoiDung");
        }

        // POST: Admin/KhoaTaiKhoan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult KhoaTaiKhoan(int id)
        {
            return DoiTrangThaiTaiKhoan(id, HangSoHeThong.TrangThaiNguoiDung.BiKhoa);
        }

        // POST: Admin/MoKhoaTaiKhoan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MoKhoaTaiKhoan(int id)
        {
            return DoiTrangThaiTaiKhoan(id, HangSoHeThong.TrangThaiNguoiDung.HoatDong);
        }

        private ActionResult DoiTrangThaiTaiKhoan(int id, string trangThaiMoi)
        {
            try
            {
                var hienTai = _nguoiDungRepository.LayTheoId(id);
                if (hienTai == null)
                {
                    TempData["Loi"] = "Không tìm thấy tài khoản cần thay đổi trạng thái.";
                    return RedirectToAction("QuanLyNguoiDung");
                }

                if (hienTai.TrangThai == trangThaiMoi)
                {
                    TempData["ThongBao"] = "Tài khoản đã ở trạng thái mong muốn.";
                    return RedirectToAction("QuanLyNguoiDung");
                }

                if (trangThaiMoi == HangSoHeThong.TrangThaiNguoiDung.BiKhoa)
                {
                    var loiQuyen = KiemTraQuyenKhoa(hienTai);
                    if (loiQuyen != null)
                    {
                        TempData["Loi"] = loiQuyen;
                        return RedirectToAction("QuanLyNguoiDung");
                    }
                }

                if (!_nguoiDungRepository.CapNhatTrangThai(id, trangThaiMoi))
                {
                    TempData["Loi"] = "Không thể cập nhật trạng thái tài khoản.";
                    return RedirectToAction("QuanLyNguoiDung");
                }

                var hanhDong = trangThaiMoi == HangSoHeThong.TrangThaiNguoiDung.BiKhoa
                    ? "Khóa tài khoản #"
                    : "Mở khóa tài khoản #";
                GhiNhatKy(hanhDong + hienTai.Id + " (" + hienTai.Email + ")");

                TempData["ThongBao"] = trangThaiMoi == HangSoHeThong.TrangThaiNguoiDung.BiKhoa
                    ? "Đã khóa tài khoản. Người dùng sẽ bị đăng xuất ở lần truy cập tiếp theo."
                    : "Đã mở khóa tài khoản.";
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }

            return RedirectToAction("QuanLyNguoiDung");
        }

        /// <summary>
        /// Kiểm tra quyền hạn trước khi lưu thay đổi tài khoản:
        /// không cho tự hạ quyền/tự khóa và không được làm mất quản trị viên cuối cùng.
        /// </summary>
        private string KiemTraQuyenCapNhat(NguoiDung hienTai, string vaiTroMoi, string trangThaiMoi)
        {
            if (hienTai.Id == PhienDangNhap.LayId()
                && (vaiTroMoi != HangSoHeThong.VaiTro.Admin
                    || trangThaiMoi != HangSoHeThong.TrangThaiNguoiDung.HoatDong))
            {
                return "Bạn không thể tự hạ quyền hoặc tự khóa tài khoản đang đăng nhập.";
            }

            if (hienTai.LaAdmin && hienTai.DangHoatDong
                && (vaiTroMoi != HangSoHeThong.VaiTro.Admin
                    || trangThaiMoi != HangSoHeThong.TrangThaiNguoiDung.HoatDong)
                && _nguoiDungRepository.DemQuanTriHoatDong() <= 1)
            {
                return "Hệ thống phải còn ít nhất một quản trị viên đang hoạt động.";
            }

            return null;
        }

        private string KiemTraQuyenKhoa(NguoiDung hienTai)
        {
            if (hienTai.Id == PhienDangNhap.LayId())
                return "Bạn không thể khóa tài khoản đang đăng nhập.";

            if (hienTai.LaAdmin && _nguoiDungRepository.DemQuanTriHoatDong() <= 1)
                return "Không thể khóa quản trị viên đang hoạt động cuối cùng của hệ thống.";

            return null;
        }

        // ============================ GIAO DỊCH ============================

        // GET: Admin/GiaoDich
        public ActionResult GiaoDich(DateTime? tuNgay, DateTime? denNgay, string trangThai, string tuKhoa)
        {
            ViewBag.Title = "Giao dịch mua tài liệu";

            var homNay = DateTime.Today;
            var tu = (tuNgay ?? homNay.AddDays(-(SoNgayMacDinh - 1))).Date;
            var den = (denNgay ?? homNay).Date;
            if (tu > den)
            {
                var tam = tu;
                tu = den;
                den = tam;
            }

            var denDocQuyen = den.AddDays(1);
            var trangThaiLoc = ChuanHoaTrangThaiGiaoDich(trangThai);

            ViewBag.TuNgay = tu;
            ViewBag.DenNgay = den;
            ViewBag.TrangThai = trangThaiLoc;
            ViewBag.TuKhoa = tuKhoa;

            try
            {
                ViewBag.DanhSachGiaoDich = _giaoDichRepository
                    .LayDanhSachQuanTri(tu, denDocQuyen, trangThaiLoc, tuKhoa, SoDongToiDa);

                ViewBag.TongGiaoDich = _giaoDichRepository.DemTheoTrangThai(null, tu, denDocQuyen);
                ViewBag.SoThanhCong = _giaoDichRepository
                    .DemTheoTrangThai(HangSoHeThong.TrangThaiGiaoDich.ThanhCong, tu, denDocQuyen);
                ViewBag.SoDangXuLy = _giaoDichRepository
                    .DemTheoTrangThai(HangSoHeThong.TrangThaiGiaoDich.DangXuLy, tu, denDocQuyen);
                ViewBag.SoThatBai = _giaoDichRepository
                    .DemTheoTrangThai(HangSoHeThong.TrangThaiGiaoDich.ThatBai, tu, denDocQuyen);
                ViewBag.DoanhThu = _giaoDichRepository.TongDoanhThu(tu, denDocQuyen);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.DanhSachGiaoDich = new List<GiaoDich>();
                ViewBag.TongGiaoDich = 0;
                ViewBag.SoThanhCong = 0;
                ViewBag.SoDangXuLy = 0;
                ViewBag.SoThatBai = 0;
                ViewBag.DoanhThu = 0L;
            }

            return View();
        }

        // GET: Admin/ChiTietGiaoDich/5
        public ActionResult ChiTietGiaoDich(int id)
        {
            ViewBag.Title = "Chi tiết giao dịch #" + id;

            try
            {
                var giaoDich = _giaoDichRepository.LayChiTietQuanTri(id);
                if (giaoDich == null)
                {
                    TempData["Loi"] = "Không tìm thấy giao dịch cần xem.";
                    return RedirectToAction("GiaoDich");
                }

                QuyenTaiLieu quyenTai = null;
                if (giaoDich.DaThanhCong)
                {
                    quyenTai = _quyenTaiLieuRepository.LayTheoGiaoDich(id);

                    if (quyenTai == null && giaoDich.UserId.HasValue && giaoDich.TaiLieuId.HasValue)
                    {
                        // Giao dịch thành công có thể dùng chung quyền đã cấp trước đó.
                        quyenTai = new QuyenTaiLieu
                        {
                            UserId = giaoDich.UserId.Value,
                            TaiLieuId = giaoDich.TaiLieuId.Value,
                            TrangThai = _quyenTaiLieuRepository.CoQuyenTai(
                                giaoDich.UserId.Value, giaoDich.TaiLieuId.Value)
                                ? HangSoHeThong.TrangThaiQuyenTai.HoatDong
                                : HangSoHeThong.TrangThaiQuyenTai.DaThuHoi
                        };
                    }
                }

                ViewBag.QuyenTai = quyenTai;
                return View(giaoDich);
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
                return RedirectToAction("GiaoDich");
            }
        }

        // ============================ BÁO CÁO THỐNG KÊ ============================

        // GET: Admin/BaoCaoThongKe
        public ActionResult BaoCaoThongKe(DateTime? tuNgay, DateTime? denNgay)
        {
            ViewBag.Title = "Báo cáo thống kê hệ thống";

            var homNay = DateTime.Today;
            var tu = (tuNgay ?? homNay.AddDays(-(SoNgayMacDinh - 1))).Date;
            var den = (denNgay ?? homNay).Date;
            if (tu > den)
            {
                var tam = tu;
                tu = den;
                den = tam;
            }

            var denDocQuyen = den.AddDays(1);

            ViewBag.TuNgay = tu;
            ViewBag.DenNgay = den;

            try
            {
                ViewBag.TongLuotXem = _lichSuRepository
                    .DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Xem, tu, denDocQuyen);
                ViewBag.TongLuotTai = _lichSuRepository
                    .DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Tai, tu, denDocQuyen);
                ViewBag.SoGiaoDichThanhCong = _giaoDichRepository.DemThanhCong(tu, denDocQuyen);
                ViewBag.DoanhThu = _giaoDichRepository.TongDoanhThu(tu, denDocQuyen);
                ViewBag.SoDangXuLy = _giaoDichRepository
                    .DemTheoTrangThai(HangSoHeThong.TrangThaiGiaoDich.DangXuLy, tu, denDocQuyen);
                ViewBag.SoThatBai = _giaoDichRepository
                    .DemTheoTrangThai(HangSoHeThong.TrangThaiGiaoDich.ThatBai, tu, denDocQuyen);

                ViewBag.DanhSachThongKe = TongHopThongKeTaiLieu(tu, denDocQuyen);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.TongLuotXem = 0;
                ViewBag.TongLuotTai = 0;
                ViewBag.SoGiaoDichThanhCong = 0;
                ViewBag.DoanhThu = 0L;
                ViewBag.SoDangXuLy = 0;
                ViewBag.SoThatBai = 0;
                ViewBag.DanhSachThongKe = new List<ThongKeTaiLieu>();
            }

            return View();
        }

        /// <summary>
        /// Gộp lượt xem/tải và doanh thu theo từng tài liệu. Doanh thu chỉ lấy từ
        /// giao dịch THANH_CONG nên giao dịch đang xử lý / thất bại không được tính.
        /// </summary>
        private List<ThongKeTaiLieu> TongHopThongKeTaiLieu(DateTime tuNgay, DateTime denNgay)
        {
            var luuLuong = _lichSuRepository.ThongKeTheoTaiLieu(tuNgay, denNgay, SoDongToiDa);
            var doanhThu = _giaoDichRepository.ThongKeDoanhThuTheoTaiLieu(tuNgay, denNgay, SoDongToiDa);

            var tongHop = new Dictionary<int, ThongKeTaiLieu>();

            foreach (var dong in luuLuong)
            {
                tongHop[dong.TaiLieuId] = dong;
            }

            foreach (var dong in doanhThu)
            {
                ThongKeTaiLieu muc;
                if (!tongHop.TryGetValue(dong.TaiLieuId, out muc))
                {
                    muc = new ThongKeTaiLieu
                    {
                        TaiLieuId = dong.TaiLieuId,
                        TenTaiLieu = dong.TenTaiLieu
                    };
                    tongHop[dong.TaiLieuId] = muc;
                }

                muc.SoGiaoDich = dong.SoGiaoDich;
                muc.DoanhThu = dong.DoanhThu;
                if (string.IsNullOrWhiteSpace(muc.TenTaiLieu))
                    muc.TenTaiLieu = dong.TenTaiLieu;
            }

            return tongHop.Values
                .OrderByDescending(t => t.DoanhThu)
                .ThenByDescending(t => t.TongLuot)
                .ThenByDescending(t => t.TaiLieuId)
                .Take(SoDongToiDa)
                .ToList();
        }

        // ============================ NHẬT KÝ HOẠT ĐỘNG ============================

        // GET: Admin/NhatKyHoatDong
        public ActionResult NhatKyHoatDong(int? userId, DateTime? tuNgay, DateTime? denNgay, string tuKhoa)
        {
            ViewBag.Title = "Nhật ký hoạt động hệ thống";

            var homNay = DateTime.Today;
            var tu = (tuNgay ?? homNay.AddDays(-(SoNgayMacDinh - 1))).Date;
            var den = (denNgay ?? homNay).Date;
            if (tu > den)
            {
                var tam = tu;
                tu = den;
                den = tam;
            }

            var denDocQuyen = den.AddDays(1);
            var nguoiDungLoc = userId.HasValue && userId.Value > 0 ? userId : null;

            ViewBag.TuNgay = tu;
            ViewBag.DenNgay = den;
            ViewBag.UserId = nguoiDungLoc;
            ViewBag.TuKhoa = tuKhoa;

            try
            {
                ViewBag.DanhSachNhatKy = _nhatKyRepository
                    .LayDanhSach(nguoiDungLoc, tu, denDocQuyen, tuKhoa, SoDongToiDa);
                ViewBag.DanhSachNguoiDung = _nguoiDungRepository.LayDanhSach(null, null, null, 1000);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.DanhSachNhatKy = new List<NhatKyHoatDong>();
                ViewBag.DanhSachNguoiDung = new List<NguoiDung>();
            }

            return View();
        }

        // ============================ TIỆN ÍCH ============================

        private void GhiNhatKy(string hanhDong)
        {
            try
            {
                _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(), hanhDong);
            }
            catch (SqlException)
            {
                // Thao tác quản trị đã hoàn tất; lỗi ghi nhật ký không được làm hỏng kết quả.
            }
        }

        private string LayLoiModelState()
        {
            var loi = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

            return string.IsNullOrWhiteSpace(loi)
                ? "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại."
                : loi;
        }

        private static string ChuanHoaVaiTro(string vaiTro)
        {
            if (vaiTro == HangSoHeThong.VaiTro.NguoiDoc
                || vaiTro == HangSoHeThong.VaiTro.ThuThu
                || vaiTro == HangSoHeThong.VaiTro.Admin)
            {
                return vaiTro;
            }

            return null;
        }

        private static string ChuanHoaTrangThaiNguoiDung(string trangThai)
        {
            if (trangThai == HangSoHeThong.TrangThaiNguoiDung.HoatDong
                || trangThai == HangSoHeThong.TrangThaiNguoiDung.BiKhoa)
            {
                return trangThai;
            }

            return null;
        }

        private static string ChuanHoaTrangThaiGiaoDich(string trangThai)
        {
            if (trangThai == HangSoHeThong.TrangThaiGiaoDich.DangXuLy
                || trangThai == HangSoHeThong.TrangThaiGiaoDich.ThanhCong
                || trangThai == HangSoHeThong.TrangThaiGiaoDich.ThatBai)
            {
                return trangThai;
            }

            return null;
        }
    }
}
