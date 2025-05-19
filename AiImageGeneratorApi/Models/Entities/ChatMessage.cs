namespace AiImageGeneratorApi.Models.Entities
{
    public class ChatMessage : BaseEntity
    {
        public Guid NguoiGuiId { get; set; }
        public string TinNhan { get; set; }
        public Guid? NhomId { get; set; }
        public Guid? NguoiNhanId { get; set; }
    }

    public class ChatGroup : BaseEntity
    {
        public string TenNhom { get; set; }
        public Guid TruongNhomId { get; set; }
    }

    public class ChatGroupMember : BaseEntity
    {
        public Guid NhomId { get; set; }
        public Guid ThanhVienId { get; set; }
    }

}