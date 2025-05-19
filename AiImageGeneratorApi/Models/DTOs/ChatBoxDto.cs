namespace AiImageGeneratorApi.Models.DTOs
{
    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid NguoiGuiId { get; set; }
        public string TenNguoiGui { get; set; }
        public string TinNhan { get; set; }
        public Guid? NguoiNhanId { get; set; }
        public string TenNguoiNhan { get; set; }
        public string ThoiGianGui { get; set; }
    }

    public class SendMessageDto
    {
        public Guid Id { get; set; }
        public Guid? NguoiNhanId { get; set; }
        public Guid? NhomId { get; set; }
        public string TinNhan { get; set; }
    }

    public class EditMessageDto
    {
        public string TinNhan { get; set; }
    }

    public class CreateGroupDto
    {
        public string TenNhom { get; set; }
        public List<Guid> ThanhViens { get; set; }
    }

    public class UpdateGroupDto
    {
        public string TenNhom { get; set; }
    }
}