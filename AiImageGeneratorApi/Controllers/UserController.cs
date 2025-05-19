using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AiImageGeneratorApi.Interfaces; // ← nơi chứa IUnitOfWork
using AiImageGeneratorApi.Models.Entities;
using System;
using System.Threading.Tasks;
using AiImageGeneratorApi.Models.DTOs;

namespace AiImageGeneratorApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUser()
        {
            var data = await _unitOfWork.Users.GetAllSelectAsync(
                x => !x.IsDeleted,
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
            user.UpdatedAt = DateTime.UtcNow;
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
            user.DeletedAt = DateTime.UtcNow;
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
