using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AiImageGeneratorApi.Interfaces; // ← nơi chứa IUnitOfWork
using AiImageGeneratorApi.Models.Entities;
using System;
using System.Threading.Tasks;
using AiImageGeneratorApi.Models.DTOs;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;
using static AiImageGeneratorApi.Controllers.AuthController;

namespace AiImageGeneratorApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Guid _currentUserId;

        public UserController(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _currentUserId = Guid.Parse(httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUser([FromQuery] string? keyword)
        {
            var data = await _unitOfWork.Users.GetAllSelectAsync(
                x => !x.IsDeleted &&
                    (string.IsNullOrEmpty(keyword) ||
                     x.HoVaTen.Contains(keyword) ||
                     x.Email.Contains(keyword) ||
                     x.Sdt.Contains(keyword)),
                x => new
                {
                    x.Id,
                    x.HoVaTen,
                    x.DiaChi,
                    x.Email,
                    x.Sdt,
                    x.HinhAnh,
                });

            return Ok(data);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet("chat/{userId}")]
        public async Task<IActionResult> GetUserProfile(Guid userId, [FromQuery] bool isNhom = false, [FromQuery] Guid? groupId = null)
        {
            string sql = "EXEC sp_Chat_GetUserProfile @CurrentUserId, @TargetUserId, @IsGroup, @GroupId";

            var result = await _unitOfWork.Connection.QueryFirstOrDefaultAsync<dynamic>(
                sql,
                new
                {
                    CurrentUserId = _currentUserId,
                    TargetUserId = userId,
                    IsGroup = isNhom,
                    GroupId = groupId
                },
                transaction: _unitOfWork.Transaction
            );

            if (result == null)
                return NotFound("Không tìm thấy thông tin người dùng.");

            return Ok(result);
        }

        [HttpPost("generate-keys")]
        public async Task<IActionResult> GenerateRsaKeysForAllUsers()
        {
            var users = await _unitOfWork.Users.FindAsync(u => string.IsNullOrEmpty(u.PublicKey));

            foreach (var user in users)
            {
                var (pub, priv) = RsaKeyGenerator.GenerateKeyPair();
                user.PublicKey = pub;
                user.PrivateKey = priv;
                _unitOfWork.Users.Update(user);
            }

            await _unitOfWork.CompleteAsync();
            return Ok(new { Count = users.Count() });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UserPutDto userDto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return NotFound();

            user.HoVaTen = userDto.HoVaTen;
            user.DiaChi = userDto.DiaChi;
            user.Email = userDto.Email;
            user.Sdt = userDto.Sdt;
            user.HinhAnh = userDto.HoVaTen;
            user.UpdatedAt = DateTime.Now;
            user.UpdatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();

            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return NotFound();

            user.IsDeleted = true;
            user.DeletedAt = DateTime.Now;
            user.DeletedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();

            return NoContent();
        }

        [HttpDelete("remove/{id}")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(x => x.Id == id, ignoreQueryFilters: true);
            if (user == null) return NotFound();

            _unitOfWork.Users.Remove(user);
            await _unitOfWork.CompleteAsync();

            return NoContent();
        }
    }
}
