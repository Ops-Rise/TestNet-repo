using System;
using Rise.Domain.ProfileImages;
using Rise.Domain.Users;
using Shouldly;

namespace Rise.Domain.Tests.ProfileImages;

public class ProfileImageShould
{
    private readonly int _testUserId = 1;
    private readonly byte[] _validImageBlob = new byte[2 * 1024 * 1024];
    private readonly string _validContentType = "image/jpeg";

    [Fact]
    public void BeCreated()
    {
        ProfileImage profileImage = new ProfileImage(_testUserId, _validImageBlob, _validContentType);

        profileImage.ShouldNotBeNull();
        profileImage.UserId.ShouldBe(_testUserId);
        profileImage.ImageBlob.ShouldBe(_validImageBlob);
        profileImage.ContentType.ShouldBe(_validContentType);
    }

    [Fact]
    public void NotBeCreatedWithEmptyImageBlob()
    {
        byte[] emptyImageBlob = Array.Empty<byte>();

        Action act = () =>
        {
            ProfileImage profileImage = new ProfileImage(_testUserId, emptyImageBlob, _validContentType);
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Image data cannot be empty. (Parameter 'ImageBlob')");
    }

    [Fact]
    public void NotBeCreatedWithExceedinglyLargeImageBlob()
    {
        byte[] largeImageBlob = new byte[ProfileImage.MaxImageSize + 1];

        Action act = () =>
        {
            ProfileImage profileImage = new ProfileImage(_testUserId, largeImageBlob, _validContentType);
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            $"Image size exceeds the maximum allowed size of {ProfileImage.MaxImageSize / 1024} KB. (Parameter 'ImageBlob')"
        );
    }

    [Fact]
    public void NotBeCreatedWithNullContentType()
    {
        string nullContentType = null!;

        Action act = () =>
        {
            ProfileImage profileImage = new ProfileImage(_testUserId, _validImageBlob, nullContentType);
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'ContentType')");
    }

    [Fact]
    public void NotBeCreatedWithUnsupportedContentType()
    {
        string invalidContentType = "image/bmp";

        Action act = () =>
        {
            ProfileImage profileImage = new ProfileImage(_testUserId, _validImageBlob, invalidContentType);
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            $"Content type '{invalidContentType}' is not supported. Allowed types are: image/jpeg, image/png, image/gif (Parameter 'ContentType')"
        );
    }

    [Fact]
    public void AllowSettingValidProperties()
    {
        ProfileImage profileImage = new ProfileImage(_testUserId, _validImageBlob, _validContentType);

        byte[] newImageBlob = new byte[1024 * 1024];
        string newContentType = "image/png";

        profileImage.ImageBlob = newImageBlob;
        profileImage.ContentType = newContentType;

        profileImage.ImageBlob.ShouldBe(newImageBlob);
        profileImage.ContentType.ShouldBe(newContentType);
    }

    [Fact]
    public void NotAllowSettingEmptyImageBlob()
    {
        ProfileImage profileImage = new ProfileImage(_testUserId, _validImageBlob, _validContentType);

        Action act = () =>
        {
            profileImage.ImageBlob = Array.Empty<byte>();
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Image data cannot be empty. (Parameter 'ImageBlob')");
    }

    [Fact]
    public void NotAllowSettingUnsupportedContentType()
    {
        ProfileImage profileImage = new ProfileImage(_testUserId, _validImageBlob, _validContentType);

        Action act = () =>
        {
            profileImage.ContentType = "application/pdf";
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            $"Content type 'application/pdf' is not supported. Allowed types are: image/jpeg, image/png, image/gif (Parameter 'ContentType')"
        );
    }
}