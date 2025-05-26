using System.ComponentModel.DataAnnotations;

namespace AiImageGeneratorApi.Models.DTOs
{
    public class AuthRequest
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; }
    }

}
