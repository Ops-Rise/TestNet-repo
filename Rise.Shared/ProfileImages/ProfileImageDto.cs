namespace Rise.Shared.ProfileImages;

public abstract class ProfileImageDto
{
    public class Index
    {
        public required int Id { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public byte[] ImageBlob { get; set; } = Array.Empty<byte>();
    }

    public class Detail
    {
        public required int Id { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public byte[] ImageBlob { get; set; } = Array.Empty<byte>();
        public int UserId { get; set; }
    }

    public class Mutate
    {
        public string ContentType { get; set; } = string.Empty;
        public byte[] ImageBlob { get; set; } = Array.Empty<byte>();
        public int UserId { get; set; }
    }

    public class Edit
    {
        public required int Id { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public byte[] ImageBlob { get; set; } = Array.Empty<byte>();
    }
}
