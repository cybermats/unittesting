using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support;

namespace SauceOnDemandDriver
{
    public class SodRemoteWebDriver : RemoteWebDriver
    {
        public SodRemoteWebDriver(Uri uri, DesiredCapabilities capabilities)
            : base(uri, capabilities)
        {
            
        }

        public SessionId ExecutionId()
        {
            this.StartClient();
            return SessionId;
        }
    }
}
