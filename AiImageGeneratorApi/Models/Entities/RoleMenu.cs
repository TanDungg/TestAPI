namespace AiImageGeneratorApi.Models.Entities
{
    public class RoleMenu : BaseEntity
    {
        public Guid RoleId { get; set; }
        public Guid MenuId { get; set; }

        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
        public bool Confirm { get; set; }

        public Role Role { get; set; }
        public Menu Menu { get; set; }
    }

}
