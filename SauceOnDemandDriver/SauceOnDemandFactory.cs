using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;

namespace SauceOnDemandDriver
{
    public static class SauceOnDemandFactory
    {
        private const string Scheme = "sauce-ondemand:";
        private const string UserName = "username";
        private const string AccessKey = "access-key";

        private const string Os = "os";
        private const string Browser = "browser";
        private const string BrowserVersion = "browser-version";
        private const string Platform = "platform";

        private const string SeleniumHost = "SELENIUM_HOST";
        private const string SeleniumPort = "SELENIUM_PORT";

        private const string DefaultWebDriverHost = "ondemand.saucelabs.com";
        private const string DefaultWebDriverPort = "80";

        private static readonly String[] NonProfileParameters= new String[]{AccessKey, Browser, BrowserVersion, Os, UserName};

        private static Dictionary<string, List<string>> PopulateParameterMap(string uri)
        {
            Console.WriteLine("uri: {0}", uri);
            var paramMap = new Dictionary<string, List<string>>();
            foreach (string param in uri.Substring(1).Split('&'))
            {
                int idx = param.IndexOf('=');
                if(idx < 0) 
                    throw new Exception("Invalid parameter format: " + uri);
                string key = param.Substring(0, idx);
                string value = param.Substring(idx + 1);
                Console.WriteLine("Resolving: [{0}] = [{1}]", key, value);

                List<string> v;
                if (!paramMap.TryGetValue(key, out v))
                {
                    v = new List<string>();
                    paramMap[key] = v;
                }
                v.Add(value);
            }

            if (!paramMap.ContainsKey(UserName) && !paramMap.ContainsKey(AccessKey))
            {
                string userName = Environment.GetEnvironmentVariable("SAUCE_USER_NAME");
                string apiKey = Environment.GetEnvironmentVariable("SAUCE_API_KEY");
                paramMap[UserName] = new List<string> { userName };
                paramMap[AccessKey] = new List<string> { apiKey };

                // Read username and access key.
            }
            return paramMap;
        }

        private static DesiredCapabilities ResolveCapabilities(string browser)
        {
            DesiredCapabilities caps = null;
            if (browser == "firefox")
            {
                caps = DesiredCapabilities.Firefox();
            }
            else if (browser == "chrome")
            {
                caps = DesiredCapabilities.Chrome();
            }
            else if (browser == "internet explorer")
            {
                caps = DesiredCapabilities.InternetExplorer();
            }
            else if (browser == "safari")
            {
                caps = DesiredCapabilities.Safari();
            }
            return caps;
        }

        private static RemoteWebDriver CreateWebDriver(string browserUrl, string uri, string jobName)
        {
            var paramMap = PopulateParameterMap(uri);
            DesiredCapabilities desiredCapabilities;

            if (paramMap.ContainsKey(Os) &&
                paramMap.ContainsKey(Browser) &&
                paramMap.ContainsKey(BrowserVersion))
            {
                string browser = paramMap[Browser].First();
                desiredCapabilities = ResolveCapabilities(browser);
                desiredCapabilities.SetCapability(CapabilityType.Version, paramMap[BrowserVersion].First());
                desiredCapabilities.SetCapability(CapabilityType.Platform, paramMap[Os].First());
                if (browser == "firefox")
                    SetFirefoxProfile(paramMap, desiredCapabilities);
                desiredCapabilities.SetCapability("name", jobName);
                PopulateDesiredCapabilities(paramMap, desiredCapabilities);
            }
            else
            {
                throw new Exception("Badly formed url: " + uri);
            }

            string host = Environment.GetEnvironmentVariable(SeleniumHost);
            if (String.IsNullOrEmpty(host))
                host = DefaultWebDriverHost;
            string portAsString = Environment.GetEnvironmentVariable(SeleniumPort);
            if (String.IsNullOrEmpty(portAsString))
                portAsString = DefaultWebDriverPort;

            string tempUri = String.Format("http://{2}:{3}@{0}:{1}/wd/hub",
                host, portAsString,
                paramMap[UserName].First(),
                paramMap[AccessKey].First());
            Console.WriteLine("TempUri: [{0}]", tempUri);
            RemoteWebDriver driver = new SodRemoteWebDriver(
                new Uri(tempUri),
                    desiredCapabilities
                );
            if(!String.IsNullOrEmpty(browserUrl))
                driver.Navigate().GoToUrl(browserUrl);
            return driver;
        }

        private static void SetFirefoxProfile(Dictionary<string, List<string>> paramMap, DesiredCapabilities desiredCapabilities)
        {
            var profile = new FirefoxProfile();
            PopulateProfilePreferences(profile, paramMap);
            desiredCapabilities.SetCapability("firefox_profile", profile);
        }

        private static void PopulateProfilePreferences(FirefoxProfile profile, Dictionary<string, List<string>> paramMap)
        {
            foreach (var kvp in paramMap)
            {
                if (!NonProfileParameters.Contains(kvp.Key))
                    profile.SetPreference(kvp.Key, kvp.Value.First());
            }
        }

        private static void PopulateDesiredCapabilities(Dictionary<string, List<string>> paramMap, DesiredCapabilities desiredCapabilities)
        {
            foreach (var kvp in paramMap)
                desiredCapabilities.SetCapability(kvp.Key, kvp.Value.First());
        }

        public static List<RemoteWebDriver> CreateWebDrivers(string browserUrl, string jobName)
        {
            string json = Environment.GetEnvironmentVariable("SAUCE_ONDEMAND_BROWSERS");
            if(String.IsNullOrEmpty(json))
                throw new Exception("Unable to find SAUCE_ONDEMAND_BROWSERS env var.");

            var browsers = new List<string>();

            if (json.StartsWith(Scheme)) // Only one browser selected
            {
                string url = json.Substring(Scheme.Length);
                if(!url.StartsWith("?"))
                    throw new Exception("Missing '?':" + url);
                browsers.Add(url);
            }
            else
            {
                var onDemandItems = OnDemandParser.ParseOnDemand(json);
                foreach (var onDemandItem in onDemandItems)
                {
                    string url = onDemandItem.Url;
                    if(!url.StartsWith(Scheme))
                        return null;

                    url = url.Substring(Scheme.Length);
                    if(!url.StartsWith("?"))
                        throw new Exception("Missing '?':" + url);
                    browsers.Add(url);
                }
            }

            return browsers.Select(browser => CreateWebDriver(browserUrl, browser, jobName)).ToList();
        }
    }
}
