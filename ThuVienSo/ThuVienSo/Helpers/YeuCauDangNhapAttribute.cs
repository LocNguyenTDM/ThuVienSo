using System.Data.SqlClient;
using System.Web;
using System.Web.Mvc;
using ThuVienSo.Data;

namespace ThuVienSo.Helpers
{
    /// <summary>
    /// Bắt buộc đăng nhập trước khi vào các chức năng cá nhân.
    /// Đồng thời kiểm tra lại trạng thái tài khoản trong CSDL để tài khoản bị khóa
    /// hoặc bị đổi vai trò bởi quản trị viên không tiếp tục dùng được phiên cũ.
    /// </summary>
    public class YeuCauDangNhapAttribute : AuthorizeAttribute
    {
        private const string KhoaThongBao = "LoiXacThuc";

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var idNguoiDung = PhienDangNhap.LayId();
            if (!idNguoiDung.HasValue)
                return false;

            try
            {
                var nguoiDung = new NguoiDungRepository().LayTheoId(idNguoiDung.Value);
                if (nguoiDung == null)
                {
                    PhienDangNhap.XoaPhien();
                    httpContext.Items[KhoaThongBao] = "Tài khoản không còn tồn tại. Vui lòng đăng nhập lại.";
                    return false;
                }

                if (!nguoiDung.DangHoatDong)
                {
                    PhienDangNhap.XoaPhien();
                    httpContext.Items[KhoaThongBao] =
                        "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.";
                    return false;
                }

                // Làm mới họ tên và vai trò trong phiên để phản ánh thay đổi mới nhất.
                PhienDangNhap.GhiPhien(nguoiDung);
                return true;
            }
            catch (SqlException)
            {
                httpContext.Items[KhoaThongBao] =
                    "Không thể kiểm tra quyền truy cập do lỗi kết nối cơ sở dữ liệu.";
                return false;
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var thongBao = httpContext.Items[KhoaThongBao] as string;

            if (!PhienDangNhap.DaDangNhap)
            {
                var returnUrl = httpContext.Request.RawUrl;
                var duongDan = "~/Account/DangNhap?returnUrl=" + HttpUtility.UrlEncode(returnUrl);

                filterContext.Controller.TempData["ThongBaoDangNhap"] =
                    thongBao ?? "Vui lòng đăng nhập để sử dụng chức năng này.";

                filterContext.Result = new RedirectResult(duongDan);
                return;
            }

            // Đã đăng nhập nhưng không đủ vai trò: không chuyển về trang đăng nhập
            // (tránh gây hiểu nhầm), mà trả về trang chủ kèm thông báo.
            filterContext.Controller.TempData["Loi"] =
                thongBao ?? "Bạn không có quyền truy cập chức năng này.";
            filterContext.Result = new RedirectResult("~/Home/Index");
        }
    }

    /// <summary>
    /// Chỉ cho phép vai trò Người đọc truy cập (các chức năng đọc/tải/mua tài liệu).
    /// </summary>
    public class YeuCauNguoiDocAttribute : YeuCauDangNhapAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            return base.AuthorizeCore(httpContext) && PhienDangNhap.LaNguoiDoc;
        }
    }

    /// <summary>
    /// Chỉ cho phép vai trò Thủ thư truy cập (các chức năng quản lý tài liệu).
    /// </summary>
    public class YeuCauThuThuAttribute : YeuCauDangNhapAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            return base.AuthorizeCore(httpContext) && PhienDangNhap.LaThuThu;
        }
    }

    /// <summary>
    /// Chỉ cho phép vai trò Quản trị viên truy cập toàn bộ chức năng quản trị hệ thống:
    /// tài khoản, phân quyền, giao dịch, thống kê và nhật ký hoạt động.
    /// </summary>
    public class YeuCauAdminAttribute : YeuCauDangNhapAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            return base.AuthorizeCore(httpContext) && PhienDangNhap.LaAdmin;
        }
    }

    /// <summary>
    /// Cho phép Người đọc, Thủ thư và Admin truy cập các chức năng xem/tải tài liệu.
    /// Thủ thư và Admin cần xem trước và tải tệp để kiểm tra chất lượng nội dung
    /// mà không cần mua quyền tải.
    /// </summary>
    public class YeuCauDocTaiLieuAttribute : YeuCauDangNhapAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            return base.AuthorizeCore(httpContext)
                   && (PhienDangNhap.LaNguoiDoc || PhienDangNhap.LaThuThu || PhienDangNhap.LaAdmin);
        }
    }
}
