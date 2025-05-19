﻿namespace AiImageGeneratorApi.Models.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }  
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }  
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }  
    }
}
