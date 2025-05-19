namespace AiImageGeneratorApi.Models.Entities
{
    public class GeneratedImage : BaseEntity
    {
        public string Prompt { get; set; }
        public string ImageUrl { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
    }

}
