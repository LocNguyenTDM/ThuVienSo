using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThuVienSo.Models;

namespace ThuVienSo.Helpers
{
    /// <summary>Kết quả trả về của trợ lý AI gợi ý tài liệu.</summary>
    public class KetQuaTroLyAi
    {
        public bool ThanhCong { get; set; }

        /// <summary>Câu trả lời dạng ngôn ngữ tự nhiên của mô hình.</summary>
        public string TraLoi { get; set; }

        /// <summary>Id các tài liệu mô hình chọn từ danh mục thật của thư viện.</summary>
        public List<int> TaiLieuIds { get; set; }

        public string Loi { get; set; }

        public KetQuaTroLyAi()
        {
            TaiLieuIds = new List<int>();
        }
    }

    /// <summary>
    /// Trợ lý AI (chatbox) gợi ý tài liệu: gọi API Chat Completions tương thích OpenAI
    /// và yêu cầu mô hình ngôn ngữ đọc mô tả của người đọc rồi chọn ra các tài liệu
    /// phù hợp trong danh mục thật đang có của thư viện.
    ///
    /// Khác với tìm kiếm thường (chỉ dò khớp chuỗi trong CSDL), ở đây việc chọn tài liệu
    /// do mô hình ngôn ngữ suy luận, nên trợ lý hiểu được mô tả tự nhiên của người đọc
    /// chứ không chỉ dò chuỗi con.
    ///
    /// Mặc định dùng dịch vụ miễn phí Pollinations.AI (không cần đăng ký, không cần
    /// key) phù hợp cho đồ án sinh viên. Nếu muốn chất lượng và giới hạn tốt hơn,
    /// chỉ cần đổi AI_Endpoint/AI_Model sang OpenAI và điền AI_ApiKey.
    ///
    /// Cấu hình đọc từ appSettings trong Web.config; riêng khóa API có thể đặt qua
    /// biến môi trường thay cho AI_ApiKey. Khi không có khóa, request gửi đi không
    /// kèm header Authorization (đúng như dịch vụ miễn phí yêu cầu).
    /// </summary>
    public static class TroLyTaiLieuAiHelper
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        private const string EndpointMacDinh = "https://text.pollinations.ai/openai";
        private const string ModelMacDinh = "openai";

        /// <summary>Độ dài tối đa của phần mô tả tài liệu đưa vào ngữ cảnh cho mô hình.</summary>
        private const int DoDaiMoTaToiDa = 300;

        /// <summary>
        /// Lấy khóa API (không bắt buộc) từ appSettings "AI_ApiKey" trong Web.config
        /// hoặc biến môi trường. Trả về null khi dùng dịch vụ miễn phí không cần khóa.
        /// </summary>
        public static string LayApiKey()
        {
            var khoa = ConfigurationManager.AppSettings["AI_ApiKey"];
            if (string.IsNullOrWhiteSpace(khoa))
                khoa = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            return string.IsNullOrWhiteSpace(khoa) ? null : khoa.Trim();
        }

        /// <summary>
        /// Gửi mô tả của người đọc cho mô hình và nhận lại câu trả lời kèm danh sách
        /// id tài liệu phù hợp. Bọc bất đồng bộ trong Task.Run để tránh deadlock
        /// SynchronizationContext của ASP.NET khi chặn luồng xử lý request.
        /// </summary>
        public static KetQuaTroLyAi HoiDap(string cauHoi, IList<TaiLieu> danhMuc)
        {
            return Task.Run(() => HoiDapAsync(cauHoi, danhMuc)).GetAwaiter().GetResult();
        }

        private static async Task<KetQuaTroLyAi> HoiDapAsync(string cauHoi, IList<TaiLieu> danhMuc)
        {
            var endpoint = ConfigurationManager.AppSettings["AI_Endpoint"];
            if (string.IsNullOrWhiteSpace(endpoint)) endpoint = EndpointMacDinh;

            var model = ConfigurationManager.AppSettings["AI_Model"];
            if (string.IsNullOrWhiteSpace(model)) model = ModelMacDinh;

            var yeuCau = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(TaoThanYeuCau(cauHoi, danhMuc, model), Encoding.UTF8, "application/json")
            };

            // Dịch vụ miễn phí không cần khóa; chỉ gắn header khi thực sự có khóa.
            var apiKey = LayApiKey();
            if (apiKey != null)
                yeuCau.Headers.Add("Authorization", "Bearer " + apiKey);

            HttpResponseMessage phanHoi;
            try
            {
                phanHoi = await Http.SendAsync(yeuCau);
            }
            catch (Exception loi)
            {
                return new KetQuaTroLyAi
                {
                    ThanhCong = false,
                    Loi = "Không gọi được dịch vụ AI: " + loi.Message
                };
            }

            string noiDung;
            try
            {
                noiDung = await phanHoi.Content.ReadAsStringAsync();
            }
            catch (Exception loi)
            {
                return new KetQuaTroLyAi
                {
                    ThanhCong = false,
                    Loi = "Không đọc được phản hồi của dịch vụ AI: " + loi.Message
                };
            }

            if (!phanHoi.IsSuccessStatusCode)
            {
                return new KetQuaTroLyAi
                {
                    ThanhCong = false,
                    Loi = "Dịch vụ AI trả về mã lỗi " + (int)phanHoi.StatusCode + ": " + TrichThongDiepLoi(noiDung)
                };
            }

            return PhanTichPhanHoi(noiDung);
        }

        private static string TaoThanYeuCau(string cauHoi, IList<TaiLieu> danhMuc, string model)
        {
            var danhMucText = new StringBuilder();
            foreach (var taiLieu in danhMuc)
            {
                var theLoai = taiLieu.TheLoai != null ? taiLieu.TheLoai.TenTheLoai : "Chưa phân loại";
                var tacGia = taiLieu.TacGia != null ? taiLieu.TacGia.TenTacGia : "Đang cập nhật";
                var moTa = taiLieu.MoTa ?? string.Empty;
                if (moTa.Length > DoDaiMoTaToiDa)
                    moTa = moTa.Substring(0, DoDaiMoTaToiDa) + "...";

                danhMucText
                    .Append(taiLieu.Id).Append(" | ")
                    .Append(taiLieu.TenTaiLieu).Append(" | Thể loại: ").Append(theLoai)
                    .Append(" | Tác giả: ").Append(tacGia)
                    .Append(" | Mô tả: ").Append(moTa)
                    .Append('\n');
            }

            var chiDan = @"Bạn là trợ lý tra cứu của một thư viện số tiếng Việt. Người đọc sẽ mô tả bằng ngôn ngữ tự nhiên loại tài liệu họ muốn tìm.
Dưới đây là DANH MỤC tài liệu thật đang có trong thư viện, mỗi dòng có dạng: ID | Tên tài liệu | Thể loại | Tác giả | Mô tả:

" + danhMucText + @"
Hãy đọc kỹ mô tả của người đọc, đối chiếu với từng tài liệu trong danh mục và chọn ra những tài liệu thực sự liên quan nhất (thường từ 1 đến 5 tài liệu). Chỉ được chọn ID có trong danh mục.
Trả về DUY NHẤT một đối tượng JSON hợp lệ, không kèm văn bản nào khác, theo đúng định dạng:
{""traLoi"": ""<câu trả lời tiếng Việt thân thiện 2-4 câu, nói ngắn gọn vì sao các tài liệu này phù hợp và gợi ý nên đọc tài liệu nào trước>"", ""taiLieuIds"": [<danh sách ID đã chọn>]}
Nếu không có tài liệu nào phù hợp, trả về ""taiLieuIds"": [] và trong ""traLoi"" hãy lịch sự nói chưa tìm thấy tài liệu khớp và đề nghị người đọc mô tả cụ thể hơn.";

            var doiTuong = new
            {
                model = model,
                messages = new object[]
                {
                    new { role = "system", content = chiDan },
                    new { role = "user", content = cauHoi }
                },
                temperature = 0.3
            };

            return JsonConvert.SerializeObject(doiTuong);
        }

        private static KetQuaTroLyAi PhanTichPhanHoi(string noiDung)
        {
            try
            {
                var goc = JObject.Parse(noiDung);
                var noiDungTraLoi = goc["choices"] != null && goc["choices"].HasValues
                    ? goc["choices"][0]["message"]["content"].ToString()
                    : null;

                if (string.IsNullOrWhiteSpace(noiDungTraLoi))
                    return new KetQuaTroLyAi { ThanhCong = false, Loi = "AI không trả về nội dung." };

                var doiTuong = JObject.Parse(TrichJson(noiDungTraLoi));
                var ketQua = new KetQuaTroLyAi
                {
                    ThanhCong = true,
                    TraLoi = doiTuong["traLoi"] != null
                        ? doiTuong["traLoi"].ToString()
                        : "Trợ lý đã xử lý yêu cầu của bạn."
                };

                var mangId = doiTuong["taiLieuIds"] as JArray;
                if (mangId != null)
                {
                    foreach (var phanTu in mangId)
                    {
                        int id;
                        if (int.TryParse(phanTu.ToString(), out id))
                            ketQua.TaiLieuIds.Add(id);
                    }
                }

                return ketQua;
            }
            catch (Exception loi)
            {
                return new KetQuaTroLyAi
                {
                    ThanhCong = false,
                    Loi = "Không đọc được phản hồi của AI: " + loi.Message
                };
            }
        }

        /// <summary>Lấy phần JSON trong nội dung trả về, phòng trường hợp mô hình bọc trong ```json ... ```.</summary>
        private static string TrichJson(string noiDung)
        {
            var batDau = noiDung.IndexOf('{');
            var ketThuc = noiDung.LastIndexOf('}');

            if (batDau >= 0 && ketThuc > batDau)
                return noiDung.Substring(batDau, ketThuc - batDau + 1);

            return noiDung;
        }

        /// <summary>Trích thông điệp lỗi mà OpenAI trả về trong trường "error.message".</summary>
        private static string TrichThongDiepLoi(string noiDung)
        {
            try
            {
                var goc = JObject.Parse(noiDung);
                if (goc["error"] != null && goc["error"]["message"] != null)
                    return goc["error"]["message"].ToString();
            }
            catch (Exception)
            {
                // Không đọc được JSON lỗi thì trả về nội dung thô.
            }

            return noiDung;
        }
    }
}
