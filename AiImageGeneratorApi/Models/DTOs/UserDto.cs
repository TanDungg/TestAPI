namespace AiImageGeneratorApi.Models.DTOs
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string HoVaTen { get; set; }
        public string Sdt { get; set; }
        public string Email { get; set; }
        public string HinhAnh { get; set; }
        public int SoNhomChung { get; set; }
        public List<string> ListHinhAnh { get; set; }
    }

}
