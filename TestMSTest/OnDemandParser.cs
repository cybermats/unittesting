using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;

namespace TestMSTest
{
    static class OnDemandParser
    {
        [DataContract]
        private class OnDemandInfo
        {
            [DataMember(Name = "platform")]
            public string Platform { get; set; }

            [DataMember(Name = "os")]
            public string Os { get; set; }

            [DataMember(Name = "browser")]
            public string Browser { get; set; }

            [DataMember(Name = "url")]
            public string Url { get; set; }

            [DataMember(Name = "browser-version")]
            public string BrowserVersion { get; set; }
        }

        public class WebDriverConfig
        {
            public string Platform { get; set; }
            public string Browser { get; set; }
            public string BrowserVersion { get; set; }
        }
        private static DesiredCapabilities ResolveCapabilities(string browser, string platform, string version)
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

            if(caps == null)
                throw new Exception("Unknown browser." + browser);
            caps.SetCapability(CapabilityType.Platform, platform);
            caps.SetCapability(CapabilityType.Version, version);
            caps.SetCapability("browserName", browser);
            return caps;
        }

        private static List<DesiredCapabilities> ParseOnDemand()
        {
            var data = new List<DesiredCapabilities>();
            string driver = Environment.GetEnvironmentVariable("SELENIUM_DRIVER");
            if (String.IsNullOrEmpty(driver))
                return data;

            if (driver.StartsWith("sauce-ondemand"))
                return data;

            Console.WriteLine("SELENIUM_DRIVER: [{0}]", driver);

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(driver)))
            {
                try
                {
                    var ser = new DataContractJsonSerializer(typeof(OnDemandInfo[]));
                    var odis = (OnDemandInfo[])ser.ReadObject(ms);
                    data.AddRange(odis.Select(odi =>
                        ResolveCapabilities(odi.Browser, odi.Platform, odi.BrowserVersion)));
                }
                catch (System.Runtime.Serialization.SerializationException e)
                {
                    Console.WriteLine("Unable to deserialize json: " + e);
                }
            }
            return data;
        }


        private static DesiredCapabilities ParseSelenium()
        {
            string platform = Environment.GetEnvironmentVariable("SELENIUM_PLATFORM");
            string browser = Environment.GetEnvironmentVariable("SELENIUM_BROWSER");
            string browserVersion = Environment.GetEnvironmentVariable("SELENIUM_VERSION");

            Console.WriteLine("platform: [{0}]", platform);
            Console.WriteLine("browser: [{0}]", browser);
            Console.WriteLine("browserVersion: [{0}]", browserVersion);

            if (String.IsNullOrEmpty(platform) ||
                String.IsNullOrEmpty(browser) ||
                String.IsNullOrEmpty(browserVersion))
                return null;

            return ResolveCapabilities(browser, platform, browserVersion);
        }

        public static List<DesiredCapabilities> ParseConfig()
        {
            var onDemand = ParseOnDemand();
            if (onDemand.Count > 0)
                return onDemand;

            var selenium = ParseSelenium();
            if(selenium != null)
                return new List<DesiredCapabilities> { selenium };
            return new List<DesiredCapabilities>();
        }

        public class SauceLabsUserInfo
        {
            public string UserName { get; set; }
            public string ApiKey { get; set; }
        }

        public static SauceLabsUserInfo UserInfo()
        {
            string userName = Environment.GetEnvironmentVariable("SAUCE_USER_NAME");
            string apiKey = Environment.GetEnvironmentVariable("SAUCE_API_KEY");

            return new SauceLabsUserInfo()
            {
                UserName = userName,
                ApiKey = apiKey
            };
        }
    }
}
