using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using ThuVienSo.Data;
using ThuVienSo.Helpers;
using ThuVienSo.Models;

namespace ThuVienSo.Controllers
{
    /// <summary>
    /// Chức năng dành cho vai trò Thủ thư: duyệt / từ chối tài liệu đóng góp,
    /// biên tập danh mục tài liệu số và theo dõi lưu lượng đọc - tải.
    /// Mọi action đều yêu cầu vai trò THU_THU qua YeuCauThuThuAttribute.
    /// </summary>
    [YeuCauThuThu]
    public class ThuThuController : Controller
    {
        /// <summary>Số ngày mặc định của báo cáo lưu lượng gần nhất.</summary>
        private const int SoNgayBaoCao = 30;

        /// <summary>Số dòng nhật ký tối đa hiển thị trong báo cáo.</summary>
        private const int SoDongNhatKyBaoCao = 50;

        /// <summary>Số tài liệu tối đa trong bảng xếp hạng lưu lượng.</summary>
        private const int SoTaiLieuTop = 10;

        /// <summary>Số dòng nhật ký tối đa khi xuất file thống kê.</summary>
        private const int SoDongXuatNhatKy = 5000;

        /// <summary>Dung lượng tối đa của tệp tài liệu thủ thư tải lên (20MB).</summary>
        private const int DungLuongTepToiDa = 20 * 1024 * 1024;

        /// <summary>Độ dài tối đa của tên tác giả / tên thể loại theo thiết kế CSDL.</summary>
        private const int DoDaiTenDanhMuc = 100;

        /// <summary>Độ dài tối đa của mô tả thể loại theo thiết kế CSDL.</summary>
        private const int DoDaiMoTaTheLoai = 255;

        /// <summary>Độ dài tối đa của quốc tịch tác giả theo thiết kế CSDL.</summary>
        private const int DoDaiQuocTich = 50;

        private const string LoiKetNoi =
            "Không thể kết nối cơ sở dữ liệu. Vui lòng kiểm tra lại chuỗi kết nối trong Web.config.";

        private const string ThuMucTep = TepTaiLieuHelper.ThuMucTep;

        private readonly TaiLieuRepository _taiLieuRepository = new TaiLieuRepository();
        private readonly LichSuTaiLieuRepository _lichSuRepository = new LichSuTaiLieuRepository();
        private readonly GiaoDichRepository _giaoDichRepository = new GiaoDichRepository();
        private readonly NhatKyHoatDongRepository _nhatKyRepository = new NhatKyHoatDongRepository();
        private readonly DanhMucRepository _danhMucRepository = new DanhMucRepository();

        // ============================ TỔNG QUAN ============================

        // GET: ThuThu
        public ActionResult Index()
        {
            ViewBag.Title = "Bảng điều khiển Thủ thư";

            var homNay = DateTime.Today;
            var tuNgay = homNay.AddDays(-(SoNgayBaoCao - 1));

            try
            {
                ViewBag.SoLuongChoDuyet = _taiLieuRepository.DemTheoTrangThai(HangSoHeThong.TrangThaiTaiLieu.ChoDuyet);
                ViewBag.TongTaiLieu = _taiLieuRepository.DemTheoTrangThai(HangSoHeThong.TrangThaiTaiLieu.HoatDong);
                ViewBag.TaiLieuNgungHoatDong = _taiLieuRepository.DemTheoTrangThai(HangSoHeThong.TrangThaiTaiLieu.NgungHoatDong);

                ViewBag.LuotXemHomNay = _lichSuRepository.DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Xem, homNay, homNay.AddDays(1));
                ViewBag.LuotTaiHomNay = _lichSuRepository.DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Tai, homNay, homNay.AddDays(1));

                ViewBag.LuotXem30Ngay = _lichSuRepository.DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Xem, tuNgay, homNay.AddDays(1));
                ViewBag.LuotTai30Ngay = _lichSuRepository.DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Tai, tuNgay, homNay.AddDays(1));

                ViewBag.DoanhThu30Ngay = _giaoDichRepository.TongDoanhThu(tuNgay, homNay.AddDays(1));
                ViewBag.SoGiaoDich30Ngay = _giaoDichRepository.DemThanhCong(tuNgay, homNay.AddDays(1));

                ViewBag.TopTaiLieu = _lichSuRepository.ThongKeTheoTaiLieu(tuNgay, homNay.AddDays(1), 5);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.SoLuongChoDuyet = 0;
                ViewBag.TongTaiLieu = 0;
                ViewBag.TaiLieuNgungHoatDong = 0;
                ViewBag.LuotXemHomNay = 0;
                ViewBag.LuotTaiHomNay = 0;
                ViewBag.LuotXem30Ngay = 0;
                ViewBag.LuotTai30Ngay = 0;
                ViewBag.DoanhThu30Ngay = 0L;
                ViewBag.SoGiaoDich30Ngay = 0;
                ViewBag.TopTaiLieu = new List<ThongKeTaiLieu>();
            }

            return View();
        }

        // ============================ DUYỆT TÀI LIỆU ============================

        // GET: ThuThu/DuyetTaiLieu
        public ActionResult DuyetTaiLieu()
        {
            ViewBag.Title = "Duyệt tài liệu mới đóng góp";
            try
            {
                return View(_taiLieuRepository.LayDanhSachChoDuyet());
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
                return View(new List<TaiLieu>());
            }
        }

        // POST: ThuThu/PheDuyet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PheDuyet(int id)
        {
            try
            {
                // Tệp do người đọc đóng góp có thể đang mang tên ngẫu nhiên;
                // khi duyệt thì đổi tên theo tên tài liệu để thủ thư dễ quản lý.
                // Bước này là best-effort, không chặn việc phê duyệt nếu tệp không tồn tại.
                try
                {
                    var taiLieu = _taiLieuRepository.LayTheoId(id);
                    if (taiLieu != null
                        && DongBoTenTepTheoTaiLieu(taiLieu, out var fileUrlMoi, out _)
                        && !string.Equals(fileUrlMoi, taiLieu.FileUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        _taiLieuRepository.CapNhatFileUrl(id, fileUrlMoi);
                    }
                }
                catch (SqlException)
                {
                    // Bỏ qua: việc đổi tên tệp không được cản trở phê duyệt.
                }

                if (_taiLieuRepository.DuyetTaiLieu(id))
                {
                    _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(), "Phê duyệt tài liệu #" + id);
                    TempData["ThongBao"] = "Đã phê duyệt tài liệu thành công.";
                }
                else
                {
                    TempData["Loi"] = "Không thể phê duyệt: tài liệu không còn ở trạng thái chờ duyệt.";
                }
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }
            return RedirectToAction("DuyetTaiLieu");
        }

        // POST: ThuThu/TuChoi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TuChoi(int id, string lyDo)
        {
            if (string.IsNullOrWhiteSpace(lyDo))
            {
                TempData["Loi"] = "Vui lòng nhập lý do từ chối để thông báo cho người đóng góp.";
                return RedirectToAction("DuyetTaiLieu");
            }

            try
            {
                if (_taiLieuRepository.TuChoiTaiLieu(id, lyDo))
                {
                    _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(),
                        "Từ chối tài liệu #" + id + ": " + lyDo.Trim());
                    TempData["ThongBao"] = "Đã từ chối tài liệu và lưu lại lý do.";
                }
                else
                {
                    TempData["Loi"] = "Không thể từ chối: tài liệu không còn ở trạng thái chờ duyệt.";
                }
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }
            return RedirectToAction("DuyetTaiLieu");
        }

        // ============================ QUẢN LÝ DANH MỤC ============================

        // GET: ThuThu/QuanLySach
        public ActionResult QuanLySach(string tuKhoa, int? theLoaiId, string trangThai)
        {
            ViewBag.Title = "Quản lý Danh mục Sách & Tài liệu";
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TheLoaiId = theLoaiId;
            ViewBag.TrangThai = trangThai;

            try
            {
                ViewBag.DanhSachTheLoai = _taiLieuRepository.LayDanhSachTheLoai();
                ViewBag.DanhSachTacGia = _taiLieuRepository.LayDanhSachTacGia();
                ViewBag.DanhSachTaiLieu = _taiLieuRepository.LayDanhSachQuanLy(tuKhoa, theLoaiId, trangThai, 300);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.DanhSachTheLoai = new List<TheLoai>();
                ViewBag.DanhSachTacGia = new List<TacGia>();
                ViewBag.DanhSachTaiLieu = new List<TaiLieu>();
            }

            return View();
        }

        // POST: ThuThu/ThemSach
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemSach(string tenTaiLieu, string moTa, int? giaBan, int? theLoaiId, int? tacGiaId,
            bool quyenDocOnline, bool quyenTai, string duongDanTep, HttpPostedFileBase tepTaiLieu)
        {
            if (string.IsNullOrWhiteSpace(tenTaiLieu))
            {
                TempData["Loi"] = "Vui lòng nhập tên tài liệu.";
                return RedirectToAction("QuanLySach");
            }

            string fileUrl;
            if (!ThuLayFileUrl(tepTaiLieu, duongDanTep, tenTaiLieu, out fileUrl, out var loiTep))
            {
                TempData["Loi"] = loiTep;
                return RedirectToAction("QuanLySach");
            }

            var taiLieu = new TaiLieu
            {
                TenTaiLieu = tenTaiLieu.Trim(),
                MoTa = moTa,
                FileUrl = fileUrl,
                GiaBan = giaBan.GetValueOrDefault(),
                TheLoaiId = theLoaiId,
                TacGiaId = tacGiaId,
                NguoiDangId = PhienDangNhap.LayId(),
                QuyenDocOnline = quyenDocOnline,
                QuyenTai = quyenTai,
                TrangThai = HangSoHeThong.TrangThaiTaiLieu.HoatDong
            };

            try
            {
                if (_taiLieuRepository.ThemTaiLieu(taiLieu))
                {
                    _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(), "Thêm tài liệu: " + taiLieu.TenTaiLieu);
                    TempData["ThongBao"] = "Đã thêm tài liệu mới vào kho.";
                }
                else
                {
                    TempData["Loi"] = "Không thể thêm tài liệu. Vui lòng thử lại.";
                }
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }

            return RedirectToAction("QuanLySach");
        }

        // POST: ThuThu/SuaSach
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaSach(int id, string tenTaiLieu, string moTa, int? giaBan, int? theLoaiId, int? tacGiaId,
            bool quyenDocOnline, bool quyenTai, string trangThai, HttpPostedFileBase tepTaiLieu)
        {
            TaiLieu taiLieuHienTai;
            try
            {
                taiLieuHienTai = _taiLieuRepository.LayTheoId(id);
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
                return RedirectToAction("QuanLySach");
            }

            if (taiLieuHienTai == null)
            {
                TempData["Loi"] = "Không tìm thấy tài liệu cần cập nhật.";
                return RedirectToAction("QuanLySach");
            }

            if (string.IsNullOrWhiteSpace(tenTaiLieu))
            {
                TempData["Loi"] = "Tên tài liệu không được để trống.";
                return RedirectToAction("QuanLySach");
            }

            // Cho phép thủ thư tải lên tệp mới để thay thế tệp tài liệu.
            // Tệp được lưu vào Content/Document, đặt tên theo tên tài liệu để dễ quản lý.
            // Không chọn tệp thì giữ nguyên tệp hiện tại.
            if (tepTaiLieu != null && tepTaiLieu.ContentLength > 0)
            {
                if (tepTaiLieu.ContentLength > DungLuongTepToiDa)
                {
                    TempData["Loi"] = "Tệp vượt quá dung lượng cho phép (tối đa 20MB).";
                    return RedirectToAction("QuanLySach");
                }

                if (!LuuTepTheoTaiLieu(tepTaiLieu, tenTaiLieu, out var fileUrlMoi, out var loiTep))
                {
                    TempData["Loi"] = loiTep;
                    return RedirectToAction("QuanLySach");
                }

                taiLieuHienTai.FileUrl = fileUrlMoi;
            }

            taiLieuHienTai.TenTaiLieu = tenTaiLieu.Trim();
            taiLieuHienTai.MoTa = moTa;
            taiLieuHienTai.GiaBan = giaBan.GetValueOrDefault();
            taiLieuHienTai.TheLoaiId = theLoaiId;
            taiLieuHienTai.TacGiaId = tacGiaId;
            taiLieuHienTai.QuyenDocOnline = quyenDocOnline;
            taiLieuHienTai.QuyenTai = quyenTai;
            taiLieuHienTai.TrangThai = ChuanHoaTrangThai(trangThai);

            try
            {
                if (_taiLieuRepository.CapNhatTaiLieu(taiLieuHienTai))
                {
                    _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(), "Cập nhật tài liệu #" + id);
                    TempData["ThongBao"] = "Đã cập nhật thông tin tài liệu.";
                }
                else
                {
                    TempData["Loi"] = "Không thể cập nhật tài liệu.";
                }
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }

            return RedirectToAction("QuanLySach");
        }

        // POST: ThuThu/XoaSach
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaSach(int id)
        {
            try
            {
                if (_taiLieuRepository.XoaTaiLieu(id))
                {
                    _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(), "Xoá tài liệu #" + id);
                    TempData["ThongBao"] = "Đã xoá tài liệu khỏi kho.";
                }
                else
                {
                    TempData["Loi"] = "Không tìm thấy tài liệu để xoá.";
                }
            }
            catch (SqlException)
            {
                // Vướng khoá ngoại (đã có giao dịch / quyền tải / lịch sử đọc).
                TempData["Loi"] = "Không thể xoá vì tài liệu đã phát sinh giao dịch hoặc lịch sử đọc. " +
                                  "Hãy chuyển sang trạng thái \"Ngừng hoạt động\" thay vì xoá.";
            }

            return RedirectToAction("QuanLySach");
        }

        // ============================ QUẢN LÝ TÁC GIẢ & THỂ LOẠI ============================

        // GET: ThuThu/QuanLyDanhMuc
        public ActionResult QuanLyDanhMuc()
        {
            ViewBag.Title = "Quản lý tác giả & thể loại";
            try
            {
                ViewBag.DanhSachTheLoai = _danhMucRepository.LayDanhSachTheLoai();
                ViewBag.DanhSachTacGia = _danhMucRepository.LayDanhSachTacGia();
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
                ViewBag.DanhSachTheLoai = new List<TheLoai>();
                ViewBag.DanhSachTacGia = new List<TacGia>();
            }

            return View();
        }

        // POST: ThuThu/ThemTheLoai
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemTheLoai(string tenTheLoai, string moTa)
        {
            tenTheLoai = CatChuoi(tenTheLoai, DoDaiTenDanhMuc);
            moTa = CatChuoi(moTa, DoDaiMoTaTheLoai);

            if (tenTheLoai.Length == 0)
            {
                TempData["Loi"] = "Vui lòng nhập tên thể loại.";
                return RedirectToAction("QuanLyDanhMuc");
            }

            try
            {
                if (_danhMucRepository.TenTheLoaiDaTonTai(tenTheLoai, null))
                {
                    TempData["Loi"] = "Thể loại \"" + tenTheLoai + "\" đã tồn tại.";
                    return RedirectToAction("QuanLyDanhMuc");
                }

                var theLoai = new TheLoai { TenTheLoai = tenTheLoai, MoTa = moTa.Length == 0 ? null : moTa };
                if (_danhMucRepository.ThemTheLoai(theLoai))
                {
                    _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(), "Thêm thể loại: " + tenTheLoai);
                    TempData["ThongBao"] = "Đã thêm thể loại \"" + tenTheLoai + "\".";
                }
                else
                {
                    TempData["Loi"] = "Không thể thêm thể loại. Vui lòng thử lại.";
                }
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }

            return RedirectToAction("QuanLyDanhMuc");
        }

        // POST: ThuThu/SuaTheLoai
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaTheLoai(int id, string tenTheLoai, string moTa)
        {
            tenTheLoai = CatChuoi(tenTheLoai, DoDaiTenDanhMuc);
            moTa = CatChuoi(moTa, DoDaiMoTaTheLoai);

            if (tenTheLoai.Length == 0)
            {
                TempData["Loi"] = "Vui lòng nhập tên thể loại.";
                return RedirectToAction("QuanLyDanhMuc");
            }

            try
            {
                if (_danhMucRepository.TenTheLoaiDaTonTai(tenTheLoai, id))
                {
                    TempData["Loi"] = "Thể loại \"" + tenTheLoai + "\" đã tồn tại.";
                    return RedirectToAction("QuanLyDanhMuc");
                }

                var theLoai = new TheLoai { Id = id, TenTheLoai = tenTheLoai, MoTa = moTa.Length == 0 ? null : moTa };
                if (_danhMucRepository.CapNhatTheLoai(theLoai))
                {
                    _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(), "Cập nhật thể loại #" + id + ": " + tenTheLoai);
                    TempData["ThongBao"] = "Đã cập nhật thể loại.";
                }
                else
                {
                    TempData["Loi"] = "Không tìm thấy thể loại để cập nhật.";
                }
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }

            return RedirectToAction("QuanLyDanhMuc");
        }

        // POST: ThuThu/XoaTheLoai
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaTheLoai(int id)
        {
            try
            {
                if (_danhMucRepository.XoaTheLoai(id))
                {
                    _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(), "Xoá thể loại #" + id);
                    TempData["ThongBao"] = "Đã xoá thể loại.";
                }
                else
                {
                    TempData["Loi"] = "Không tìm thấy thể loại để xoá.";
                }
            }
            catch (SqlException)
            {
                TempData["Loi"] = "Không thể xoá vì vẫn còn tài liệu thuộc thể loại này. " +
                                  "Hãy chuyển các tài liệu đó sang thể loại khác trước.";
            }

            return RedirectToAction("QuanLyDanhMuc");
        }

        // POST: ThuThu/ThemTacGia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemTacGia(string tenTacGia, string quocTich)
        {
            tenTacGia = CatChuoi(tenTacGia, DoDaiTenDanhMuc);
            quocTich = CatChuoi(quocTich, DoDaiQuocTich);

            if (tenTacGia.Length == 0)
            {
                TempData["Loi"] = "Vui lòng nhập tên tác giả.";
                return RedirectToAction("QuanLyDanhMuc");
            }

            try
            {
                if (_danhMucRepository.TenTacGiaDaTonTai(tenTacGia, null))
                {
                    TempData["Loi"] = "Tác giả \"" + tenTacGia + "\" đã tồn tại.";
                    return RedirectToAction("QuanLyDanhMuc");
                }

                var tacGia = new TacGia { TenTacGia = tenTacGia, QuocTich = quocTich.Length == 0 ? null : quocTich };
                if (_danhMucRepository.ThemTacGia(tacGia))
                {
                    _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(), "Thêm tác giả: " + tenTacGia);
                    TempData["ThongBao"] = "Đã thêm tác giả \"" + tenTacGia + "\".";
                }
                else
                {
                    TempData["Loi"] = "Không thể thêm tác giả. Vui lòng thử lại.";
                }
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }

            return RedirectToAction("QuanLyDanhMuc");
        }

        // POST: ThuThu/SuaTacGia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaTacGia(int id, string tenTacGia, string quocTich)
        {
            tenTacGia = CatChuoi(tenTacGia, DoDaiTenDanhMuc);
            quocTich = CatChuoi(quocTich, DoDaiQuocTich);

            if (tenTacGia.Length == 0)
            {
                TempData["Loi"] = "Vui lòng nhập tên tác giả.";
                return RedirectToAction("QuanLyDanhMuc");
            }

            try
            {
                if (_danhMucRepository.TenTacGiaDaTonTai(tenTacGia, id))
                {
                    TempData["Loi"] = "Tác giả \"" + tenTacGia + "\" đã tồn tại.";
                    return RedirectToAction("QuanLyDanhMuc");
                }

                var tacGia = new TacGia { Id = id, TenTacGia = tenTacGia, QuocTich = quocTich.Length == 0 ? null : quocTich };
                if (_danhMucRepository.CapNhatTacGia(tacGia))
                {
                    _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(), "Cập nhật tác giả #" + id + ": " + tenTacGia);
                    TempData["ThongBao"] = "Đã cập nhật tác giả.";
                }
                else
                {
                    TempData["Loi"] = "Không tìm thấy tác giả để cập nhật.";
                }
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }

            return RedirectToAction("QuanLyDanhMuc");
        }

        // POST: ThuThu/XoaTacGia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaTacGia(int id)
        {
            try
            {
                if (_danhMucRepository.XoaTacGia(id))
                {
                    _nhatKyRepository.GhiNhatKy(PhienDangNhap.LayId(), "Xoá tác giả #" + id);
                    TempData["ThongBao"] = "Đã xoá tác giả.";
                }
                else
                {
                    TempData["Loi"] = "Không tìm thấy tác giả để xoá.";
                }
            }
            catch (SqlException)
            {
                TempData["Loi"] = "Không thể xoá vì vẫn còn tài liệu của tác giả này. " +
                                  "Hãy chuyển các tài liệu đó sang tác giả khác trước.";
            }

            return RedirectToAction("QuanLyDanhMuc");
        }

        /// <summary>Bỏ khoảng trắng thừa và cắt bớt chuỗi cho vừa độ dài cột CSDL.</summary>
        private static string CatChuoi(string giaTri, int doDaiToiDa)
        {
            giaTri = (giaTri ?? string.Empty).Trim();
            return giaTri.Length > doDaiToiDa ? giaTri.Substring(0, doDaiToiDa).Trim() : giaTri;
        }

        // ============================ BÁO CÁO LƯU LƯỢNG ============================

        // GET: ThuThu/BaoCaoLuuLuong
        public ActionResult BaoCaoLuuLuong(DateTime? tuNgay, DateTime? denNgay, string hanhDong)
        {
            ViewBag.Title = "Báo cáo lưu lượng đọc & tải tài liệu";

            DateTime tu, den;
            TinhKhoangBaoCao(tuNgay, denNgay, out tu, out den);

            ViewBag.TuNgay = tu;
            ViewBag.DenNgay = den.AddDays(-1);
            ViewBag.HanhDong = string.IsNullOrWhiteSpace(hanhDong) ? null : hanhDong;

            try
            {
                ViewBag.TongLuotXem = _lichSuRepository.DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Xem, tu, den);
                ViewBag.TongLuotTai = _lichSuRepository.DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Tai, tu, den);
                ViewBag.SoGiaoDich = _giaoDichRepository.DemThanhCong(tu, den);
                ViewBag.DoanhThu = _giaoDichRepository.TongDoanhThu(tu, den);
                ViewBag.TopTaiLieu = _lichSuRepository.ThongKeTheoTaiLieu(tu, den, SoTaiLieuTop);
                ViewBag.NhatKy = _lichSuRepository.LayTheoKhoangThoiGian(tu, den, ViewBag.HanhDong as string, SoDongNhatKyBaoCao);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.TongLuotXem = 0;
                ViewBag.TongLuotTai = 0;
                ViewBag.SoGiaoDich = 0;
                ViewBag.DoanhThu = 0L;
                ViewBag.TopTaiLieu = new List<ThongKeTaiLieu>();
                ViewBag.NhatKy = new List<LichSuTaiLieu>();
            }

            return View();
        }

        // GET: ThuThu/XuatBaoCaoLuuLuong
        /// <summary>
        /// Xuất báo cáo lưu lượng đọc/tải ra sổ làm việc Excel chuẩn XLSX.
        /// Giữ nguyên bộ lọc khoảng thời gian và hành động đang xem trên báo cáo.
        /// </summary>
        public ActionResult XuatBaoCaoLuuLuong(DateTime? tuNgay, DateTime? denNgay, string hanhDong)
        {
            DateTime tu, den;
            TinhKhoangBaoCao(tuNgay, denNgay, out tu, out den);

            var hanhDongLoc = string.IsNullOrWhiteSpace(hanhDong) ? null : hanhDong;

            List<ThongKeTaiLieu> topTaiLieu;
            List<LichSuTaiLieu> nhatKy;
            int tongLuotXem, tongLuotTai, soGiaoDich;
            long doanhThu;

            try
            {
                tongLuotXem = _lichSuRepository.DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Xem, tu, den);
                tongLuotTai = _lichSuRepository.DemTheoHanhDong(HangSoHeThong.HanhDongTaiLieu.Tai, tu, den);
                soGiaoDich = _giaoDichRepository.DemThanhCong(tu, den);
                doanhThu = _giaoDichRepository.TongDoanhThu(tu, den);
                topTaiLieu = _lichSuRepository.ThongKeTheoTaiLieu(tu, den, SoTaiLieuTop);
                nhatKy = _lichSuRepository.LayTheoKhoangThoiGian(tu, den, hanhDongLoc, SoDongXuatNhatKy);
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
                return RedirectToAction("BaoCaoLuuLuong", new { tuNgay, denNgay, hanhDong });
            }

            var cacDong = new List<object[]>
            {
                new object[] { "BÁO CÁO LƯU LƯỢNG ĐỌC & TẢI TÀI LIỆU" },
                new object[] { "Từ ngày", tu.ToString("dd/MM/yyyy") },
                new object[] { "Đến ngày", den.AddDays(-1).ToString("dd/MM/yyyy") },
                new object[] { "Hành động", MoTaHanhDong(hanhDongLoc) },
                new object[] { "Người xuất", PhienDangNhap.LayHoTen() },
                new object[] { "Thời gian xuất", DateTime.Now.ToString("dd/MM/yyyy HH:mm") },
                new object[0],
                new object[] { "TỔNG HỢP" },
                new object[] { "Chỉ số", "Giá trị" },
                new object[] { "Tổng lượt đọc", tongLuotXem },
                new object[] { "Tổng lượt tải", tongLuotTai },
                new object[] { "Giao dịch thành công", soGiaoDich },
                new object[] { "Doanh thu (đ)", doanhThu },
                new object[0],
                new object[] { "TÀI LIỆU ĐƯỢC ĐỌC/TẢI NHIỀU NHẤT" },
                new object[] { "STT", "Tên tài liệu", "Lượt đọc", "Lượt tải", "Tổng lượt" }
            };

            var thuTu = 0;
            foreach (var item in topTaiLieu)
            {
                thuTu++;
                cacDong.Add(new object[] { thuTu, item.TenTaiLieu, item.LuotXem, item.LuotTai, item.TongLuot });
            }

            cacDong.Add(new object[0]);
            cacDong.Add(new object[] { "NHẬT KÝ ĐỌC/TẢI CHI TIẾT" });
            cacDong.Add(new object[] { "Thời gian", "Độc giả", "Email", "Tài liệu", "Thể loại", "Hành động" });
            foreach (var item in nhatKy)
            {
                cacDong.Add(new object[]
                {
                    item.ThoiGian.ToString("dd/MM/yyyy HH:mm"),
                    item.NguoiDung != null && !string.IsNullOrEmpty(item.NguoiDung.HoTen)
                        ? item.NguoiDung.HoTen
                        : "Người dùng #" + item.UserId,
                    item.NguoiDung != null ? item.NguoiDung.Email : null,
                    item.TaiLieu != null ? item.TaiLieu.TenTaiLieu : "Tài liệu #" + item.TaiLieuId,
                    item.TaiLieu != null && item.TaiLieu.TheLoai != null ? item.TaiLieu.TheLoai.TenTheLoai : null,
                    item.HanhDong == HangSoHeThong.HanhDongTaiLieu.Tai ? "Tải" : "Đọc"
                });
            }

            var tenTep = "bao-cao-luu-luong-" + tu.ToString("yyyyMMdd") + "-" + den.AddDays(-1).ToString("yyyyMMdd") + ".xlsx";
            return File(TaoTepXlsx(cacDong),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", tenTep);
        }

        // ============================ TIỆN ÍCH ============================

        /// <summary>
        /// Chuẩn hoá khoảng thời gian báo cáo: "den" là cận trên mở (đã +1 ngày)
        /// để dùng trực tiếp trong truy vấn thoi_gian >= tu AND thoi_gian &lt; den.
        /// </summary>
        private void TinhKhoangBaoCao(DateTime? tuNgay, DateTime? denNgay, out DateTime tu, out DateTime den)
        {
            var homNay = DateTime.Today;
            tu = (tuNgay ?? homNay.AddDays(-(SoNgayBaoCao - 1))).Date;
            den = (denNgay ?? homNay).Date.AddDays(1);

            if (tu >= den)
                tu = den.AddDays(-1);
        }

        /// <summary>Mô tả bộ lọc hành động dùng trong file xuất.</summary>
        private static string MoTaHanhDong(string hanhDong)
        {
            if (hanhDong == HangSoHeThong.HanhDongTaiLieu.Tai)
                return "Chỉ lượt tải (TAI)";

            if (hanhDong == HangSoHeThong.HanhDongTaiLieu.Xem)
                return "Chỉ lượt đọc (XEM)";

            return "Tất cả hành động";
        }

        private static byte[] TaoTepXlsx(IList<object[]> cacDong)
        {
            using (var boNho = new MemoryStream())
            {
                using (var goi = new ZipArchive(boNho, ZipArchiveMode.Create, true))
                {
                    ThemTepZip(goi, "[Content_Types].xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                        "</Types>");
                    ThemTepZip(goi, "_rels/.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                        "</Relationships>");
                    ThemTepZip(goi, "xl/workbook.xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                        "<sheets><sheet name=\"Báo cáo\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                    ThemTepZip(goi, "xl/_rels/workbook.xml.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                        "</Relationships>");
                    ThemTrangTinhZip(goi, cacDong);
                }

                return boNho.ToArray();
            }
        }

        private static void ThemTepZip(ZipArchive goi, string tenTep, string noiDung)
        {
            var tep = goi.CreateEntry(tenTep, CompressionLevel.Optimal);
            using (var luong = tep.Open())
            {
                var duLieu = new UTF8Encoding(false).GetBytes(noiDung);
                luong.Write(duLieu, 0, duLieu.Length);
            }
        }

        private static void ThemTrangTinhZip(ZipArchive goi, IList<object[]> cacDong)
        {
            const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var tep = goi.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
            var caiDat = new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false };

            using (var luong = tep.Open())
            using (var xml = XmlWriter.Create(luong, caiDat))
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("worksheet", ns);
                xml.WriteStartElement("cols", ns);
                var doRong = new[] { 24, 32, 30, 48, 28, 20 };
                for (var i = 0; i < doRong.Length; i++)
                {
                    xml.WriteStartElement("col", ns);
                    xml.WriteAttributeString("min", (i + 1).ToString());
                    xml.WriteAttributeString("max", (i + 1).ToString());
                    xml.WriteAttributeString("width", doRong[i].ToString());
                    xml.WriteAttributeString("customWidth", "1");
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();

                xml.WriteStartElement("sheetData", ns);
                for (var hang = 0; hang < cacDong.Count; hang++)
                {
                    xml.WriteStartElement("row", ns);
                    xml.WriteAttributeString("r", (hang + 1).ToString());

                    var giaTriHang = cacDong[hang] ?? new object[0];
                    for (var cot = 0; cot < giaTriHang.Length; cot++)
                    {
                        if (giaTriHang[cot] == null)
                            continue;

                        var diaChi = TenCotExcel(cot + 1) + (hang + 1);
                        xml.WriteStartElement("c", ns);
                        xml.WriteAttributeString("r", diaChi);

                        if (LaSo(giaTriHang[cot]))
                        {
                            xml.WriteAttributeString("t", "n");
                            xml.WriteElementString("v", ns,
                                Convert.ToString(giaTriHang[cot], System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            xml.WriteAttributeString("t", "inlineStr");
                            xml.WriteStartElement("is", ns);
                            xml.WriteStartElement("t", ns);
                            xml.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                            xml.WriteString(Convert.ToString(giaTriHang[cot]) ?? string.Empty);
                            xml.WriteEndElement();
                            xml.WriteEndElement();
                        }

                        xml.WriteEndElement();
                    }

                    xml.WriteEndElement();
                }
                xml.WriteEndElement();

                xml.WriteStartElement("mergeCells", ns);
                xml.WriteAttributeString("count", "1");
                xml.WriteStartElement("mergeCell", ns);
                xml.WriteAttributeString("ref", "A1:F1");
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }

        private static bool LaSo(object giaTri)
        {
            return giaTri is byte || giaTri is short || giaTri is int || giaTri is long
                || giaTri is float || giaTri is double || giaTri is decimal;
        }

        private static string TenCotExcel(int cot)
        {
            var ten = string.Empty;
            while (cot > 0)
            {
                cot--;
                ten = (char)('A' + cot % 26) + ten;
                cot /= 26;
            }

            return ten;
        }

        private bool ThuLayFileUrl(HttpPostedFileBase tepTaiLieu, string duongDanTep, string tenTaiLieu,
            out string fileUrl, out string loi)
        {
            fileUrl = null;
            loi = null;

            if (tepTaiLieu != null && tepTaiLieu.ContentLength > 0)
            {
                // Tệp tải lên được đặt tên theo tên tài liệu để dễ quản lý trong Content/Document.
                return LuuTepTheoTaiLieu(tepTaiLieu, tenTaiLieu, out fileUrl, out loi);
            }

            if (!string.IsNullOrWhiteSpace(duongDanTep))
            {
                return ChuanHoaDuongDanTep(duongDanTep, out fileUrl, out loi);
            }

            loi = "Vui lòng tải lên tệp tài liệu hoặc nhập đường dẫn tệp đã có trong thư mục lưu trữ.";
            return false;
        }

        /// <summary>
        /// Chuẩn hoá đường dẫn tệp do người dùng nhập về dạng file_url trong CSDL
        /// (ví dụ Content/Document/ten-tep.pdf) và kiểm tra tệp có thật trong thư mục hay không.
        /// </summary>
        private bool ChuanHoaDuongDanTep(string duongDanTep, out string fileUrl, out string loi)
        {
            fileUrl = null;
            loi = null;

            if (string.IsNullOrWhiteSpace(duongDanTep))
            {
                loi = "Đường dẫn tệp không được để trống.";
                return false;
            }

            var chuanHoa = TepTaiLieuHelper.TaoFileUrl(duongDanTep);

            if (!TepTaiLieuHelper.TonTai(chuanHoa))
            {
                loi = "Không tìm thấy tệp \"" + chuanHoa + "\" trong thư mục Content/Document.";
                return false;
            }

            fileUrl = chuanHoa;
            return true;
        }

        /// <summary>Các phần mở rộng tệp tài liệu được phép tải lên.</summary>
        private static bool LaDinhDangTepChoPhep(string phanMoRong)
        {
            return phanMoRong == ".pdf" || phanMoRong == ".doc"
                || phanMoRong == ".docx" || phanMoRong == ".epub";
        }

        /// <summary>
        /// Lưu tệp tài liệu thủ thư tải lên vào Content/Document, đặt tên theo tên tài liệu
        /// để dễ nhận biết và quản lý. Nếu tệp cùng tên đã tồn tại thì ghi đè nội dung cũ.
        /// Trả về giá trị file_url để lưu vào CSDL.
        /// </summary>
        private bool LuuTepTheoTaiLieu(HttpPostedFileBase tepTaiLieu, string tenTaiLieu,
            out string fileUrl, out string loi)
        {
            fileUrl = null;
            loi = null;

            var phanMoRong = Path.GetExtension(tepTaiLieu.FileName).ToLowerInvariant();
            if (!LaDinhDangTepChoPhep(phanMoRong))
            {
                loi = "Chỉ chấp nhận tệp PDF, Word (.doc, .docx) hoặc EPUB.";
                return false;
            }

            var tenTep = TepTaiLieuHelper.TaoTenTepTheoTaiLieu(tenTaiLieu, phanMoRong);
            var duongDanVatLy = Server.MapPath(ThuMucTep + tenTep);

            try
            {
                tepTaiLieu.SaveAs(duongDanVatLy);
            }
            catch (Exception ex)
            {
                loi = "Không thể lưu tệp tài liệu: " + ex.Message;
                return false;
            }

            fileUrl = TepTaiLieuHelper.TaoFileUrl(tenTep);
            return true;
        }

        /// <summary>
        /// Đổi tên tệp của tài liệu trong Content/Document thành tên theo tên tài liệu.
        /// Dùng khi phê duyệt để chuẩn hoá tệp do người đọc đóng góp (trước đây đặt tên ngẫu nhiên).
        /// Trả về false nếu không tìm thấy tệp (khi đó bỏ qua, không chặn phê duyệt).
        /// </summary>
        private bool DongBoTenTepTheoTaiLieu(TaiLieu taiLieu, out string fileUrlMoi, out string loi)
        {
            fileUrlMoi = null;
            loi = null;

            var duongDanCu = TepTaiLieuHelper.LayDuongDanVatLy(taiLieu.FileUrl);
            if (string.IsNullOrEmpty(duongDanCu) || !System.IO.File.Exists(duongDanCu))
            {
                loi = "Không tìm thấy tệp tài liệu để cập nhật tên.";
                return false;
            }

            var phanMoRong = Path.GetExtension(duongDanCu);
            var tenTepMongMuon = TepTaiLieuHelper.TaoTenTepTheoTaiLieu(taiLieu.TenTaiLieu, phanMoRong);
            var duongDanMoi = Server.MapPath(ThuMucTep + tenTepMongMuon);

            if (!string.Equals(duongDanCu, duongDanMoi, StringComparison.OrdinalIgnoreCase))
            {
                if (System.IO.File.Exists(duongDanMoi))
                    System.IO.File.Delete(duongDanMoi);

                System.IO.File.Move(duongDanCu, duongDanMoi);
            }

            fileUrlMoi = TepTaiLieuHelper.TaoFileUrl(tenTepMongMuon);
            return true;
        }

        /// <summary>Chỉ nhận các trạng thái hợp lệ của tài liệu; mặc định là HOAT_DONG.</summary>
        private static string ChuanHoaTrangThai(string trangThai)
        {
            if (trangThai == HangSoHeThong.TrangThaiTaiLieu.ChoDuyet
                || trangThai == HangSoHeThong.TrangThaiTaiLieu.NgungHoatDong)
                return trangThai;

            return HangSoHeThong.TrangThaiTaiLieu.HoatDong;
        }
    }
}
