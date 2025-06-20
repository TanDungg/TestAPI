﻿using AiImageGeneratorApi.Data;
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

        public static class RsaKeyGenerator
        {
            public static (string PublicKey, string PrivateKey) GenerateKeyPair()
            {
                using var rsa = RSA.Create(2048);
                return (
                    Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()),
                    Convert.ToBase64String(rsa.ExportPkcs8PrivateKey())
                );
            }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] UserRegisterDto auth)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                       .ToDictionary(
                                           kvp => kvp.Key,
                                           kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                       );

                return BadRequest(errors);
            }

            if (_context.Users.Any(u => u.TenDangNhap == auth.TenDangNhap && !u.IsDeleted))
            {
                return Conflict("Người dùng đã tồn tại!");
            }

            try
            {
                var userId = Guid.NewGuid(); // ✅ Tạo thủ công

                var user = new User
                {
                    Id = userId,
                    TenDangNhap = auth.TenDangNhap,
                    MatKhau = MD5Hash(auth.MatKhau),
                    HoVaTen = auth.HoVaTen,
                    DiaChi = auth?.DiaChi,
                    Email = auth?.Email,
                    Sdt = auth?.Sdt,
                    HinhAnh = auth.HinhAnh,
                    CreatedAt = DateTime.Now,
                    CreatedBy = userId // ✅ an toàn
                };

                var (publicKey, privateKey) = RsaKeyGenerator.GenerateKeyPair();
                user.PublicKey = publicKey;
                user.PrivateKey = privateKey;

                _context.Users.Add(user);
                _context.SaveChanges();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthRequest auth)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                       .ToDictionary(
                                           kvp => kvp.Key,
                                           kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                       );

                return BadRequest(errors);
            }

            var user = _context.Users.FirstOrDefault(u => u.TenDangNhap == auth.Username && !u.IsDeleted);
            if (user == null)
            {
                return Conflict("Tài khoản không tồn tại!");
            }

            var hashedPassword = MD5Hash(auth.Password);
            if (user.MatKhau != hashedPassword)
            {
                return Conflict("Mật khẩu không đúng!");
            }

            var token = _tokenService.GenerateToken(user);

            var userInfo = new UserRegisterDto
            {
                Id = user.Id,
                HoVaTen = user.HoVaTen,
                TenDangNhap = user.TenDangNhap,
                Email = user.Email,
                HinhAnh = user.HinhAnh,
                Sdt = user.Sdt
            };

            return Ok(new AuthResponse
            {
                Token = token,
                User = userInfo
            });
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
