using Rise.Client.Auth;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Components;

namespace Rise.Client.Pages;

public class LoginDisplayShould : TestContext
{
	public LoginDisplayShould(ITestOutputHelper outputHelper)
	{
		Services.AddXunitLogger(outputHelper);
	}

	[Fact]
	public void UnauthenticatedAndUnauthorizedShouldRenderLoginDisplayWithLogInLink()
	{
		// Arrange
		this.AddTestAuthorization();

		// Act
		var cut = RenderComponent<LoginDisplay>();
		var loginLink = cut.Find("a[href='authentication/login']");

		// Assert
		Assert.NotNull(loginLink);
		Assert.Contains("Log in", cut.Markup);

	}

	[Fact]
	public void AuthenticatedAndAuthorizedShouldRenderLoginDisplayWithNameMessageAndLogOutButton()
	{
		// Arrange
		var authContext = this.AddTestAuthorization();
		authContext.SetAuthorized("TEST USER");
		var ctx = new TestContext();
		var nav = ctx.Services.GetRequiredService<NavigationManager>();

		// Act
		var cut = RenderComponent<LoginDisplay>();
		var logoutButton = cut.Find("button");

		// Assert
		Assert.NotNull(logoutButton);
		Assert.Equal("Log out", logoutButton.TextContent);
		Assert.Contains("Hello, TEST USER!", cut.Markup);	
	}
}

