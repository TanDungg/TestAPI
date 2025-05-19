using AiImageGeneratorApi.Data;
using AiImageGeneratorApi.Models.DTOs;
using AiImageGeneratorApi.Models.Entities;
using AiImageGeneratorApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AiImageGeneratorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(ApplicationDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public IActionResult Register(UserRegisterDto auth)
        {
            if (_context.Users.Any(u => u.TenDangNhap == auth.TenDangNhap && !u.IsDeleted))
                return BadRequest("Người dùng đã tồn tại!");

            var user = new User
            {
                TenDangNhap = auth.TenDangNhap,
                MatKhau = MD5Hash(auth.MatKhau),
                HoVaTen = auth.HoVaTen,
                DiaChi = auth.DiaChi,
                Email = auth.Email,
                Sdt = auth.Sdt,
                HinhAnh = auth.HinhAnh,
                CreatedAt = DateTime.Now,
                CreatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok();
        }

        [HttpPost("login")]
        public IActionResult Login(AuthRequest auth)
        {
            if (auth.Username == null || auth.Username == "")
            {
                return Conflict("Tên đăng nhập là bắt buộc!");
            }

            if (auth.Password == null || auth.Password == "")
            {
                return Conflict("Mật khẩu là bắt buộc!");
            }

            var hash = MD5Hash(auth.Password);
            var find_user = _context.Users.FirstOrDefault(u => u.TenDangNhap == auth.Username);
            if (find_user == null)
            {
                return Conflict("Tài khoản không tồn tại!");
            }

            if (find_user.MatKhau != hash)
            {
                return Conflict("Mật khẩu sai, vui lòng nhập lại mật khẩu!");
            }

            var token = _tokenService.GenerateToken(find_user);
            return Ok(new AuthResponse { Token = token });
        }


        private string MD5Hash(string input)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = md5.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
