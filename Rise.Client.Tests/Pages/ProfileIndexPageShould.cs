using System;
using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;
using System.Threading.Tasks;
using NSubstitute;
using Rise.Client.Services;
using Rise.Shared.Addresses;
using Rise.Shared.Users;
using Rise.Shared.ProfileImages;
using Xunit;
using Xunit.Abstractions;

namespace Rise.Client.Pages.Profile
{
    public class ProfileIndexPageShould : TestContext
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly IUserService _userServiceMock;
        private readonly IProfileImageService _profileImageServiceMock;
        private readonly ProfileStateService _profileStateServiceMock;
        private readonly AuthenticationStateProvider _authenticationStateProviderMock;

        public ProfileIndexPageShould(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;

            JSInterop.Mode = JSRuntimeMode.Loose;

            _userServiceMock = Substitute.For<IUserService>();
            _profileImageServiceMock = Substitute.For<IProfileImageService>();
            _authenticationStateProviderMock = Substitute.For<AuthenticationStateProvider>();
            _profileStateServiceMock = Substitute.For<ProfileStateService>();

            Services.AddScoped(_ => _userServiceMock);
            Services.AddScoped(_ => _profileImageServiceMock);
            Services.AddScoped(_ => _authenticationStateProviderMock);
            Services.AddScoped(_ => _profileStateServiceMock);

            Services.AddMudServices(options =>
            {
                options.PopoverOptions.CheckForPopoverProvider = false;
            });
        }

        private void SetupUserProfile()
        {
            // Mock user profile
            var userProfile = new UserDto.Index
            {
                Id = 1,
                Firstname = "John",
                Lastname = "Doe",
                BirthDay = DateTime.Parse("1990-01-01"),
                Email = "john.doe@example.com",
                PhoneNumber = "0471234567",
                Address = new AddressDto { Street = "Hoofdstraat", HouseNumber = "1", PostalCode = "1000", City = "Brussel", UnitNumber = "3" }
            };
            
            var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim("sub", "userId")
            })));

            _authenticationStateProviderMock.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));
            _userServiceMock.GetUserAsync("userId").Returns(Task.FromResult(userProfile));           
        }

        [Fact]
        public void ProfilePage_RendersUserDetails_WhenUserProfileIsLoaded()
        {
            SetupUserProfile();

            // Arrange
            var component = RenderComponent<Index>();

            // Act & Assert
            Assert.Contains("Profiel", component.Markup);
            Assert.Contains("John", component.Markup);
            Assert.Contains("Doe", component.Markup);
            Assert.Contains("0471234567", component.Markup);
            Assert.Contains("Hoofdstraat 1 bus 3 1000 Brussel", component.Markup);
            Assert.Contains("Bewerk Profiel", component.Markup);
        }

        [Fact]
        public void ProfilePage_NavigatesToEditProfile_WhenEditButtonIsClicked()
        {
            SetupUserProfile();

            // Arrange
            var component = RenderComponent<Index>();
            var editButton = component.Find("button");
            var navMan = Services.GetRequiredService<FakeNavigationManager>();

            // Act
            editButton.Click();

            // Assert
            Assert.Equal("http://localhost/profile/edit", navMan.Uri);
        }

        [Fact]
        public void ProfilePage_RendersDefaultImage_WhenProfileImageIsNotAvailable()
        {
            SetupUserProfile();

            // Arrange
            var component = RenderComponent<Index>();

            // Act
            var profileImage = component.Find("img");

            // Assert
            Assert.Equal("/Images/default_profile_image.png", profileImage.GetAttribute("src"));
        }

        [Fact]
        public void ProfilePage_RendersProfileImage_WhenAvailable()
        {
            SetupUserProfile();

            //Create a mock base64-encoded image
            var mockProfileImage = new ProfileImageDto.Detail
            {
                Id = 1,
                UserId = 1,
                ContentType = "image/png",
                ImageBlob = new byte[] { 0x89, 0x50, 0x4E, 0x47 } // Example image data
            };

            //var base64Image = $"data:{mockProfileImage.ContentType};base64,{Convert.ToBase64String(mockProfileImage.ImageBlob)}";

            // Arrange
            //_profileStateServiceMock.UpdateAvatar(base64Image);
            _profileImageServiceMock.GetProfileImageAsync(1).Returns(Task.FromResult(mockProfileImage));

            // Act
            var component = RenderComponent<Index>();

            // Assert
            var profileImage = component.Find("img");
            Assert.Contains("data:image", profileImage.GetAttribute("src"));
        }
    }
}