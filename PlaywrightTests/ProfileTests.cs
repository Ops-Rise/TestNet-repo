//broke

/*using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class ProfilePageTests
{
    private IPlaywright playwright;
    private IBrowser browser;
    private IBrowserContext context;
    private IPage page;

    [OneTimeSetUp] // Setup Playwright once for all tests
    public async Task OneTimeSetUp()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = false }
        );
    }

    [SetUp]
    public async Task SetUp()
    {
        var browserNewContextOptions = new BrowserNewContextOptions { IgnoreHTTPSErrors = true };
        context = await browser.NewContextAsync(browserNewContextOptions);
        page = await context.NewPageAsync();
    }

    [TearDown] // Close the context after each test
    public async Task TearDown()
    {
        await context.CloseAsync();
    }

    [OneTimeTearDown] // Close the browser after all tests
    public async Task OneTimeTearDown()
    {
        await browser.CloseAsync();
        playwright.Dispose(); // Dispose Playwright
    }

    [Test]
    public async Task ProfilePage_LoadsAndDisplaysProfileInfo()
    {
        await page.GotoAsync("https://localhost:5003/profile");

        // Ensure the Profile page is loaded
        await Assertions.Expect(page).ToHaveURLAsync("https://localhost:5003/profile");
        await Assertions.Expect(page.Locator("h5")).ToContainTextAsync("Profiel");

        // Check if profile details are displayed (name, email, phone number)
        var firstName = await page.Locator("strong:has-text('Voornaam:')");
        var lastName = await page.Locator("strong:has-text('Achternaam:')");
        var email = await page.Locator("strong:has-text('E-mail:')");
        var phone = await page.Locator("strong:has-text('Telefoonnummer:')");

        Assert.IsNotNull(firstName, "First name should be displayed.");
        Assert.IsNotNull(lastName, "Last name should be displayed.");
        Assert.IsNotNull(email, "Email should be displayed.");
        Assert.IsNotNull(phone, "Phone number should be displayed.");

        // Check if the profile image is either the default image or the user's uploaded image
        var profileImage = await page.QuerySelectorAsync("img[src^='/Images/default_profile_image.png']");
        var uploadedImage = await page.QuerySelectorAsync("img[src^='data:']");

        // If the user has an uploaded profile image, it should be visible
        if (uploadedImage != null)
        {
            Assert.IsNotNull(uploadedImage, "Uploaded profile image should be displayed.");
        }
        else
        {
            Assert.IsNotNull(profileImage, "Default profile image should be displayed.");
        }
    }

    [Test]
    public async Task NavigateToEditProfilePage_OnClick()
    {
        await page.GotoAsync("https://localhost:5003/profile");

        // Ensure the Profile page is loaded
        await Assertions.Expect(page).ToHaveURLAsync("https://localhost:5003/profile");

        // Click on the "Bewerk Profiel" button
        var editButton = await page.QuerySelectorAsync("button:has-text('Bewerk Profiel')");
        Assert.IsNotNull(editButton, "'Bewerk Profiel' button should be visible.");

        // Click the button and wait for navigation to the edit profile page
        await editButton.ClickAsync();
        await page.WaitForNavigationAsync();

        // Verify that the user is navigated to the profile edit page
        Assert.AreEqual("https://localhost:5003/profile/edit", page.Url);
    }

    [Test]
    public async Task ProfilePage_ShowsErrorWhenProfileDataFailsToLoad()
    {
        // Simulate an error when loading the profile page
        await page.GotoAsync("https://localhost:5003/profile");

        // Assuming there's an error message or a placeholder when the profile data fails to load
        await Assertions.Expect(page.Locator("div")).ToContainTextAsync("Er is een fout opgetreden bij het laden van uw profiel.");
    }
}
*/