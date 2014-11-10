using System;
using System.Security.Cryptography;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;


namespace TestNunit
{
    [TestFixture("firefox", "33", "Windows 7")]
//    [TestFixture("chrome", "38", "Windows 7")]
//    [TestFixture("internet explorer", "11", "Windows 7")]
//    [TestFixture("firefox", "33", "OS X 10.9")]
//    [TestFixture("chrome", "38", "OS X 10.9")]
//    [TestFixture("safari", "7", "OS X 10.9")]
    class SauceLabsTest
    {
        private RemoteWebDriver _driver;

        private readonly string _browser;
        private readonly string _version;
        private readonly string _platform;

        private const bool RunLocally = false;

        public SauceLabsTest(string browser, string version, string platform)
        {
            _browser = browser;
            _version = version;
            _platform = platform;
        }

        private DesiredCapabilities ResolveCapabilities()
        {
            DesiredCapabilities caps = null;
            if (_browser == "firefox")
            {
                caps = DesiredCapabilities.Firefox();
            }
            else if (_browser == "chrome")
            {
                caps = DesiredCapabilities.Chrome();
            }
            else if(_browser == "internet explorer")
            {
                caps = DesiredCapabilities.InternetExplorer();
            }
            else if (_browser == "safari")
            {
                caps = DesiredCapabilities.Safari();
            }

            Assert.IsNotNull(caps, "Unknown browser");
            caps.SetCapability(CapabilityType.Platform, _platform);
            caps.SetCapability(CapabilityType.Version, _version);
            caps.SetCapability("browserName", _browser);
            return caps;
        }


        [SetUp]
        public void Init()
        {
            var capabilities = ResolveCapabilities();
            capabilities.SetCapability("name", TestContext.CurrentContext.Test.Name);
            capabilities.SetCapability("username", Properties.Settings.Default.SauceLabsAccountName);
            capabilities.SetCapability("accessKey", Properties.Settings.Default.SauceLabsAccountKey);


            if (RunLocally)
            {
                _driver = new FirefoxDriver();
            }
            else
            {
                var commandExecutorUri = new Uri("http://ondemand.saucelabs.com:80/wd/hub");
                _driver = new RemoteWebDriver(commandExecutorUri, capabilities);
            }
            _driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(30));
            _driver.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromSeconds(30));

            _driver.Navigate().GoToUrl(Properties.Settings.Default.TestSite);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (!RunLocally)
                {
                    var passed = TestContext.CurrentContext.Result.Status == TestStatus.Passed;
                    ((IJavaScriptExecutor)_driver).ExecuteScript("sauce:job-result=" + (passed ? "passed" : "failed"));
                }
            }
            finally
            {
                _driver.Quit();
            }
        }

        [Test]
        public void PageTitle()
        {
            Assert.AreEqual("Bokadirekt", _driver.Title);
        }

        [Test]
        public void LinkWorks()
        {
            var link = _driver.FindElement(By.Id("thumb-massage"));
            link.Click();

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
            wait.Until(d => d.Url.Contains("Massage/Var/"));

            StringAssert.Contains("Massage/Var/", _driver.Url);

            var result = _driver.FindElement(By.ClassName("search-header")).Text;
            Assert.IsNotNull(result);
            Assert.IsTrue(false);
        }
        [Test]
        public void SearchWorks()
        {
            const string searchWhat = "Sjukgymnaster";
            const string searchWhere = "Stockholm";

            var searchWhatBox = _driver.FindElement(By.XPath("//input[contains(@id, 'txtSearchWhat')]"));
            searchWhatBox.SendKeys(searchWhat);
            var searchWhereBox = _driver.FindElement(By.XPath("//input[contains(@id, 'txtSearchWhere')]"));
            searchWhereBox.SendKeys(searchWhere);
//            searchWhereBox.Submit();
            var searchButton = _driver.FindElement(By.PartialLinkText("Sök"));
            searchButton.Click();

            string expectedUrl = String.Format("/{0}/{1}", searchWhat, searchWhere);
            string expectedHeader = String.Format("{0} {1}", searchWhat, searchWhere);

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
            wait.Until(d => d.Url.Contains(expectedUrl));

            StringAssert.Contains(expectedUrl, _driver.Url);
            Assert.AreEqual(expectedHeader, _driver.Title);
        }
    }
}
