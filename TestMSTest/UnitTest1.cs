using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

        private List<RemoteWebDriver> _driver = new List<RemoteWebDriver>();

        private const bool RunLocally = false;


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

        private RemoteWebDriver CreateWebDriver(DesiredCapabilities capabilities)
        {
            RemoteWebDriver driver = null;
            if(RunLocally)
                driver = new FirefoxDriver();
            else
            {
                var commandExecutorUri = new Uri("http://ondemand.saucelabs.com:80/wd/hub");
                driver = new RemoteWebDriver(commandExecutorUri, capabilities);
            }

            driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(30));
            driver.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromSeconds(30));
            driver.Navigate().GoToUrl(Properties.Settings.Default.TestSite);

            return driver;
        }

        [TestInitialize]
        public void Initialize()
        {

            var allCapabilities = OnDemandParser.ParseConfig();
            var credentials = OnDemandParser.UserInfo();

            Console.WriteLine("Found {0} settings.", allCapabilities.Count);

            foreach (var capabilities in allCapabilities)
            {
                capabilities.SetCapability("name", TestContext.TestName);
                capabilities.SetCapability("username", credentials.UserName);
                capabilities.SetCapability("accessKey", credentials.ApiKey);
                _driver.Add(CreateWebDriver(capabilities));
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (!RunLocally)
                {
                    var passed = TestContext.CurrentTestOutcome == UnitTestOutcome.Passed;
                    foreach(var driver in _driver)
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript(
                            "sauce:job-result=" + (passed ? "passed" : "failed"));
                    }
                }
            }
            finally
            {
                foreach (var driver in _driver)
                    driver.Quit();
            }
        }

        private void RunTest(Action<RemoteWebDriver> test)
        {
            foreach (var driver in _driver)
            {
                test(driver);
            }
        }


        [TestMethod]
        public void PageTitle()
        {
            RunTest(d => Assert.AreEqual("Bokadirekt", d.Title));
        }

        [TestMethod]
        public void LinkWorks()
        {
            RunTest(driver =>
            {
                var link = driver.FindElement(By.Id("thumb-massage"));
                link.Click();

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                wait.Until(d => d.Url.Contains("Massage/Var/"));

                StringAssert.Contains(driver.Url, "Massage/Var/");

                var result = driver.FindElement(By.ClassName("search-header")).Text;
                Assert.IsNotNull(result);
            });
        }

        [TestMethod]
        public void SearchWorks()
        {
            RunTest(driver =>
            {
                const string searchWhat = "Sjukgymnaster";
                const string searchWhere = "Stockholm";

                var searchWhatBox = driver.FindElement(By.XPath("//input[contains(@id, 'txtSearchWhat')]"));
                searchWhatBox.SendKeys(searchWhat);
                var searchWhereBox = driver.FindElement(By.XPath("//input[contains(@id, 'txtSearchWhere')]"));
                searchWhereBox.SendKeys(searchWhere);
                //            searchWhereBox.Submit();
                var searchButton = driver.FindElement(By.PartialLinkText("Sök"));
                searchButton.Click();

                string expectedUrl = String.Format("/{0}/{1}", searchWhat, searchWhere);
                string expectedHeader = String.Format("{0} {1}", searchWhat, searchWhere);

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                wait.Until(d => d.Url.Contains(expectedUrl));

                StringAssert.Contains(driver.Url, expectedUrl);
                Assert.AreEqual(expectedHeader, driver.Title);
            });
        }

    }
}
