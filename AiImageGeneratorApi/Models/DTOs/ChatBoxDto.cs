using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiImageGeneratorApi.Models.DTOs
{
    /* Dto gửi tin nhắn */
    public class SendMessageDto
    {
        public Guid? NguoiNhanId { get; set; }
        public Guid? NhomId { get; set; }
        public string TinNhan { get; set; }
        public List<ChatFileDto>? List_Files { get; set; }
    }

    public class ChatFileDto
    {
        public Guid? Id { get; set; }
        public string FileUrl { get; set; }
    }

    /* Dto lấy danh sách user và group */
    public class ListChatDto
    {
        public Guid Id { get; set; }
        public bool IsNhom { get; set; }
        public string Ten { get; set; }
        public string HinhAnh { get; set; }
        public string TinNhanMoiNhat { get; set; }
        public string ThoiGianNhan { get; set; }
        public int SoLuongChuaXem { get; set; }
        public bool IsGui { get; set; }
        public bool IsThongBao { get; set; }
    }

    /* Dto lấy danh sách tin nhắn cả user và group */
    [Keyless]
    public class ChatInfoMessage
    {
        public Guid Id { get; set; }
        public string Ten { get; set; }
        public string HinhAnh { get; set; }
        public bool IsNhom { get; set; }
        public string? SoLuongThanhVien { get; set; }
        public string List_Ngays { get; set; }
    }
    public class ChatMessageGroupedByDateDto
    {
        public string Ngay { get; set; }
        public List<ChatMessageDto> List_Messages { get; set; }
    }
    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid NguoiGuiId { get; set; }
        public string? TenNguoiGui { get; set; }
        public string? HinhAnh { get; set; }
        public string? TinNhan { get; set; }
        public Guid? NguoiNhanId { get; set; }
        public string? TenNguoiNhan { get; set; }
        public string ThoiGianGui { get; set; }
        public bool IsSend { get; set; }
        public bool IsRead { get; set; }
        public bool IsThongBao { get; set; }
        public string? LoaiThongBao { get; set; }
        public string EncryptedMessage { get; set; }
        public string EncryptedKey { get; set; }
        public string IV { get; set; }               
        public List<ChatFileDto> List_Files { get; set; }
    }

    public class CreateGroupDto
    {
        public string TenNhom { get; set; }
        public string HinhAnh { get; set; }
        public List<Guid> ThanhViens { get; set; }
    }

    public class UpdateGroupDto
    {
        public string TenNhom { get; set; }
        public string HinhAnh { get; set; }
    }                 
}