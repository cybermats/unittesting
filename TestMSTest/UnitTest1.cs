using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace TestMSTest
{
    [TestClass]
    public class UnitTest1
    {
        public TestContext TestContext { get; set; }

        private RemoteWebDriver _driver;
        private readonly string _browser = "firefox";
        private readonly string _version = "33";
        private readonly string _platform = "Windows 7";

        private const bool RunLocally = false;

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
            else if (_browser == "internet explorer")
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

        private void PrintVariable(string name)
        {
            string variable = Environment.GetEnvironmentVariable(name);
            if (!String.IsNullOrEmpty(variable))
            {
                Console.WriteLine("Name: [{0}] Value: [{1}]", name, variable);
            }
            else
            {
                Console.WriteLine("Name: [{0}] No value", name);
            }
            
        }


        [TestInitialize]
        public void Initialize()
        {
            var capabilities = ResolveCapabilities();

            PrintVariable("SELENIUM_HOST");
            PrintVariable("SELENIUM_PORT");
            PrintVariable("SELENIUM_PLATFORM");
            PrintVariable("SELENIUM_VERSION");
            PrintVariable("SELENIUM_BROWSER");
            PrintVariable("SELENIUM_DRIVER");
            PrintVariable("SAUCE_ONDEMAND_BROWSERS");
            PrintVariable("SELENIUM_URL");
            PrintVariable("SAUCE_USER_NAME");
            PrintVariable("SAUCE_API_KEY");

            capabilities.SetCapability("name", TestContext.TestName);
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

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (!RunLocally)
                {
                    var passed = TestContext.CurrentTestOutcome == UnitTestOutcome.Passed;
                    ((IJavaScriptExecutor)_driver).ExecuteScript("sauce:job-result=" + (passed ? "passed" : "failed"));
                }
            }
            finally
            {
                _driver.Quit();
            }
        }

        [TestMethod]
        public void PageTitle()
        {
            Assert.AreEqual("Bokadirekt", _driver.Title);
        }

        [TestMethod]
        public void LinkWorks()
        {
            var link = _driver.FindElement(By.Id("thumb-massage"));
            link.Click();

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
            wait.Until(d => d.Url.Contains("Massage/Var/"));

            StringAssert.Contains(_driver.Url, "Massage/Var/");

            var result = _driver.FindElement(By.ClassName("search-header")).Text;
            Assert.IsNotNull(result);
        }

        [TestMethod]
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

            StringAssert.Contains(_driver.Url, expectedUrl);
            Assert.AreEqual(expectedHeader, _driver.Title);
        }

    }
}
