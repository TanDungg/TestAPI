namespace AiImageGeneratorApi.Models.DTOs
{
    public class RoleMenuPermissionDto
    {
        public Guid MenuId { get; set; }
        public string MaMenu { get; set; }
        public string TenMenu { get; set; }
        public string Icon { get; set; }
        public string DuongDan { get; set; }
        public int ThuTu { get; set; }

        // Các quyền
        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
        public bool Confirm { get; set; }
    }

}
