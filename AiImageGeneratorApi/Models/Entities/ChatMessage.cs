namespace AiImageGeneratorApi.Models.Entities
{
    public class ChatMessage : BaseEntity
    {
        public Guid NguoiGuiId { get; set; }
        public string TinNhan { get; set; }
        public Guid? NhomId { get; set; } = null;
        public Guid? NguoiNhanId { get; set; } = null;
        public bool IsRead { get; set; } = false;
        public bool IsThongBao { get; set; } = false;
        public string? LoaiThongBao { get; set; } = null;
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