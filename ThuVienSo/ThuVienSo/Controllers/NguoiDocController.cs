using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ThuVienSo.Data;
using ThuVienSo.Helpers;
using ThuVienSo.Models;

namespace ThuVienSo.Controllers
{
    /// <summary>
    /// Chức năng dành cho người đọc (vai trò NGUOI_DOC):
    /// tìm kiếm, xem chi tiết, đọc trực tuyến, tải về, mua quyền tải,
    /// tủ sách cá nhân và lịch sử giao dịch / lịch sử đọc.
    /// Mọi kiểm tra quyền đều được thực hiện ở phía máy chủ dựa trên Session
    /// và các hàm / procedure của CSDL QuanLyThuVien.
    /// </summary>
    public class NguoiDocController : Controller
    {
        private const int SoTaiLieuHienThi = 60;
        private const int SoDongLichSu = 50;

        /// <summary>Dung lượng tối đa của tệp tài liệu người đọc đóng góp (20MB).</summary>
        private const int DungLuongTepToiDa = 20 * 1024 * 1024;

        private const string LoiKetNoi =
            "Không thể kết nối cơ sở dữ liệu. Vui lòng kiểm tra lại chuỗi kết nối trong Web.config.";

        private readonly TaiLieuRepository _taiLieuRepository = new TaiLieuRepository();
        private readonly QuyenTaiLieuRepository _quyenTaiLieuRepository = new QuyenTaiLieuRepository();
        private readonly GiaoDichRepository _giaoDichRepository = new GiaoDichRepository();
        private readonly LichSuTaiLieuRepository _lichSuRepository = new LichSuTaiLieuRepository();
        private readonly NguoiDungRepository _nguoiDungRepository = new NguoiDungRepository();
        private readonly NhatKyHoatDongRepository _nhatKyRepository = new NhatKyHoatDongRepository();

        // ============================ TỦ SÁCH CÁ NHÂN ============================

        // GET: /NguoiDoc
        [YeuCauDangNhap]
        public ActionResult Index()
        {
            ViewBag.Title = "Tủ sách cá nhân";

            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return RedirectToAction("DangNhap", "Account");

            try
            {
                ViewBag.NguoiDung = _nguoiDungRepository.LayTheoId(idNguoiDung.Value);
                ViewBag.DanhSachQuyenTai = _quyenTaiLieuRepository.LayTheoNguoiDung(idNguoiDung.Value);
                ViewBag.SoQuyenTai = _quyenTaiLieuRepository.DemQuyenHieuLuc(idNguoiDung.Value);
                ViewBag.SoDaXem = _lichSuRepository.DemTaiLieuDaXem(idNguoiDung.Value);
                ViewBag.DangDocDo = _lichSuRepository.LayGanNhat(idNguoiDung.Value, HangSoHeThong.HanhDongTaiLieu.Xem);
                ViewBag.LichSuGanDay = _lichSuRepository.LayTheoNguoiDung(idNguoiDung.Value, 6);
                ViewBag.SoGiaoDichThanhCong = _giaoDichRepository
                    .LayTheoNguoiDung(idNguoiDung.Value)
                    .Count(g => g.DaThanhCong);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.DanhSachQuyenTai = new List<QuyenTaiLieu>();
                ViewBag.LichSuGanDay = new List<LichSuTaiLieu>();
            }

            return View();
        }

        // ============================ TÌM KIẾM & DANH SÁCH ============================

        // GET: /NguoiDoc/TimKiemSach
        // Tìm kiếm tài liệu theo tên, tác giả, thể loại hoặc từ khóa.
        public ActionResult TimKiemSach(string tuKhoa, int? theLoaiId, string dinhDang, string phamVi)
        {
            ViewBag.Title = "Tra cứu & Tìm kiếm tài liệu";

            var phamViHopLe = TaiLieuRepository.ChuanHoaPhamVi(phamVi);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TheLoaiId = theLoaiId;
            ViewBag.DinhDang = dinhDang;
            ViewBag.PhamVi = phamViHopLe;
            ViewBag.CoDieuKienLoc = !string.IsNullOrWhiteSpace(tuKhoa)
                                    || theLoaiId.HasValue
                                    || !string.IsNullOrWhiteSpace(dinhDang);

            try
            {
                ViewBag.DanhSachTheLoai = _taiLieuRepository.LayDanhSachTheLoai();
                ViewBag.DanhSachTaiLieu = _taiLieuRepository
                    .LayDanhSach(tuKhoa, theLoaiId, dinhDang, phamViHopLe, SoTaiLieuHienThi, null);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.DanhSachTheLoai = new List<TheLoai>();
                ViewBag.DanhSachTaiLieu = new List<TaiLieu>();
            }

            return View();
        }

        // GET: /NguoiDoc/MienPhi
        public ActionResult MienPhi(string tuKhoa, int? theLoaiId, string dinhDang)
        {
            ViewBag.Title = "Tài liệu miễn phí";
            ViewBag.IsMienPhiPage = true;

            try
            {
                ViewBag.DanhSachTheLoai = _taiLieuRepository.LayDanhSachTheLoai();
                ViewBag.DanhSachTaiLieu = _taiLieuRepository.LayDanhSach(tuKhoa, theLoaiId, dinhDang, null, SoTaiLieuHienThi, true);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.DanhSachTheLoai = new List<TheLoai>();
                ViewBag.DanhSachTaiLieu = new List<TaiLieu>();
            }

            return View();
        }

        // GET: /NguoiDoc/TraPhi
        public ActionResult TraPhi(string tuKhoa, int? theLoaiId, string dinhDang)
        {
            ViewBag.Title = "Tài liệu trả phí";
            ViewBag.IsTraPhiPage = true;

            try
            {
                ViewBag.DanhSachTheLoai = _taiLieuRepository.LayDanhSachTheLoai();
                ViewBag.DanhSachTaiLieu = _taiLieuRepository.LayDanhSach(tuKhoa, theLoaiId, dinhDang, null, SoTaiLieuHienThi, false);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.DanhSachTheLoai = new List<TheLoai>();
                ViewBag.DanhSachTaiLieu = new List<TaiLieu>();
            }

            return View();
        }

        // POST: /NguoiDoc/HoiDapAI
        // Chatbox AI: nhận mô tả tự nhiên của người đọc, gọi mô hình ngôn ngữ
        // (dịch vụ tương thích OpenAI, xem TroLyTaiLieuAiHelper) để phân tích và chọn
        // ra tài liệu phù hợp trong danh mục thật của thư viện, rồi trả về JSON cho
        // widget chat ở góc màn hình.
        [HttpPost]
        public JsonResult HoiDapAI(string moTa)
        {
            if (string.IsNullOrWhiteSpace(moTa))
            {
                return Json(new
                {
                    thanhCong = false,
                    traLoi = "Bạn hãy mô tả tài liệu mình cần tìm nhé (chủ đề, lĩnh vực, trình độ...).",
                    taiLieu = new object[0]
                });
            }

            List<TaiLieu> danhMuc;
            try
            {
                danhMuc = _taiLieuRepository.LayDanhSach(null, null, null, null, 200, null);
            }
            catch (SqlException)
            {
                return Json(new
                {
                    thanhCong = false,
                    traLoi = LoiKetNoi,
                    taiLieu = new object[0]
                });
            }

            var ketQua = TroLyTaiLieuAiHelper.HoiDap(moTa, danhMuc);

            if (!ketQua.ThanhCong)
            {
                return Json(new
                {
                    thanhCong = false,
                    traLoi = "Xin lỗi, trợ lý đang gặp sự cố: " + ketQua.Loi,
                    taiLieu = new object[0]
                });
            }

            // Đối chiếu id do AI chọn với danh mục để lấy thông tin hiển thị cho người đọc.
            var tuDien = danhMuc.ToDictionary(t => t.Id);
            var goiY = new List<object>();
            foreach (var id in ketQua.TaiLieuIds)
            {
                TaiLieu taiLieu;
                if (!tuDien.TryGetValue(id, out taiLieu)) continue;

                goiY.Add(new
                {
                    id = taiLieu.Id,
                    ten = taiLieu.TenTaiLieu,
                    theLoai = taiLieu.TheLoai != null ? taiLieu.TheLoai.TenTheLoai : "Chưa phân loại",
                    tacGia = taiLieu.TacGia != null ? taiLieu.TacGia.TenTacGia : "Đang cập nhật",
                    gia = taiLieu.GiaHienThi,
                    mienPhi = taiLieu.MienPhi,
                    docOnline = taiLieu.QuyenDocOnline,
                    duocTai = taiLieu.QuyenTai,
                    dinhDang = TepTaiLieuHelper.LayNhanDinhDang(taiLieu.FileUrl),
                    urlChiTiet = Url.Action("ChiTietTaiLieu", "NguoiDoc", new { id = taiLieu.Id })
                });
            }

            return Json(new
            {
                thanhCong = true,
                traLoi = ketQua.TraLoi,
                taiLieu = goiY
            });
        }

        // GET: /NguoiDoc/ChiTietTaiLieu/5
        public ActionResult ChiTietTaiLieu(int id)
        {
            TaiLieu taiLieu;
            try
            {
                taiLieu = _taiLieuRepository.LayTheoId(id);
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
                return RedirectToAction("TimKiemSach");
            }

            var idNguoiDung = PhienDangNhap.LayId();
            var laChuSoHuu = idNguoiDung.HasValue && taiLieu != null && taiLieu.NguoiDangId == idNguoiDung.Value;
            var laQuanTri = PhienDangNhap.LaThuThu || PhienDangNhap.LaAdmin;

            if (taiLieu == null || (!taiLieu.DangHoatDong && !laChuSoHuu && !laQuanTri))
                return HttpNotFound();

            ViewBag.Title = taiLieu.TenTaiLieu;
            var coQuyenTai = false;
            GiaoDich giaoDichDangXuLy = null;

            if (idNguoiDung.HasValue && PhienDangNhap.LaNguoiDoc)
            {
                try
                {
                    coQuyenTai = _quyenTaiLieuRepository.CoQuyenTai(idNguoiDung.Value, taiLieu.Id);
                    if (!coQuyenTai && !taiLieu.MienPhi)
                        giaoDichDangXuLy = _giaoDichRepository.LayGiaoDichDangXuLy(idNguoiDung.Value, taiLieu.Id);
                }
                catch (SqlException)
                {
                    coQuyenTai = false;
                }
            }
            else if (laQuanTri)
            {
                // Thủ thư / Admin có toàn quyền truy cập tài liệu để kiểm tra, xem trước.
                coQuyenTai = true;
            }

            ViewBag.CoQuyenTai = coQuyenTai;
            ViewBag.LaQuanTri = laQuanTri;
            ViewBag.GiaoDichDangXuLy = giaoDichDangXuLy;
            ViewBag.TonTaiPdf = TepTaiLieuHelper.TonTai(taiLieu.FileUrl, "pdf");
            ViewBag.TonTaiWord = TepTaiLieuHelper.TonTai(taiLieu.FileUrl, "docx");
            ViewBag.NhanDinhDang = TepTaiLieuHelper.LayNhanDinhDang(taiLieu.FileUrl);

            return View(taiLieu);
        }

        // ============================ ĐỌC TRỰC TUYẾN ============================

        // GET: /NguoiDoc/TaiLieuSo/5  (không truyền id thì mở tài liệu mới nhất)
        [YeuCauDangNhap]
        public ActionResult TaiLieuSo(int? id)
        {
            ViewBag.Title = "Trình đọc tài liệu trực tuyến";

            TaiLieu taiLieu;
            try
            {
                taiLieu = id.HasValue
                    ? _taiLieuRepository.LayTheoId(id.Value)
                    : _taiLieuRepository.LayMoiNhat();
            }
            catch (SqlException)
            {
                ViewBag.ThongBao = LoiKetNoi;
                return View();
            }

            var idNguoiDung = PhienDangNhap.LayId();
            var laChuSoHuu = idNguoiDung.HasValue && taiLieu != null && taiLieu.NguoiDangId == idNguoiDung.Value;
            var laQuanTri = PhienDangNhap.LaThuThu || PhienDangNhap.LaAdmin;

            if (taiLieu == null || (!taiLieu.DangHoatDong && !laChuSoHuu && !laQuanTri))
            {
                ViewBag.ThongBao = "Không tìm thấy tài liệu cần đọc trong hệ thống hoặc tài liệu chưa được duyệt.";
                return View();
            }

            var coQuyenTai = false;

            try
            {
                if (idNguoiDung.HasValue && PhienDangNhap.LaNguoiDoc)
                    coQuyenTai = _quyenTaiLieuRepository.CoQuyenTai(idNguoiDung.Value, taiLieu.Id);
                else if (laQuanTri)
                    coQuyenTai = true; // Thủ thư / Admin có toàn quyền xem trước và tải để kiểm tra tài liệu
            }
            catch (SqlException)
            {
                ViewBag.ThongBao = LoiKetNoi;
                return View();
            }

            // Đọc trực tuyến: tài liệu phải cho phép đọc online và người đọc phải có quyền.
            // Hàm fn_CoQuyenTaiTaiLieu trả về 1 cho cả tài liệu miễn phí.
            // Thủ thư / Admin luôn đọc được để phục vụ kiểm duyệt và kiểm tra tệp.
            var duocDocOnline = (taiLieu.QuyenDocOnline && coQuyenTai) || laQuanTri;

            ViewBag.TaiLieu = taiLieu;
            ViewBag.CoQuyenTai = coQuyenTai;
            ViewBag.DuocDocOnline = duocDocOnline;
            // Kiểm tra tệp theo đúng định dạng lưu trong CSDL (không ép sang PDF),
            // vì tài liệu do người đọc đóng góp có thể chỉ có bản DOCX.
            ViewBag.TonTaiTep = TepTaiLieuHelper.TonTai(taiLieu.FileUrl);
            ViewBag.TrinhDuyetDocDuoc = TepTaiLieuHelper.DocTrucTuyenDuoc(taiLieu.FileUrl);
            ViewBag.NhanDinhDang = TepTaiLieuHelper.LayNhanDinhDang(taiLieu.FileUrl);

            // Chỉ ghi nhận lưu lượng đọc của người đọc; lượt đọc của thủ thư/admin
            // là thao tác nghiệp vụ, không phản ánh nhu cầu độc giả.
            if (duocDocOnline && idNguoiDung.HasValue && PhienDangNhap.LaNguoiDoc)
            {
                try
                {
                    _lichSuRepository.GhiNhanLuot(idNguoiDung.Value, taiLieu.Id, HangSoHeThong.HanhDongTaiLieu.Xem);
                }
                catch (SqlException)
                {
                    // Ghi nhận lượt xem là best-effort, không chặn thao tác đọc.
                }
            }

            return View();
        }

        // GET: /NguoiDoc/NoiDung/5
        // Trả nội dung tệp để nhúng vào trình đọc trực tuyến.
        // Kiểm tra quyền đọc ở phía máy chủ trước khi trả nội dung.
        // Người đọc, Thủ thư và Admin đều được phép (thủ thư/admin cần kiểm tra nội dung tệp).
        [YeuCauDocTaiLieu]
        public ActionResult NoiDung(int id)
        {
            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return RedirectToAction("DangNhap", "Account");

            TaiLieu taiLieu;
            bool coQuyenTai;

            try
            {
                taiLieu = _taiLieuRepository.LayTheoId(id);
                coQuyenTai = idNguoiDung.HasValue && _quyenTaiLieuRepository.CoQuyenTai(idNguoiDung.Value, id);
            }
            catch (SqlException)
            {
                return HttpNotFound();
            }

            var laChuSoHuu = idNguoiDung.HasValue && taiLieu != null && taiLieu.NguoiDangId == idNguoiDung.Value;
            var laQuanTri = PhienDangNhap.LaThuThu || PhienDangNhap.LaAdmin;

            // Cho phép Thủ thư, Admin hoặc Chủ sở hữu xem nội dung tài liệu (kể cả chưa duyệt)
            if (laQuanTri || laChuSoHuu)
            {
                if (taiLieu == null) return HttpNotFound();
            }
            else
            {
                if (taiLieu == null || !taiLieu.DangHoatDong || !taiLieu.QuyenDocOnline || !coQuyenTai)
                    return HttpNotFound();
            }

            // Mặc định trả về bản PDF để đọc online mượt mà hơn.
            var duongDan = TepTaiLieuHelper.LayDuongDanVatLy(taiLieu.FileUrl, "pdf");
            if (string.IsNullOrEmpty(duongDan) || !System.IO.File.Exists(duongDan))
            {
                // Nếu không có PDF, thử lấy bản gốc
                duongDan = TepTaiLieuHelper.LayDuongDanVatLy(taiLieu.FileUrl);
            }

            if (string.IsNullOrEmpty(duongDan) || !System.IO.File.Exists(duongDan))
                return HttpNotFound();

            return File(duongDan, TepTaiLieuHelper.LayKieuNoiDung(duongDan));
        }

        // ============================ TẢI VỀ ============================

        // GET: /NguoiDoc/DongGop
        [YeuCauNguoiDoc]
        public ActionResult DongGop()
        {
            ViewBag.Title = "Đóng góp tài liệu số";
            ViewBag.DanhSachTheLoai = _taiLieuRepository.LayDanhSachTheLoai();
            ViewBag.DanhSachTacGia = _taiLieuRepository.LayDanhSachTacGia();
            return View();
        }

        // POST: /NguoiDoc/DongGop
        [HttpPost]
        [YeuCauNguoiDoc]
        [ValidateAntiForgeryToken]
        public ActionResult DongGop(TaiLieu model, HttpPostedFileBase fileTaiLieu)
        {
            if (string.IsNullOrWhiteSpace(model.TenTaiLieu))
            {
                ModelState.AddModelError("", "Vui lòng nhập tên tài liệu.");
            }

            if (fileTaiLieu == null || fileTaiLieu.ContentLength == 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn tệp tài liệu để tải lên.");
            }
            else
            {
                var ext = System.IO.Path.GetExtension(fileTaiLieu.FileName).ToLower();
                if (ext != ".pdf" && ext != ".docx" && ext != ".doc")
                {
                    ModelState.AddModelError("", "Chỉ chấp nhận định dạng PDF hoặc Word (.doc, .docx).");
                }
                else if (fileTaiLieu.ContentLength > DungLuongTepToiDa)
                {
                    ModelState.AddModelError("", "Tệp vượt quá dung lượng cho phép (tối đa 20MB).");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lưu tệp vào Content/Document, đặt tên theo tên tài liệu để thủ thư
                    // dễ nhận biết và quản lý (bước phê duyệt chỉ đổi trạng thái hiển thị).
                    var phanMoRong = System.IO.Path.GetExtension(fileTaiLieu.FileName).ToLower();
                    var tenTep = TepTaiLieuHelper.TaoTenTepTheoTaiLieu(model.TenTaiLieu, phanMoRong);
                    var physicalPath = Server.MapPath(TepTaiLieuHelper.ThuMucTep + tenTep);
                    fileTaiLieu.SaveAs(physicalPath);

                    model.FileUrl = TepTaiLieuHelper.TaoFileUrl(tenTep);
                    model.NguoiDangId = PhienDangNhap.LayId();
                    model.TrangThai = HangSoHeThong.TrangThaiTaiLieu.ChoDuyet;
                    model.QuyenDocOnline = true; // Mặc định cho phép đọc online
                    model.QuyenTai = true;       // Mặc định cho phép tải

                    if (_taiLieuRepository.ThemTaiLieu(model))
                    {
                        TempData["ThongBao"] = "Đóng góp tài liệu thành công! Vui lòng đợi thủ thư xác duyệt.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Lỗi hệ thống khi lưu thông tin tài liệu.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi tải tệp lên: " + ex.Message);
                }
            }

            ViewBag.DanhSachTheLoai = _taiLieuRepository.LayDanhSachTheLoai();
            ViewBag.DanhSachTacGia = _taiLieuRepository.LayDanhSachTacGia();
            return View(model);
        }

        // GET: /NguoiDoc/Tai/5?dinhDang=pdf
        // Người đọc, Thủ thư và Admin đều được tải tài liệu.
        // Thủ thư / Admin không cần mua quyền tải (phục vụ kiểm tra, xem trước tệp).
        [YeuCauDocTaiLieu]
        public ActionResult Tai(int id, string dinhDang)
        {
            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return RedirectToAction("DangNhap", "Account");

            TaiLieu taiLieu;
            bool coQuyenTai;

            try
            {
                taiLieu = _taiLieuRepository.LayTheoId(id);
                coQuyenTai = idNguoiDung.HasValue && _quyenTaiLieuRepository.CoQuyenTai(idNguoiDung.Value, id);
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
                return RedirectToAction("ChiTietTaiLieu", new { id });
            }

            var laQuanTri = PhienDangNhap.LaThuThu || PhienDangNhap.LaAdmin;

            if (taiLieu == null)
                return HttpNotFound();

            // Người đọc phải thỏa mãn đầy đủ điều kiện nghiệp vụ;
            // Thủ thư / Admin được bỏ qua bước mua quyền để phục vụ kiểm duyệt.
            if (!laQuanTri)
            {
                if (!taiLieu.DangHoatDong)
                    return HttpNotFound();

                if (!taiLieu.QuyenTai)
                {
                    TempData["Loi"] = "Tài liệu này không cho phép tải xuống.";
                    return RedirectToAction("ChiTietTaiLieu", new { id });
                }

                // Chặn tải trực tiếp bằng URL khi chưa có quyền (tài liệu trả phí chưa mua).
                if (!coQuyenTai)
                {
                    TempData["Loi"] = "Bạn chưa có quyền tải tài liệu này. Vui lòng mua quyền tải để tiếp tục.";
                    return RedirectToAction("MuaTaiLieu", new { id });
                }
            }

            var duongDan = TepTaiLieuHelper.LayDuongDanVatLy(taiLieu.FileUrl, dinhDang);
            var dinhDangThucTe = dinhDang;

            // Khi người dùng không chỉ định định dạng, file_url trong CSDL có thể
            // không khớp phần mở rộng của tệp thật (ví dụ file_url là .txt nhưng tệp
            // lưu trên máy chủ là .pdf/.docx). Tự dò lần lượt PDF, Word để tải đúng
            // tệp đang có thay vì báo lỗi.
            if (string.IsNullOrEmpty(dinhDang)
                && (string.IsNullOrEmpty(duongDan) || !System.IO.File.Exists(duongDan)))
            {
                foreach (var ungVien in new[] { "pdf", "docx", "doc" })
                {
                    var duongDanThu = TepTaiLieuHelper.LayDuongDanVatLy(taiLieu.FileUrl, ungVien);
                    if (!string.IsNullOrEmpty(duongDanThu) && System.IO.File.Exists(duongDanThu))
                    {
                        duongDan = duongDanThu;
                        dinhDangThucTe = ungVien;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(duongDan) || !System.IO.File.Exists(duongDan))
            {
                TempData["Loi"] = "Tệp tài liệu định dạng " + (dinhDang?.ToUpper() ?? "gốc") + " chưa được tải lên hệ thống.";
                return RedirectToAction("ChiTietTaiLieu", new { id });
            }

            // Chỉ ghi nhận lưu lượng tải của người đọc (procedure sp_GhiNhanLuotTaiLieu
            // chỉ chấp nhận tài khoản NGUOI_DOC đang hoạt động).
            if (PhienDangNhap.LaNguoiDoc)
            {
                try
                {
                    _lichSuRepository.GhiNhanLuot(idNguoiDung.Value, id, HangSoHeThong.HanhDongTaiLieu.Tai);
                }
                catch (SqlException)
                {
                    // Ghi nhận lượt tải là best-effort.
                }
            }

            var kieuNoiDung = TepTaiLieuHelper.LayKieuNoiDung(taiLieu.FileUrl, dinhDangThucTe);
            var tenTepTaiVe = TepTaiLieuHelper.LayTenTepTaiVe(taiLieu, dinhDangThucTe);

            // Đánh dấu bản quyền lên bản tải về: chèn khối watermark chứa tên người tải
            // và thời điểm tải. Tệp nhị phân thật (PDF/DOCX) không sửa được nên trả nguyên bản.
            var khoiWatermark = WatermarkHelper.TaoKhoiWatermark(
                PhienDangNhap.LayHoTen(), idNguoiDung, PhienDangNhap.LayEmail(), DateTime.Now);
            var noiDungDaDanhDau = WatermarkHelper.ChenWatermark(duongDan, khoiWatermark);
            if (noiDungDaDanhDau != null)
                return File(noiDungDaDanhDau, kieuNoiDung, tenTepTaiVe);

            return File(duongDan, kieuNoiDung, tenTepTaiVe);
        }

        // ============================ MUA QUYỀN TẢI ============================

        // GET: /NguoiDoc/MuaTaiLieu/5
        [YeuCauNguoiDoc]
        public ActionResult MuaTaiLieu(int id)
        {
            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return RedirectToAction("DangNhap", "Account");

            TaiLieu taiLieu;
            bool coQuyenTai;

            try
            {
                taiLieu = _taiLieuRepository.LayTheoId(id);
                coQuyenTai = _quyenTaiLieuRepository.CoQuyenTai(idNguoiDung.Value, id);
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
                return RedirectToAction("TimKiemSach");
            }

            if (taiLieu == null || !taiLieu.DangHoatDong)
                return HttpNotFound();

            // Tài liệu miễn phí hoặc đã có quyền thì không cần mua.
            if (taiLieu.MienPhi || coQuyenTai)
                return RedirectToAction("ChiTietTaiLieu", new { id });

            ViewBag.Title = "Mua quyền tải tài liệu";
            ViewBag.CoQuyenTai = coQuyenTai;

            try
            {
                ViewBag.GiaoDichDangXuLy = _giaoDichRepository.LayGiaoDichDangXuLy(idNguoiDung.Value, id);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
            }

            return View(taiLieu);
        }

        // POST: /NguoiDoc/TaoGiaoDich
        [HttpPost]
        [ValidateAntiForgeryToken]
        [YeuCauNguoiDoc]
        public ActionResult TaoGiaoDich(int taiLieuId)
        {
            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return RedirectToAction("DangNhap", "Account");

            try
            {
                var taiLieu = _taiLieuRepository.LayTheoId(taiLieuId);
                if (taiLieu == null || !taiLieu.DangHoatDong)
                    return HttpNotFound();

                if (taiLieu.MienPhi || _quyenTaiLieuRepository.CoQuyenTai(idNguoiDung.Value, taiLieuId))
                    return RedirectToAction("ChiTietTaiLieu", new { id = taiLieuId });

                // Đã có giao dịch chờ thanh toán thì dùng lại, tránh tạo trùng.
                if (_giaoDichRepository.LayGiaoDichDangXuLy(idNguoiDung.Value, taiLieuId) == null)
                {
                    _giaoDichRepository.TaoGiaoDich(idNguoiDung.Value, taiLieuId);
                    _nhatKyRepository.GhiNhatKy(idNguoiDung.Value, "Tạo giao dịch mua tài liệu #" + taiLieuId);
                }
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }

            return RedirectToAction("MuaTaiLieu", new { id = taiLieuId });
        }

        // POST: /NguoiDoc/XacNhanThanhToan
        [HttpPost]
        [ValidateAntiForgeryToken]
        [YeuCauNguoiDoc]
        public ActionResult XacNhanThanhToan(int giaoDichId)
        {
            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return RedirectToAction("DangNhap", "Account");

            GiaoDich giaoDich;
            try
            {
                giaoDich = _giaoDichRepository.LayTheoId(giaoDichId);
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
                return RedirectToAction("Index");
            }

            // Chỉ chủ sở hữu giao dịch mới được xác nhận thanh toán.
            if (giaoDich == null || giaoDich.UserId != idNguoiDung.Value)
                return HttpNotFound();

            if (giaoDich.TrangThai == HangSoHeThong.TrangThaiGiaoDich.ThanhCong)
            {
                TempData["ThongBao"] = "Giao dịch này đã được thanh toán trước đó.";
                return RedirectToAction("KetQuaGiaoDich", new { id = giaoDichId });
            }

            if (giaoDich.TrangThai != HangSoHeThong.TrangThaiGiaoDich.DangXuLy)
            {
                TempData["Loi"] = "Giao dịch không còn ở trạng thái chờ thanh toán.";
                return RedirectToAction("KetQuaGiaoDich", new { id = giaoDichId });
            }

            try
            {
                if (!MoPhongCongThanhToan(giaoDich))
                {
                    TempData["Loi"] = "Thanh toán không thành công. Vui lòng thử lại.";
                    return RedirectToAction("KetQuaGiaoDich", new { id = giaoDichId });
                }

                // Chỉ máy chủ gọi procedure xác nhận; trigger trg_GiaoDich_CapQuyenTai
                // sẽ tự động cấp quyền tải cho người đọc.
                _giaoDichRepository.XacNhanThanhCong(giaoDichId);
                _nhatKyRepository.GhiNhatKy(idNguoiDung.Value, "Xác nhận thanh toán giao dịch #" + giaoDichId);
                TempData["ThongBao"] = "Thanh toán thành công. Quyền tải tài liệu đã được cấp.";
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
            }

            return RedirectToAction("KetQuaGiaoDich", new { id = giaoDichId });
        }

        // GET: /NguoiDoc/KetQuaGiaoDich/5
        [YeuCauNguoiDoc]
        public ActionResult KetQuaGiaoDich(int id)
        {
            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return RedirectToAction("DangNhap", "Account");

            GiaoDich giaoDich;
            bool coQuyenTai = false;

            try
            {
                giaoDich = _giaoDichRepository.LayTheoId(id);

                if (giaoDich != null && giaoDich.UserId == idNguoiDung.Value && giaoDich.TaiLieuId.HasValue)
                    coQuyenTai = _quyenTaiLieuRepository.CoQuyenTai(idNguoiDung.Value, giaoDich.TaiLieuId.Value);
            }
            catch (SqlException)
            {
                TempData["Loi"] = LoiKetNoi;
                return RedirectToAction("Index");
            }

            if (giaoDich == null || giaoDich.UserId != idNguoiDung.Value)
                return HttpNotFound();

            ViewBag.Title = "Kết quả giao dịch";
            ViewBag.CoQuyenTai = coQuyenTai;

            return View(giaoDich);
        }

        // ============================ LỊCH SỬ ============================

        // GET: /NguoiDoc/LichSuMuon
        [YeuCauDangNhap]
        public ActionResult LichSuMuon()
        {
            ViewBag.Title = "Lịch sử giao dịch & Lịch sử đọc tài liệu";

            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return RedirectToAction("DangNhap", "Account");

            try
            {
                ViewBag.DanhSachGiaoDich = _giaoDichRepository.LayTheoNguoiDung(idNguoiDung.Value);
                ViewBag.DanhSachLichSu = _lichSuRepository.LayTheoNguoiDung(idNguoiDung.Value, SoDongLichSu);
                ViewBag.DanhSachQuyenTai = _quyenTaiLieuRepository.LayTheoNguoiDung(idNguoiDung.Value);
                ViewBag.DanhSachTaiLieuDongGop = _taiLieuRepository.LayTheoNguoiDang(idNguoiDung.Value);
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi = LoiKetNoi;
                ViewBag.DanhSachGiaoDich = new List<GiaoDich>();
                ViewBag.DanhSachLichSu = new List<LichSuTaiLieu>();
                ViewBag.DanhSachQuyenTai = new List<QuyenTaiLieu>();
                ViewBag.DanhSachTaiLieuDongGop = new List<TaiLieu>();
            }

            return View();
        }

        // ============================ HỖ TRỢ ============================

        /// <summary>
        /// Mô phỏng phản hồi của cổng thanh toán trước khi ghi nhận giao dịch thành công.
        /// GIỚI HẠN: hệ thống chưa tích hợp cổng thanh toán thật (VNPay / MoMo / ngân hàng).
        /// Khi triển khai thực tế phải thay hàm này bằng bước xác minh chữ ký / callback
        /// do cổng thanh toán trả về; không xác nhận thành công chỉ vì người dùng bấm nút.
        /// </summary>
        private static bool MoPhongCongThanhToan(GiaoDich giaoDich)
        {
            return giaoDich != null && giaoDich.SoTien.HasValue && giaoDich.SoTien.Value > 0;
        }
    }
}
