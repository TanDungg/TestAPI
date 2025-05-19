namespace AiImageGeneratorApi.Models.DTOs
{
    public class UserRoleDto
    {
        public Guid UserId { get; set; }
        public List<Guid> RoleIds { get; set; }
    }
}
