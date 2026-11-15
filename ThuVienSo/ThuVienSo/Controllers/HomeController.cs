using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using ThuVienSo.Data;
using ThuVienSo.Models;

namespace ThuVienSo.Controllers
{
    /// <summary>
    /// Trang công khai: trang chủ hiển thị danh sách tài liệu số mới nhất từ CSDL,
    /// giới thiệu và liên hệ.
    /// </summary>
    public class HomeController : Controller
    {
        private const int SoTaiLieuTrangChu = 8;

        private readonly TaiLieuRepository _taiLieuRepository = new TaiLieuRepository();

        public ActionResult Index()
        {
            ViewBag.Title = "Trang chủ - Thư Viện Số";

            try
            {
                ViewBag.DanhSachTaiLieu = _taiLieuRepository.LayDanhSach(null, null, null, null, SoTaiLieuTrangChu, null);
                ViewBag.TongTaiLieu = _taiLieuRepository.DemTaiLieuHoatDong();
                ViewBag.DanhSachTheLoai = _taiLieuRepository.LayDanhSachTheLoai();
            }
            catch (SqlException)
            {
                ViewBag.LoiKetNoi =
                    "Không thể kết nối cơ sở dữ liệu. Vui lòng kiểm tra lại chuỗi kết nối trong Web.config.";
                ViewBag.DanhSachTaiLieu = new List<TaiLieu>();
                ViewBag.DanhSachTheLoai = new List<TheLoai>();
                ViewBag.TongTaiLieu = 0;
            }

            return View();
        }

        public ActionResult GioiThieu()
        {
            ViewBag.Title = "Giới thiệu - Thư Viện Số";
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Hệ thống Quản lý Thư viện số hiện đại.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Thông tin liên hệ Ban Quản lý Thư viện.";
            return View();
        }
    }
}
