//broke
/*using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class EditProfileFormAndFlowTests
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
    public async Task EditProfileForm_EditAndSave_ChangesSavedSuccessfully()
    {
        await page.GotoAsync("https://localhost:5003/profile/edit");

        // Ensure the EditProfile page is loaded
        await Assertions.Expect(page).ToHaveURLAsync("https://localhost:5003/profile/edit");
        await Assertions.Expect(page.Locator("h5")).ToContainTextAsync("Profiel bewerken");

        // Fill in new profile details
        await page.GetByLabel("Voornaam").ClickAsync();
        await page.GetByLabel("Voornaam").FillAsync("UpdatedFirstName");
        await page.GetByLabel("Achternaam").ClickAsync();
        await page.GetByLabel("Achternaam").FillAsync("UpdatedLastName");
        await page.GetByLabel("Telefoonnummer").ClickAsync();
        await page.GetByLabel("Telefoonnummer").FillAsync("0475123456");
        await page.GetByLabel("Straat").ClickAsync();
        await page.GetByLabel("Straat").FillAsync("UpdatedStreet");
        await page.GetByLabel("Stad").ClickAsync();
        await page.GetByLabel("Stad").FillAsync("UpdatedCity");
        await page.GetByLabel("Postcode").ClickAsync();
        await page.GetByLabel("Postcode").FillAsync("9999");
        await page.GetByLabel("Huisnummer").ClickAsync();
        await page.GetByLabel("Huisnummer").FillAsync("123");
        await page.GetByLabel("Busnummer").ClickAsync();
        await page.GetByLabel("Busnummer").FillAsync("4B");

        // Click the Save button to submit the form
        await page.GetByRole(AriaRole.Button, new() { Name = "Opslaan" }).ClickAsync();

        // Wait for the update to be processed and check if we are redirected to the profile page
        await page.WaitForURLAsync("https://localhost:5003/profile");
        
        // Verify that the changes are saved correctly
        await Assertions.Expect(page.Locator("h5")).ToContainTextAsync("Profiel");
        await Assertions.Expect(page.Locator("div")).ToContainTextAsync("UpdatedFirstName UpdatedLastName");
        await Assertions.Expect(page.Locator("div")).ToContainTextAsync("UpdatedStreet 123 4B");
        await Assertions.Expect(page.Locator("div")).ToContainTextAsync("UpdatedCity, 9999");
        await Assertions.Expect(page.Locator("div")).ToContainTextAsync("0475123456");

        // Optionally, verify the profile picture upload
        // Assuming a default picture is shown until updated, you can click on the upload button to simulate the profile picture change
        var fileInput = page.Locator("input[type='file']");
        await fileInput.SetInputFilesAsync("path/to/test/image.jpg");
        
        // You could also verify if the avatar has been updated
        await page.WaitForSelectorAsync(".mud-avatar img[src*='image.jpg']");  // Adjust the selector as needed
    }

    [Test]
    public async Task EditProfileForm_InvalidPhoneNumber_ShowsErrorAndStaysOnEditPage()
    {
        await page.GotoAsync("https://localhost:5003/profile/edit");

        // Fill in invalid phone number
        await page.GetByLabel("Telefoonnummer").ClickAsync();
        await page.GetByLabel("Telefoonnummer").FillAsync("invalid-phone");

        // Attempt to submit the form
        await page.GetByRole(AriaRole.Button, new() { Name = "Opslaan" }).ClickAsync();

        // Check for error message
        await Assertions.Expect(page.Locator("div")).ToContainTextAsync("Ongeldig Belgisch telefoonnummer");

        // Verify that the page stays on the edit profile form
        await Assertions.Expect(page).ToHaveURLAsync("https://localhost:5003/profile/edit");
    }

    [Test]
    public async Task CancelChanges_BackToProfilePage()
    {
        await page.GotoAsync("https://localhost:5003/profile/edit");

        // Click the cancel button
        await page.GetByRole(AriaRole.Button, new() { Name = "Annuleren" }).ClickAsync();

        // Verify that we are redirected back to the profile page
        await page.WaitForURLAsync("https://localhost:5003/profile");
    }
}*/