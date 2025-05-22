using AiImageGeneratorApi.Interfaces;
using AiImageGeneratorApi.Models.DTOs;
using AiImageGeneratorApi.Models.Entities;
using AiImageGeneratorApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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

        [HttpGet]
        public async Task<IActionResult> GetAllMenu()
        {
            var data = await _unitOfWork.Menus.FindAsync(m => !m.IsDeleted);
            var tree = BuildMenuTree(data.OrderBy(m => m.ThuTu).ToList(), null);
            return Ok(tree);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdMenu(Guid id)
        {
            var menu = await _unitOfWork.Menus.GetByIdAsync(id);
            if (menu == null || menu.IsDeleted) return NotFound();
            return Ok(menu);
        }

        [HttpGet("theo-nguoi-dung")]
        public async Task<IActionResult> GetMenusForCurrentUser()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest();

            var userRoles = await _unitOfWork.UserRoles.FindAsync(ur => ur.UserId == userId);
            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();

            if (!roleIds.Any()) return Ok(new List<object>());

            var roleMenus = await _unitOfWork.RoleMenus.FindAsync(rm => roleIds.Contains(rm.RoleId) && rm.View);
            var menuIds = roleMenus.Select(rm => rm.MenuId).Distinct().ToList();

            if (!menuIds.Any()) return Ok(new List<object>());

            var menus = await _unitOfWork.Menus.FindAsync(m => !m.IsDeleted && menuIds.Contains(m.Id));
            var tree = BuildMenuTree(menus.OrderBy(m => m.ThuTu).ToList(), null);
            return Ok(tree);
        }

        private List<object> BuildMenuTree(List<Menu> menus, Guid? parentId)
        {
            return menus
                .Where(m => m.ParentId == parentId)
                .OrderBy(m => m.ThuTu)
                .Select(m => new
                {
                    m.Id,
                    m.MaMenu,
                    m.TenMenu,
                    m.Icon,
                    m.DuongDan,
                    m.ParentId,
                    children = BuildMenuTree(menus, m.Id)
                }).ToList<object>();
        }

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
                return BadRequest();
            }

            var menu = new Menu
            {
                MaMenu = menuDto.MaMenu,
                TenMenu = menuDto.TenMenu,
                Icon = menuDto.Icon,
                DuongDan = menuDto.DuongDan,
                ThuTu = maxThuTu + 1,
                ParentId = menuDto.ParentId,
                CreatedAt = DateTime.Now,
                CreatedBy = createdByGuid
            };

            await _unitOfWork.Menus.AddAsync(menu);
            await _unitOfWork.CompleteAsync();

            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMenu(Guid id, [FromBody] MenuDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var menu = await _unitOfWork.Menus.GetByIdAsync(id);
            if (menu == null || menu.IsDeleted)
                return NotFound();

            menu.MaMenu = dto.MaMenu;
            menu.TenMenu = dto.TenMenu;
            menu.Icon = dto.Icon;
            menu.DuongDan = dto.DuongDan;
            menu.ParentId = dto.ParentId;
            menu.UpdatedAt = DateTime.Now;
            menu.UpdatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            _unitOfWork.Menus.Update(menu);
            await _unitOfWork.CompleteAsync();
            return Ok("Cập nhật thành công.");
        }


        [HttpPut("thu-tu/{id}")]
        public async Task<IActionResult> UpdateThuTu(Guid id, [FromQuery] int thuTu)
        {
            if (thuTu <= 0)
                return BadRequest("Thứ tự phải là số nguyên dương.");

            var existingMenu = await _unitOfWork.Menus.GetByIdAsync(id);
            if (existingMenu == null || existingMenu.IsDeleted)
                return NotFound("Menu không tồn tại!");

            var allMenus = (await _unitOfWork.Menus.FindAsync(m => !m.IsDeleted && m.ParentId == existingMenu.ParentId))
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
                    m.UpdatedAt = DateTime.Now;
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
            menu.DeletedAt = DateTime.Now;
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