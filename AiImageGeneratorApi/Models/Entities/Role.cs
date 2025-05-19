namespace AiImageGeneratorApi.Models.Entities
{
    public class Role : BaseEntity
    {
        public string MaQuyen { get; set; }
        public string TenQuyen { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<RoleMenu> RoleMenus { get; set; }
    }
}
