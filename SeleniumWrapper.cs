using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace FlickrFollowerBot
{
    internal class SeleniumWrapper : IDisposable
    {
        private readonly IJavaScriptExecutor JsDriver;

        private static ChromeOptions GetOptions(string w, string h, string binary, IEnumerable<string> seleniumBrowserArguments)
        {
            ChromeOptions options = new ChromeOptions
            {
                PageLoadStrategy = PageLoadStrategy.Eager,
                BinaryLocation = binary
            };
            options.AddArgument("--window-size=" + w + "," + h);
            options.AddExcludedArguments("excludeSwitches", "enable-logging");
            foreach (string a in seleniumBrowserArguments)
            {
                options.AddArgument(a);
            }
            return options;
        }

        public static SeleniumWrapper NewChromeSeleniumWrapper(string path, string w, string h, string binary, IEnumerable<string> seleniumBrowserArguments, float botSeleniumTimeoutSec)
        {
            ChromeOptions options = GetOptions(w, h, binary, seleniumBrowserArguments);
            return new SeleniumWrapper(new ChromeDriver(path, options), botSeleniumTimeoutSec);
        }

        public static SeleniumWrapper NewRemoteSeleniumWrapper(string configUri, string w, string h, string binary, IEnumerable<string> seleniumBrowserArguments, float botSeleniumTimeoutSec)
        {
            if (!Uri.TryCreate(configUri, UriKind.Absolute, out Uri uri)) 
            {
                uri = new Uri("http://" + configUri + ":4444/wd/hub");
            }
            ChromeOptions options = GetOptions(w, h, binary, seleniumBrowserArguments);
            return new SeleniumWrapper(new RemoteWebDriver(uri, options), botSeleniumTimeoutSec);
        }

        private readonly TimeSpan NormalWaiter;

        private SeleniumWrapper(IWebDriver webDriver, float botSeleniumTimeoutSec)
        {
            NormalWaiter = TimeSpan.FromSeconds(botSeleniumTimeoutSec);

            WebDriver = webDriver;
            webDriver.Manage().Timeouts().PageLoad = NormalWaiter;
            webDriver.Manage().Timeouts().ImplicitWait = NormalWaiter;
            webDriver.Manage().Timeouts().AsynchronousJavaScript = NormalWaiter;

            JsDriver = (IJavaScriptExecutor)webDriver;
        }

        private IEnumerable<IWebElement> FindElementsThatMayBeEmpty(By by)
        {
            try
            {
                return WebDriver.FindElements(by);
            }
            catch
            {
                return Array.Empty<IWebElement>();
            }
        }

        public string Url
        {
            get => WebDriver.Url;
            set => WebDriver.Url = value;
        }

        public string Title => WebDriver.Title;

        internal string CurrentPageSource => JsDriver.ExecuteScript("return document.documentElement.innerHTML").ToString();

        public IEnumerable<IWebElement> GetElements(string cssSelector, bool displayedOnly = true, bool noImplicitWait = false)
        {
            if (noImplicitWait)
            {
                WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            }
            IEnumerable<IWebElement> ret = FindElementsThatMayBeEmpty(By.CssSelector(cssSelector));
            if (displayedOnly)
            {
                ret = ret.Where(x => x.Displayed);
            }
            if (noImplicitWait)
            {
                WebDriver.Manage().Timeouts().ImplicitWait = NormalWaiter;
            }
            return ret;
        }

        public IEnumerable<string> GetAttributes(string cssSelector, string attribute = "href", bool displayedOnly = true, bool noImplicitWait = false)
        {
            return GetElements(cssSelector, displayedOnly, noImplicitWait)
                .Select(x => x.GetAttribute(attribute));
        }

        public bool ClickIfPresent(string cssSelector)
        {
            WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            IEnumerable<IWebElement> found = WebDriver.FindElements(By.CssSelector(cssSelector));
            WebDriver.Manage().Timeouts().ImplicitWait = NormalWaiter;

            if (found.Any())
            {
                found.First().Click();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Click(string cssSelector)
        {
            WebDriver.FindElement(By.CssSelector(cssSelector))
                .Click();
        }

        public void ClickThis(string cssSelector, int index)
        {
            IEnumerable<IWebElement> links = WebDriver.FindElements(By.CssSelector(cssSelector));
            links.ElementAt(index).Click();
        }

        public void ClickThisIfClickable(string cssSelector, int index = 0)
        {
            if (index == 0)
            {
                WaitUntilElementClickable(cssSelector).Click();
            }
            else
            {
                ClickThis(cssSelector, index);
            }
        }

        private IWebElement WaitUntilElementClickable(string cssSelector, int timeout = 10)
        {
            try
            {
                var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(timeout));
                return wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector(cssSelector + ":first-child")));
            }
            catch (NoSuchElementException)
            {
                throw;
            }
        }

        public int GetElementsCount(string cssSelector)
        {
            return WebDriver.FindElements(By.CssSelector(cssSelector)).Count;
        }

        public string GetElementContent(string cssSelector, int index)
        {
            return WebDriver.FindElements(By.CssSelector(cssSelector))[index].Text;
        }

        public string GetElementHref(string cssSelector, int index)
        {
            return WebDriver.FindElements(By.CssSelector(cssSelector))[index].GetAttribute("href");
        }

        public bool SwitchToIframe(string cssIframe)
        {
            WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            IEnumerable<IWebElement> frame = WebDriver.FindElements(By.CssSelector("iframe" + cssIframe));
            WebDriver.Manage().Timeouts().ImplicitWait = NormalWaiter;

            if (frame.Any())
            {
                WebDriver.SwitchTo().Frame(frame.First());
                return true;
            }
            else
            {
                return false;
            }
        }

        public void InputWrite(string cssSelector, string text)
        {
            WebDriver.FindElement(By.CssSelector(cssSelector))
                    .SendKeys(text);
        }

        public void EnterKey(string cssSelector)
        {
            WebDriver.FindElement(By.CssSelector(cssSelector))
                    .SendKeys(Keys.Enter);
        }

        internal void ScrollToBottom()
        {
            JsDriver.ExecuteScript("window.scrollTo(0, document.body.scrollHeight)");
        }

        internal IEnumerable<object> Cookies
        {
            get => WebDriver.Manage().Cookies.AllCookies;
            set
            {
                WebDriver.Manage().Cookies.DeleteAllCookies();
                foreach (JObject cookie in value.OfType<JObject>())
                {
                    Cookie c = Cookie.FromDictionary(cookie.ToObject<Dictionary<string, object>>());
                    WebDriver.Manage().Cookies.AddCookie(c);
                }
            }
        }

        internal IDictionary<string, string> LocalStorage
        {
            get
            {
                IDictionary<string, object> ret = JsDriver.ExecuteScript("return localStorage;") as IDictionary<string, object>;
                return new Dictionary<string, string>(ret
                    .Where(x => x.Value is string)
                    .Select(x => new KeyValuePair<string, string>(x.Key, x.Value as string)));
            }
            set
            {
                StringBuilder s = new StringBuilder("localStorage.clear();");
                foreach (KeyValuePair<string, string> kv in value)
                {
                    s.AppendFormat("localStorage.setItem('{0}', '{1}');", kv.Key, kv.Value);
                }
                JsDriver.ExecuteScript(s.ToString());
            }
        }

        internal IDictionary<string, string> SessionStorage
        {
            get
            {
                IDictionary<string, object> ret = JsDriver.ExecuteScript("return sessionStorage;") as IDictionary<string, object>;
                return new Dictionary<string, string>(ret
                    .Where(x => x.Value is string)
                    .Select(x => new KeyValuePair<string, string>(x.Key, x.Value as string)));
            }
            set
            {
                StringBuilder s = new StringBuilder("sessionStorage.clear();");
                foreach (KeyValuePair<string, string> kv in value)
                {
                    s.AppendFormat("sessionStorage.setItem('{0}', '{1}');", kv.Key, kv.Value);
                }
                JsDriver.ExecuteScript(s.ToString());
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; 
        private IWebDriver WebDriver;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        WebDriver.Quit();
                    }
                    catch
                    {
                        // disposing
                    }
                    finally
                    {
                        WebDriver.Dispose();
                    }
                    WebDriver = null;
                }
                disposedValue = true;
            }
        }

        internal void DumpCurrentPage(string basePath, string userName)
        {
            string dt = DateTime.Now.ToString("yyyyMMdd-HHmmss");

            string html = JsDriver.ExecuteScript("return document.documentElement.innerHTML").ToString()
                .Replace("href=\"/", "href=\"https://www.flickr.com/");
            File.WriteAllText(Path.Combine(basePath, string.Concat(userName, '.', dt, ".html")), html);

            Screenshot ss = ((ITakesScreenshot)WebDriver).GetScreenshot();
            ss.SaveAsFile(Path.Combine(basePath, string.Concat(userName, '.', dt, ".png")));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}
