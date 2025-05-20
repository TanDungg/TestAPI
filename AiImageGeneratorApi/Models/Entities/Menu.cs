namespace AiImageGeneratorApi.Models.Entities
{
    public class Menu : BaseEntity
    {
        public string MaMenu { get; set; }
        public string TenMenu { get; set; }
        public string Icon { get; set; }
        public string DuongDan { get; set; }
        public int ThuTu { get; set; }

        public Guid? ParentId { get; set; }
        public Menu Parent { get; set; }
        public ICollection<Menu> Children { get; set; }
    }


}