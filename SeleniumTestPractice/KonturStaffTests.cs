using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using DotNetEnv;

namespace SeleniumTestPractice;

[TestFixture]
public class KonturStaffTests
{
    private ChromeDriver _driver;
    private WebDriverWait _wait;
    private const string BaseUrl = "https://staff-testing.testkontur.ru";

    [OneTimeSetUp]
    public void GlobalSetup() => Env.Load();

    [SetUp]
    public void Setup()
    {
        _driver = new ChromeDriver();
        _driver.Manage().Window.Maximize();
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    }

    [TearDown]
    public void Teardown()
    {
        _driver.Quit();
        _driver.Dispose();
    }

    private void Login()
    {
        _driver.Navigate().GoToUrl(BaseUrl);

        var loginInput = _wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Username")));
        
        loginInput.SendKeys(Environment.GetEnvironmentVariable("STAFF_LOGIN"));
        
        _driver.FindElement(By.Id("Password")).SendKeys(Environment.GetEnvironmentVariable("STAFF_PASSWORD"));
        
        _driver.FindElement(By.Name("button")).Click();
        
        _wait.Until(ExpectedConditions.UrlContains("/news"));
    }

    [Test]
    public void User_Can_Successfully_Log_In()
    {
        Login();
        
        var isTitleCorrect = _wait.Until(ExpectedConditions.TitleContains("Новости"));
        
        Assert.That(isTitleCorrect, Is.True);
    }
    
    [Test]
    public void User_Can_Logout_Successfully()
    {
        Login();

        var avatar = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("[data-tid='Avatar']")));
        avatar.Click();

        var logoutMenuButton = _wait.Until(ExpectedConditions.ElementToBeClickable(
            By.XPath("//*[contains(text(), 'Выйти')]")));
        logoutMenuButton.Click();

        var returnButton = _wait.Until(ExpectedConditions.ElementToBeClickable(
            By.XPath("//*[contains(text(), 'Вернуться')]")));
        returnButton.Click();
        
        var usernameField = _wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Username")));
        Assert.Multiple(() =>
        {
            Assert.That(usernameField.Displayed, Is.True);
            Assert.That(_driver.Url, Does.Contain("Account/Login"));
        });
    }
    
    [Test]
    public void User_Can_Edit_About_Me_Field()
    {
        Login();
        _driver.Navigate().GoToUrl($"{BaseUrl}/profile/settings/edit");
        _wait.Until(ExpectedConditions.UrlContains("/profile/settings/edit"));
        
        var aboutMeInput = _wait.Until(ExpectedConditions.ElementIsVisible(
            By.XPath("//div[label[contains(text(), 'Чем занимаюсь')]]//textarea")));

        _driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", aboutMeInput);

        aboutMeInput.Click();
        aboutMeInput.SendKeys(Keys.Control + "a");
        aboutMeInput.SendKeys(Keys.Backspace);
    
        const string newText = "Пишу автотесты на селениум";
        aboutMeInput.SendKeys(newText);
        
        var saveButton = _wait.Until(ExpectedConditions.ElementToBeClickable(
            By.XPath("//button[contains(., 'Сохранить')]")));
        saveButton.Click();

        _wait.Until(ExpectedConditions.UrlContains("/profile"));
    
        var result = _wait.Until(ExpectedConditions.ElementIsVisible(
            By.XPath($"//*[contains(text(), '{newText}')]")));

        Assert.That(result.Displayed, Is.True);
    }
    
    [Test]
    public void User_Can_Search_Admin_Profile_Successfully()
    {
        Login();
        var searchLocator = By.XPath("//input[contains(@placeholder, 'Поиск')] | //*[contains(@data-tid, 'Search')]//input");
    
        var searchInput = _wait.Until(ExpectedConditions.ElementExists(searchLocator));
        _driver.ExecuteScript("arguments[0].click();", searchInput);
        _driver.ExecuteScript("arguments[0].focus();", searchInput);

        new OpenQA.Selenium.Interactions.Actions(_driver)
            .SendKeys("Admin")
            .Pause(TimeSpan.FromMilliseconds(500))
            .SendKeys(Keys.Enter)
            .Perform();
        
        var adminLink = _wait.Until(ExpectedConditions.ElementToBeClickable(
            By.XPath("//a[contains(., 'Admin')] | //*[contains(@data-tid, 'SearchResult')]//a")));
    
        adminLink.Click();

        var employeeName = _wait.Until(ExpectedConditions.ElementIsVisible(
            By.CssSelector("[data-tid='EmployeeName']")));

        Assert.That(employeeName.Text, Is.EqualTo("Admin"));
    }
    
    [Test]
    [Description("Создание нового сообщества и проверка перехода на страницу управления")]
    public void User_Can_Create_New_Community()
    {
        Login();
        _driver.Navigate().GoToUrl($"{BaseUrl}/communities");

        var mainCreateBtn = _wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[contains(., 'СОЗДАТЬ')]")));
        mainCreateBtn.Click();

        var communityName = string.Concat("Автотест ", Guid.NewGuid().ToString().AsSpan(0, 8));

        var nameInput = _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-tid='Name'] textarea")));
        nameInput.SendKeys(communityName);

        var modalCreateBtn = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("[data-tid='CreateButton'] button")));
        modalCreateBtn.Click();

        _wait.Until(ExpectedConditions.UrlMatches(@"/communities/[\w-]+"));

        var titleElement = _wait.Until(d => {
            var el = d.FindElement(By.CssSelector("[data-tid='Title']"));
            return el.Text.Contains("Управление сообществом") ? el : null;
        });
        
        Assert.That(titleElement.Text, Does.Contain("Управление сообществом"));
        Assert.That(titleElement.Text, Does.Contain(communityName));
        
    }
    
    [Test]
    public void User_Can_Navigate_Through_Company_Structure()
    {
        Login();
        _driver.Navigate().GoToUrl($"{BaseUrl}/company");

        var administrationLink = _wait.Until(ExpectedConditions.ElementToBeClickable(
            By.XPath("//a[@data-tid='Link' and contains(., 'Администрация')]")));
        administrationLink.Click();

        var financeDepartmentLink = _wait.Until(ExpectedConditions.ElementToBeClickable(
            By.XPath("//a[@data-tid='Link' and contains(., 'Департамент финансов')]")));
        financeDepartmentLink.Click();
        
        var isTitleChanged = _wait.Until(ExpectedConditions.TextToBePresentInElementLocated(
            By.CssSelector("[data-tid='Title']"), "Департамент финансов"));

        var pageTitle = _driver.FindElement(By.CssSelector("[data-tid='Title']"));

        Assert.Multiple(() =>
        {
            Assert.That(isTitleChanged, Is.True);
            Assert.That(pageTitle.Text, Is.EqualTo("Департамент финансов"));
        });
    }
}