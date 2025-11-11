using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;
using System;
using System.IO;
using DotNetEnv;
using System.Linq;
using System.Collections.Generic;

namespace EnsekSeleniumTest
{
    public class EnergyPurchaseTests : IDisposable
    {
        private readonly IWebDriver driver;
        private readonly StreamWriter log;
        private readonly string baseUrl;
        private int total, passed, failed;

        public EnergyPurchaseTests()
        {
            Env.Load();
            baseUrl = Environment.GetEnvironmentVariable("BASE_URL")
                      ?? throw new InvalidOperationException("BASE_URL not found in .env file.");
            driver = new ChromeDriver();
            log = new StreamWriter("EnsekTestResults.txt", append: true);
        }

        [Fact]
        public void Verify_Homepage_UIElements()
        {
            RunTest(() =>
            {
                driver.Navigate().GoToUrl(baseUrl);
                Log("Opened home page.");

                Assert.Equal("ENSEK Energy Corp. - Candidate Test", driver.Title);
                Log($"Page title verified: {driver.Title}");

                var leadParagraph = driver.FindElement(By.CssSelector("p.lead"));
                Assert.Equal("Doing all things energy since 2010!", leadParagraph.Text.Trim());
                Log("Verified <p class='lead'> text.");

                var h2Elements = driver.FindElements(By.TagName("h2"));
                var headers = new[] { "Buy some energy", "Sell some energy", "About us" };
                foreach (var header in headers)
                {
                    Assert.Contains(h2Elements, e => e.Text.Trim() == header);
                    Log($"Verified <h2> text: {header}");
                }

                var findOutMore = driver.FindElement(By.CssSelector("a.btn.btn-primary.btn-lg"));
                Assert.Equal("https://www.ensek.com", findOutMore.GetAttribute("href").TrimEnd('/'));
                Log("Verified primary 'Find out more »' button.");

                var buttons = driver.FindElements(By.CssSelector("a.btn.btn-default"));
                var expectedButtons = new[]
                {
                    new { Text = "Buy energy »", Href = "/Energy/Buy" },
                    new { Text = "Sell energy »", Href = "/Energy/Sell" },
                    new { Text = "Learn more »", Href = "/Home/About" },
                };
                foreach (var expected in expectedButtons)
                {
                    bool found = buttons.Any(b =>
                        b.Text.Trim().Equals(expected.Text, StringComparison.OrdinalIgnoreCase) &&
                        b.GetAttribute("href").EndsWith(expected.Href, StringComparison.OrdinalIgnoreCase)
                    );
                    Assert.True(found, $"Button '{expected.Text}' not found.");
                    Log($"Verified button '{expected.Text}' with href '{expected.Href}'");
                }
            });
        }

        [Fact]
        public void Navigate_To_About_Section()
        {
            RunTest(() =>
            {
                driver.Navigate().GoToUrl(baseUrl);
                Log("Opened home page.");
                ClickWhenReady(By.LinkText("About"));
                Log("Opened About page.");
                ClickWhenReady(By.XPath("//a[text()='Find out more about us »']"));
                Log("Transitioned to detailed section.");
            });
        }

        [Fact]
        public void Buy_All_Energy_Types()
        {
            RunTest(() =>
            {
                driver.Navigate().GoToUrl(baseUrl + "/Energy/Buy");
                Log($"Navigated to Buy Energy page: {driver.Url}");

                var energyTypes = new[] { "Gas", "Electricity", "Oil" };
                int quantity = 50;

                foreach (var type in energyTypes)
                {
                    BuyEnergy(type, quantity);
                }
            });
        }

        [Fact]
        public void Reset_Buy_Energy_Units()
        {
            RunTest(() =>
            {
                driver.Navigate().GoToUrl(baseUrl + "/Energy/Buy");
                Log("Navigated to Buy Energy page.");

                string sampleType = "Gas";
                int units = 20;

                BuyEnergy(sampleType, units);
                Log($"Purchased {units} units of {sampleType}, now testing reset.");

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                var resetButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Name("Reset")));
                resetButton.Click();
                Log("Clicked Reset button.");

                wait.Until(d =>
                {
                    var inputs = d.FindElements(By.XPath("//input[@type='text']"));
                    return inputs.All(i => i.GetDomProperty("value") == "0");
                });

                var inputsAfterReset = driver.FindElements(By.XPath("//input[@type='text']"));
                foreach (var input in inputsAfterReset)
                {
                    Assert.Equal("0", input.GetDomProperty("value"));
                }
                Log("All energy input fields correctly reset to '0'.");
            });
        }

        private void BuyEnergy(string type, int units)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            var row = wait.Until(d =>
            {
                var rows = d.FindElements(By.XPath($"//tr[td[normalize-space(text())='{type}']]"));
                return rows.Count > 0 ? rows[0] : null;
            }) ?? throw new NoSuchElementException($"Row not found for {type}");

            var input = wait.Until(d =>
            {
                var el = row.FindElements(By.XPath(".//input[@type='text']")).FirstOrDefault();
                return el != null && el.Displayed && el.Enabled ? el : null;
            }) ?? throw new NoSuchElementException($"Input not found for {type}");

            input.Clear();
            input.SendKeys(units.ToString());
            Log($"Entered {units} units for {type}.");

            var buyButton = row.FindElement(By.XPath(".//input[@type='submit' and @value='Buy']"));
            wait.Until(d => buyButton.Displayed && buyButton.Enabled);
            buyButton.Click();
            Log($"Clicked Buy button for {type}.");

            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                By.XPath("//h2[normalize-space(text())='Sale Confirmed!']")));
            Log($"Purchase confirmed for {type}.");

            var buyMore = wait.Until(
                SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.LinkText("Buy more »")));
            buyMore.Click();
            Log("Clicked Buy more button to continue purchases.");
        }

        private void RunTest(Action action)
        {
            total++;
            try
            {
                action();
                passed++;
            }
            catch (Exception ex)
            {
                failed++;
                Capture("ErrorScreenshot.png");
                Log($"Failure: {ex.Message}");
                throw;
            }
            finally
            {
                Log($"Summary: Total={total}, Passed={passed}, Failed={failed}");
            }
        }

        private void ClickWhenReady(By locator)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(locator));
            element.Click();
        }

        private void Capture(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(path);
            }
            catch { }
        }

        private void Log(string msg)
        {
            string entry = $"{DateTime.Now}: {msg}";
            log.WriteLine(entry);
            Console.WriteLine(entry);
        }

        public void Dispose()
        {
            driver.Quit();
            log.Close();
        }
    }
}
