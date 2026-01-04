namespace DemoCiCdAzureApi.Entities
{
    public record User
    {
        public int Id { get; init; }
        public required string Username { get; init; }
        public required string Email { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}