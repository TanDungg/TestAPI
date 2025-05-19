namespace AiImageGeneratorApi.Models.DTOs
{
    public class RoleMenuDto
    {
        public Guid MenuId { get; set; }
        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
        public bool Confirm { get; set; }
    }
}
