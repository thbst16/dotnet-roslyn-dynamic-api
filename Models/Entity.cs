namespace dotnet_roslyn_dynamic_api.Models
{
    public class Entity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Fields {get; set;}
        public bool isActive { get; set; }
    }
}