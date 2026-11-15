using System;
using System.Data.SqlClient;
using System.Web.Mvc;
using ThuVienSo.Data;
using ThuVienSo.Helpers;
using ThuVienSo.Models;
using ThuVienSo.Models.ViewModels;

namespace ThuVienSo.Controllers
{
    /// <summary>
    /// Xử lý chức năng tài khoản người đọc: đăng ký, đăng nhập, đăng xuất.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly NguoiDungRepository _nguoiDungRepository = new NguoiDungRepository();
        private readonly NhatKyHoatDongRepository _nhatKyRepository = new NhatKyHoatDongRepository();

        // GET: Account/DangNhap
        [HttpGet]
        public ActionResult DangNhap(string returnUrl)
        {
            if (PhienDangNhap.DaDangNhap)
                return ChuyenHuongTheoVaiTro();

            ViewBag.ReturnUrl = returnUrl;
            return View(new DangNhapViewModel());
        }

        // POST: Account/DangNhap
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(DangNhapViewModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var nguoiDung = _nguoiDungRepository.LayTheoEmail(model.Email);
                if (nguoiDung == null)
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                    return View(model);
                }

                if (nguoiDung.TrangThai != HangSoHeThong.TrangThaiNguoiDung.HoatDong)
                {
                    ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                    return View(model);
                }

                bool laMatKhauCuChuaBam;
                if (!MatKhauHelper.KiemTra(model.MatKhau, nguoiDung.MatKhau, out laMatKhauCuChuaBam))
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                    return View(model);
                }

                // Nâng cấp mật khẩu văn bản thuần của dữ liệu mẫu sang dạng băm.
                if (laMatKhauCuChuaBam)
                {
                    _nguoiDungRepository.CapNhatMatKhau(nguoiDung.Id, MatKhauHelper.Bam(model.MatKhau));
                }

                PhienDangNhap.GhiPhien(nguoiDung);
                _nhatKyRepository.GhiNhatKy(nguoiDung.Id, "Đăng nhập hệ thống");
            }
            catch (SqlException)
            {
                ModelState.AddModelError("", "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return ChuyenHuongTheoVaiTro();
        }

        /// <summary>Điều hướng người dùng về đúng cổng chức năng theo vai trò đang đăng nhập.</summary>
        private ActionResult ChuyenHuongTheoVaiTro()
        {
            if (PhienDangNhap.LaAdmin)
                return RedirectToAction("Index", "Admin");

            if (PhienDangNhap.LaThuThu)
                return RedirectToAction("Index", "ThuThu");

            return RedirectToAction("Index", "NguoiDoc");
        }

        // GET: Account/DangKy
        [HttpGet]
        public ActionResult DangKy()
        {
            if (PhienDangNhap.DaDangNhap)
                return ChuyenHuongTheoVaiTro();

            return View(new DangKyViewModel());
        }

        // POST: Account/DangKy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(DangKyViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var email = model.Email.Trim();

                if (_nguoiDungRepository.KiemTraEmailTonTai(email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng. Vui lòng chọn email khác.");
                    return View(model);
                }

                var nguoiDungMoi = new NguoiDung
                {
                    HoTen = model.HoTen.Trim(),
                    Email = email,
                    MatKhau = MatKhauHelper.Bam(model.MatKhau),
                    SoDienThoai = model.SoDienThoai.Trim(),
                    VaiTro = HangSoHeThong.VaiTro.NguoiDoc,
                    TrangThai = HangSoHeThong.TrangThaiNguoiDung.HoatDong
                };

                var idMoi = _nguoiDungRepository.ThemNguoiDoc(nguoiDungMoi);
                _nhatKyRepository.GhiNhatKy(idMoi, "Đăng ký tài khoản người đọc");
            }
            catch (SqlException)
            {
                ModelState.AddModelError("", "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.");
                return View(model);
            }

            TempData["ThongBaoThanhCong"] = "Đăng ký thành công. Vui lòng đăng nhập bằng tài khoản vừa tạo.";
            return RedirectToAction("DangNhap");
        }

        // POST: Account/DangXuat
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangXuat()
        {
            var idNguoiDung = PhienDangNhap.LayId();

            PhienDangNhap.XoaPhien();

            if (idNguoiDung.HasValue)
            {
                try
                {
                    _nhatKyRepository.GhiNhatKy(idNguoiDung.Value, "Đăng xuất khỏi hệ thống");
                }
                catch (SqlException)
                {
                    // Không chặn đăng xuất nếu ghi nhật ký thất bại.
                }
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/HoSoCaNhan
        [HttpGet]
        [YeuCauDangNhap]
        public ActionResult HoSoCaNhan()
        {
            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return RedirectToAction("DangNhap");

            try
            {
                var nguoiDung = _nguoiDungRepository.LayTheoId(idNguoiDung.Value);
                if (nguoiDung == null)
                    return RedirectToAction("DangXuat");

                ViewBag.Title = "Thông tin cá nhân";
                ViewBag.VaiTro = nguoiDung.VaiTro;
                ViewBag.TrangThai = nguoiDung.TrangThai;

                return View(new CapNhatHoSoViewModel
                {
                    Email = nguoiDung.Email,
                    HoTen = nguoiDung.HoTen,
                    SoDienThoai = nguoiDung.SoDienThoai
                });
            }
            catch (SqlException)
            {
                ModelState.AddModelError("", "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.");
                return View(new CapNhatHoSoViewModel());
            }
        }

        // POST: Account/HoSoCaNhan
        [HttpPost]
        [ValidateAntiForgeryToken]
        [YeuCauDangNhap]
        public ActionResult HoSoCaNhan(CapNhatHoSoViewModel model)
        {
            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return RedirectToAction("DangNhap");

            // Email là tài khoản đăng nhập nên không cho phép thay đổi từ biểu mẫu.
            ModelState.Remove("Email");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var nguoiDung = _nguoiDungRepository.LayTheoId(idNguoiDung.Value);
                if (nguoiDung == null)
                    return RedirectToAction("DangXuat");

                _nguoiDungRepository.CapNhatThongTin(
                    idNguoiDung.Value,
                    model.HoTen.Trim(),
                    model.SoDienThoai.Trim());

                // Cập nhật lại tên hiển thị trong phiên đăng nhập.
                nguoiDung.HoTen = model.HoTen.Trim();
                nguoiDung.SoDienThoai = model.SoDienThoai.Trim();
                PhienDangNhap.GhiPhien(nguoiDung);

                _nhatKyRepository.GhiNhatKy(idNguoiDung.Value, "Cập nhật thông tin cá nhân");

                TempData["ThongBaoThanhCong"] = "Cập nhật thông tin cá nhân thành công.";
                return RedirectToAction("HoSoCaNhan");
            }
            catch (SqlException)
            {
                ModelState.AddModelError("", "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.");
                return View(model);
            }
        }

        // GET: Account/DoiMatKhau
        [HttpGet]
        [YeuCauDangNhap]
        public ActionResult DoiMatKhau()
        {
            ViewBag.Title = "Đổi mật khẩu";
            return View(new DoiMatKhauViewModel());
        }

        // POST: Account/DoiMatKhau
        [HttpPost]
        [ValidateAntiForgeryToken]
        [YeuCauDangNhap]
        public ActionResult DoiMatKhau(DoiMatKhauViewModel model)
        {
            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return RedirectToAction("DangNhap");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var nguoiDung = _nguoiDungRepository.LayTheoId(idNguoiDung.Value);
                if (nguoiDung == null)
                    return RedirectToAction("DangXuat");

                bool laMatKhauCuChuaBam;
                if (!MatKhauHelper.KiemTra(model.MatKhauCu, nguoiDung.MatKhau, out laMatKhauCuChuaBam))
                {
                    ModelState.AddModelError("MatKhauCu", "Mật khẩu hiện tại không đúng.");
                    return View(model);
                }

                _nguoiDungRepository.CapNhatMatKhau(idNguoiDung.Value, MatKhauHelper.Bam(model.MatKhauMoi));
                _nhatKyRepository.GhiNhatKy(idNguoiDung.Value, "Đổi mật khẩu tài khoản");

                TempData["ThongBaoThanhCong"] = "Đổi mật khẩu thành công.";
                return RedirectToAction("HoSoCaNhan");
            }
            catch (SqlException)
            {
                ModelState.AddModelError("", "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.");
                return View(model);
            }
        }
    }
}
