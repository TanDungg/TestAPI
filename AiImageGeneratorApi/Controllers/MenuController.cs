using AiImageGeneratorApi.Interfaces;
using AiImageGeneratorApi.Models.DTOs;
using AiImageGeneratorApi.Models.Entities;
using AiImageGeneratorApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AiImageGeneratorApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public MenuController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/menu
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var data = await _unitOfWork.Menus.GetAllSelectAsync(
                m => !m.IsDeleted,
                m => new
                {
                    m.Id,
                    m.MaMenu,
                    m.TenMenu,
                    m.Icon,
                    m.DuongDan,
                    m.ThuTu
                });

            return Ok(data.OrderBy(m => m.ThuTu));
        }

        // GET: api/menu/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var menu = await _unitOfWork.Menus.GetByIdAsync(id);
            if (menu == null || menu.IsDeleted) return NotFound();
            return Ok(menu);
        }

        // POST: api/menu
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MenuDto menuDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var allMenus = await _unitOfWork.Menus.FindAsync(m => !m.IsDeleted);
            var maxThuTu = allMenus.Any() ? allMenus.Max(m => m.ThuTu) : 0;

            Guid createdByGuid;
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out createdByGuid))
            {
                return BadRequest("Invalid User GUID.");
            }

            var menu = new Menu
            {
                MaMenu = menuDto.MaMenu,
                TenMenu = menuDto.TenMenu,
                Icon = menuDto.Icon,
                DuongDan = menuDto.DuongDan,
                ThuTu = maxThuTu + 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdByGuid
            };

            await _unitOfWork.Menus.AddAsync(menu);
            await _unitOfWork.CompleteAsync();

            return Ok();
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] MenuDto menuDto)
        {
            var menu = await _unitOfWork.Menus.GetByIdAsync(id);
            if (menu == null || menu.IsDeleted) return NotFound();

            menu.MaMenu = menuDto.MaMenu;
            menu.TenMenu = menuDto.TenMenu;
            menu.Icon = menuDto.Icon;
            menu.DuongDan = menuDto.DuongDan;
            menu.UpdatedAt = DateTime.UtcNow;
            menu.UpdatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            _unitOfWork.Menus.Update(menu);
            await _unitOfWork.CompleteAsync();
            return NoContent();
        }

        [HttpPut("thu-tu/{id}")]
        public async Task<IActionResult> UpdateThuTu(Guid id, [FromQuery] int thuTu)
        {
            if (thuTu <= 0)
                return BadRequest("Thứ tự phải là số nguyên dương.");

            var existingMenu = await _unitOfWork.Menus.GetByIdAsync(id);
            if (existingMenu == null || existingMenu.IsDeleted)
                return NotFound("Menu không tồn tại!");

            var allMenus = (await _unitOfWork.Menus.FindAsync(m => !m.IsDeleted))
                            .OrderBy(m => m.ThuTu)
                            .ToList();

            int maxThuTu = allMenus.Count;

            if (thuTu > maxThuTu)
                thuTu = maxThuTu;

            if (existingMenu.ThuTu == thuTu)
                return Ok();

            allMenus.RemoveAll(m => m.Id == existingMenu.Id);

            allMenus.Insert(thuTu - 1, existingMenu);

            for (int i = 0; i < allMenus.Count; i++)
            {
                var m = allMenus[i];
                int newThuTu = i + 1;

                if (m.ThuTu != newThuTu)
                {
                    m.ThuTu = newThuTu;
                    m.UpdatedAt = DateTime.UtcNow;
                    m.UpdatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    _unitOfWork.Menus.Update(m);
                }
            }

            await _unitOfWork.CompleteAsync();
            return Ok("Cập nhật thứ tự thành công.");
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var menu = await _unitOfWork.Menus.GetByIdAsync(id);
            if (menu == null || menu.IsDeleted) return NotFound();

            menu.IsDeleted = true;
            menu.DeletedAt = DateTime.UtcNow;
            menu.DeletedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            _unitOfWork.Menus.Update(menu);
            await _unitOfWork.CompleteAsync();

            return Ok(menu);
        }

        [HttpDelete("remove/{id}")]
        public async Task<IActionResult> Remove(Guid id)
        {
            var menu = await _unitOfWork.Menus.FirstOrDefaultAsync(x => x.Id == id, ignoreQueryFilters: true);
            if (menu == null) return NotFound();

            _unitOfWork.Menus.Remove(menu);
            await _unitOfWork.CompleteAsync();
            return Ok();
        }
    }
}
