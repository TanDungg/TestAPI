using System.ComponentModel.DataAnnotations;

namespace AiImageGeneratorApi.Models.DTOs
{
    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        public string HoVaTen { get; set; }
        public string DiaChi { get; set; }
        public string Email { get; set; }
        public string Sdt { get; set; }

        [Required(ErrorMessage = "Hình ảnh là bắt buộc")]
        public string HinhAnh { get; set; }
    }
    public class UserPutDto
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        public string HoVaTen { get; set; }
        public string DiaChi { get; set; }
        public string Email { get; set; }
        public string Sdt { get; set; }

        [Required(ErrorMessage = "Hình ảnh là bắt buộc")]
        public string HinhAnh { get; set; }
    }
}