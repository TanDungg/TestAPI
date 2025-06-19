namespace AiImageGeneratorApi.Models.Entities
{
    public class User : BaseEntity
    {
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public string HoVaTen { get; set; }
        public string? DiaChi { get; set; }
        public string? Email { get; set; }
        public string? Sdt { get; set; }
        public string HinhAnh { get; set; }
        public string PublicKey { get; set; } 
        public string PrivateKey { get; set; }
        public ICollection<ChatMessage> MessagesSent { get; set; }
        public ICollection<ChatMessage> MessagesReceived { get; set; }
    }

}
