namespace AiImageGeneratorApi.Models.DTOs
{
    public class MenuDto
    {
        public Guid Id { get; set; }
        public string MaMenu { get; set; }
        public string TenMenu { get; set; }
        public string Icon { get; set; }
        public string DuongDan { get; set; }
        public Guid? ParentId { get; set; } 
    }

}
