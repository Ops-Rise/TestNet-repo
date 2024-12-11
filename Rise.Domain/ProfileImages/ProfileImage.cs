    using System;
    using Rise.Domain.Users;

    namespace Rise.Domain.ProfileImages;

    public class ProfileImage : Entity
    {
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        private byte[] imageBlob = Array.Empty<byte>();
        public byte[] ImageBlob
        {
            get => imageBlob;
            set
            {
                if (value.Length == 0)
                    throw new ArgumentException("Image data cannot be empty.", nameof(ImageBlob));
                if (value.Length > MaxImageSize)
                    throw new ArgumentException($"Image size exceeds the maximum allowed size of {MaxImageSize / 1024} KB.", nameof(ImageBlob));
                imageBlob = value;
            }
        }

        private string contentType = default!;
        public string ContentType
        {
            get => contentType;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(ContentType));
                if (!AllowedContentTypes.Contains(value.ToLower()))
                    throw new ArgumentException($"Content type '{value}' is not supported. Allowed types are: {string.Join(", ", AllowedContentTypes)}", nameof(ContentType));
                contentType = value;
            }
        }

        public static readonly int MaxImageSize = 2 * 1024 * 1024;
        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/gif" };

        public ProfileImage(int userId, byte[] imageBlob, string contentType)
        {
            UserId = userId;
            ImageBlob = imageBlob;
            ContentType = contentType;
        }
    }