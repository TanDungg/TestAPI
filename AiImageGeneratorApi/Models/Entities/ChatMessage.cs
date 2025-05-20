namespace AiImageGeneratorApi.Models.Entities
{
    public class ChatMessage : BaseEntity
    {
        public Guid NguoiGuiId { get; set; }
        public string TinNhan { get; set; }
        public Guid? NhomId { get; set; }
        public Guid? NguoiNhanId { get; set; }
        public bool IsRead { get; set; } = false;
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

    public class ChatMessageRead
    {
        public Guid Id { get; set; }
        public Guid TinNhanId { get; set; }
        public Guid ThanhVienId { get; set; }
        public DateTime ThoiGianXem { get; set; }

        public ChatMessage Message { get; set; }
        public User User { get; set; }
    }


}