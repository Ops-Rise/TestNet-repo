//broke
/*using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class RegistrationFormAndFlowTests
{
    private IPlaywright playwright;
    private IBrowser browser;
    private IBrowserContext context;
    private IPage page;

    [OneTimeSetUp] // Gebruik OneTimeSetUp om Playwright eenmalig op te zetten
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

    [TearDown] // Zorg ervoor dat je de context sluit na elke test
    public async Task TearDown()
    {
        await context.CloseAsync();
    }

    [OneTimeTearDown] // Zorg ervoor dat je de browser sluit na alle tests
    public async Task OneTimeTearDown()
    {
        await browser.CloseAsync();
        playwright.Dispose(); // Zorg ervoor dat Playwright wordt vrijgegeven
    }

    [Test]
    public async Task NewUserSignUp_RegisterCorrect_GoesToBookingPage_LogoutLogin_GoesToHomePage()
    {
        await page.GotoAsync("https://localhost:5003");

        await page.Locator("div")
            .Filter(new() { HasTextRegex = new Regex("^Log in$") })
            .ClickAsync();
        await page.Locator("div")
            .Filter(new() { HasTextRegex = new Regex("^Log in$") })
            .ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Log in" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Sign up" }).ClickAsync();
        await page.GetByLabel("Email address").ClickAsync();
        string randomEmail =
            "test"
            + new Random().Next(1000, 9999999)
            + "@"
            + "test"
            + new Random().Next(1000, 9999999)
            + ".com";
        string password = "TestWachtwoord1";
        await page.GetByLabel("Email address").FillAsync(randomEmail);
        await page.GetByLabel("Email address").PressAsync("Tab");
        await page.GetByLabel("Password").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue", Exact = true })
            .ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Accept" }).ClickAsync();
        //API call to get the user
        // no user -> save user with auth0UserId and email
        // -> registration page
        await Assertions.Expect(page).ToHaveURLAsync("https://localhost:5003/registration");
        await Assertions.Expect(page.Locator("#title")).ToContainTextAsync("Registratie");
        await Assertions.Expect(page.Locator("#app")).ToContainTextAsync("Log out");

        
        await page.GetByLabel("Voornaam").ClickAsync();
        await page.GetByLabel("Voornaam").FillAsync("TestVoornaam");
        await page.GetByLabel("Voornaam").PressAsync("Tab");
        await page.GetByLabel("Achternaam").FillAsync("TestAchternaam");
        await page.GetByLabel("Achternaam").PressAsync("Tab");
        await page.GetByLabel("Straatnaam").FillAsync("TestStraat");
        await page.GetByLabel("Straatnaam").PressAsync("Tab");
        await page.GetByLabel("Nummer").FillAsync("11");
        await page.GetByLabel("Nummer").PressAsync("Tab");
        await page.GetByLabel("Bus").PressAsync("Tab");
        await page.GetByLabel("Postcode").FillAsync("9875");
        await page.GetByLabel("Postcode").PressAsync("Tab");
        await page.GetByLabel("Plaats").FillAsync("TestPlaats");
        await page.GetByLabel("Plaats").PressAsync("Tab");
        await page.GetByLabel("Telefoon").FillAsync("0477889911");
        await page.GetByLabel("Telefoon").PressAsync("Tab");
        await page.GetByLabel("Open Date Picker").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "2024", Exact = true }).ClickAsync();
        await page.GetByText("1985").ClickAsync();
        await page.GetByLabel("juni").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "1", Exact = true })
            .First.ClickAsync();
        await page.GetByLabel("Ik ga akkoord met de algemene").CheckAsync();
        await page.GetByLabel("Ik ga akkoord met het").CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Registreer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync("https://localhost:5003/bookings");
        //logout login
        await page.GetByRole(AriaRole.Button, new() { Name = "Log out" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Log in" }).ClickAsync();
        await page.GetByLabel("Email address").ClickAsync();
        await page.GetByLabel("Email address").FillAsync(randomEmail);
        await page.GetByLabel("Email address").PressAsync("Tab");
        await page.GetByLabel("Password").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue", Exact = true })
            .ClickAsync();
        //API call to get the user
        // user is not null, property isRegistrationComplete = true
        //should go to home page
        await Assertions.Expect(page).ToHaveURLAsync("https://localhost:5003/");
    }

    [Test]
    public async Task NewUserSignUp_WrongPhoneNumber_ShowsErrorAndShouldStayOnSamePage_LogoutLogin_GoesAgainToRegistrationPage()
    {
        await page.GotoAsync("https://localhost:5003");

  await page.GetByRole(AriaRole.Toolbar).GetByRole(AriaRole.Link, new() { Name = "Log in" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Sign up" }).ClickAsync();
        await page.GetByLabel("Email address").ClickAsync();
        string randomEmail =
            "test"
            + new Random().Next(1000, 9999999)
            + "@"
            + "test"
            + new Random().Next(1000, 9999999)
            + ".com";
        string password = "TestWachtwoord1";
        await page.GetByLabel("Email address").FillAsync(randomEmail);
        await page.GetByLabel("Email address").PressAsync("Tab");
        await page.GetByLabel("Password").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue", Exact = true })
            .ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Accept" }).ClickAsync();
        //API call to get the user
        // no user -> save user with auth0UserId and email
        // -> registration page
        await Assertions.Expect(page).ToHaveURLAsync("https://localhost:5003/registration");
        await Assertions.Expect(page.Locator("#title")).ToContainTextAsync("Registratie");
        await Assertions.Expect(page.Locator("#app")).ToContainTextAsync("Log out");


        await page.GetByLabel("Voornaam").ClickAsync();
        await page.GetByLabel("Voornaam").FillAsync("TestVoornaam");
        await page.GetByLabel("Voornaam").PressAsync("Tab");
        await page.GetByLabel("Achternaam").FillAsync("TestAchternaam");
        await page.GetByLabel("Achternaam").PressAsync("Tab");
        await page.GetByLabel("Straatnaam").FillAsync("TestStraat");
        await page.GetByLabel("Straatnaam").PressAsync("Tab");
        await page.GetByLabel("Nummer").FillAsync("11");
        await page.GetByLabel("Nummer").PressAsync("Tab");
        await page.GetByLabel("Bus").PressAsync("Tab");
        await page.GetByLabel("Postcode").FillAsync("9875");
        await page.GetByLabel("Postcode").PressAsync("Tab");
        await page.GetByLabel("Plaats").FillAsync("TestPlaats");
        await page.GetByLabel("Plaats").PressAsync("Tab");
        await page.GetByLabel("Telefoon").FillAsync("0277889911");
        await page.GetByLabel("Telefoon").PressAsync("Tab");


        //should show error
        await Assertions.Expect(page.GetByText("Ongeldig Belgisch")).ToBeVisibleAsync();
        await Assertions
            .Expect(page.Locator("form"))
            .ToContainTextAsync(
                "Ongeldig Belgisch telefoonnummer. Het moet beginnen met 04 (10 cijfers) of +32 (11 cijfers)."
            );


        await page.GetByLabel("Open Date Picker").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "2024", Exact = true }).ClickAsync();
        await page.GetByText("1985").ClickAsync();
        await page.GetByLabel("juni").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "1", Exact = true })
            .First.ClickAsync();
        await page.GetByLabel("Ik ga akkoord met de algemene").CheckAsync();
        await page.GetByLabel("Ik ga akkoord met het").CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Registreer" }).ClickAsync();

        //should stay on the same page
        await Assertions.Expect(page).ToHaveURLAsync("https://localhost:5003/registration");

        //logout login
        await page.GetByRole(AriaRole.Button, new() { Name = "Log out" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Log in" }).ClickAsync();
        await page.GetByLabel("Email address").ClickAsync();
        await page.GetByLabel("Email address").FillAsync(randomEmail);
        await page.GetByLabel("Email address").PressAsync("Tab");
        await page.GetByLabel("Password").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue", Exact = true })
            .ClickAsync();
        //API call to get the user
        // user is not null, but property isRegistrationComplete = false
        // -> registration page
        //should go again to registration page
        await Assertions.Expect(page).ToHaveURLAsync("https://localhost:5003/registration");
    }
}
*/