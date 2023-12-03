using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using SeleniumExtras.WaitHelpers;
//using Serilog;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using NLog;

namespace RyanairFlightTrackBot
{
    internal class WebScraper
    {
        //private ILogger logger = Log.ForContext<Flight>();
        private static readonly Logger logger = LoggerManager.GetLogger();

        private readonly Flight flight;

        internal WebScraper(Flight flight)
        {
            this.flight = flight;
        }

        public void GetFlightPrice(string operatingSystem)
        {
            bool isFlightPriceObtained = false;
            int getPriceAttempts = 0;

            while (!isFlightPriceObtained && getPriceAttempts < 10)
            {
                getPriceAttempts++;

                // Initialize the WebDriver
                DateTime startTime = DateTime.Now;
                //var options = new EdgeOptions();
                //options.AddArgument("headless");
                try
                {
                    using (WebDriver driver = new EdgeDriver())//options); }
                    {
                        // Navigate to the search page
                        driver.Navigate().GoToUrl("https://www.ryanair.com/gb/en");

                        // Click 'Agree' on Cookie Window - Legacy className="cookie-popup-with-overlay__button" - changed to find by data-ref
                        IWebElement cookieWindow = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                            .Until(ExpectedConditions.ElementExists(By.ClassName("cookie-popup-with-overlay__box")));
                        
                        IReadOnlyCollection<IWebElement> cookieOptions = cookieWindow.FindElements(By.TagName("button"));
                        IWebElement agreeButton = null;
                        foreach (IWebElement cookieOption in cookieOptions)
                        {
                            if (cookieOption.Text.ToLower().Contains("agree"))
                            {
                                // Assuming only one of the several buttons has the substring "agree" in its name - handle exception?
                                agreeButton = cookieOption;
                                break;
                            }
                        }
                        if (agreeButton != null)
                        {
                            agreeButton.Click();
                        }
                        else
                        {
                            logger.Error("Cookie window agree button not found. None of the buttons contain the sub-string 'agree'.");
                            ///////////////////////////////////////////////////////////////////////
                            // Actually breaking out of the 'try 10 times' loop here. ////////////
                            break; // This avoids a log msg relating to the one-way check-box. //                                                   //
                            ////////////////////////////////////////////////////////////////////
                        }

                        // Check 'One Way' Checkbox
                        IReadOnlyCollection<IWebElement> checkBoxes = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                            .Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.TagName("ry-radio-button")));

                        IWebElement oneWayCheckBox = null;
                        foreach (IWebElement checkBox in checkBoxes)
                        {
                            if (checkBox.GetAttribute("data-ref") == "flight-search-trip-type__one-way-trip")
                            {
                                oneWayCheckBox = checkBox;
                                break;
                            }
                        }
                        oneWayCheckBox?.Click();

                        // Flight Search Widget Controls
                        IWebElement flightSearch = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                            .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.TagName("fsw-flight-search-widget-controls")));

                        IWebElement departureSearchBox = flightSearch.FindElement(By.CssSelector($"input[id=input-button__departure]"));

                        // Populate Departure Searchbox
                        if (operatingSystem.ToUpper() == "MAC") departureSearchBox.SendKeys(Keys.Command + "a");                                  //// test without selecting all. This should clear the text box anyway. NO LONGER NEED THE OperatingSystem input arg if it works!!
                        else departureSearchBox.SendKeys(Keys.Control + "a");
                        departureSearchBox.SendKeys(Keys.Backspace);
                        departureSearchBox.SendKeys(flight.originAirport);

                        // Populate Destination Searchbox
                        IWebElement destinationSearchBox = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                            .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id("input-button__destination")));

                        startTime = DateTime.Now;
                        while ((DateTime.Now - startTime) < TimeSpan.FromSeconds(10))
                        {
                            destinationSearchBox.SendKeys(flight.destinationAirport);
                            if (destinationSearchBox.GetAttribute("value") == flight.destinationAirport) break;

                        }

                        // Find correct Destination in airports list
                        // wait for destination airport options to appear
                        IWebElement airportsListContainer = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                            .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.ClassName("list__airports-scrollable-container")));

                        IReadOnlyCollection<IWebElement> destinationOptions = airportsListContainer.FindElements(By.ClassName("ng-star-inserted"));
                        // find and click the destination option for the Destination_Airport
                        foreach (IWebElement destOption in destinationOptions)
                        {
                            if (flight.destinationAirport.Contains(destOption.Text))
                            {
                                destOption.Click();
                                break;
                            }
                        }

                        // Select the flightDate from the calendar 
                        IWebElement monthsContainer = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                            .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.ClassName("m-toggle__scrollable-items")));

                        IReadOnlyCollection<IWebElement> monthList = monthsContainer.FindElements(By.ClassName("m-toggle__month"));
                        foreach (IWebElement tmpMonth in monthList)
                        {
                            if (tmpMonth.GetAttribute("data-id") == flight.month)
                            {
                                // Calculate the date 9 months from now with day set to 1
                                DateTime nineMonthsFromNow = startTime.AddMonths(9).AddDays(1);

                                if (flight.flightDate >= nineMonthsFromNow)
                                {
                                    // Scroll past the current visible 9 months by clicking scroll 9 times
                                    IWebElement monthsScrollWrap = driver.FindElement(By.ClassName("m-toggle__wrap"));
                                    IReadOnlyCollection<IWebElement> scrollButtons = monthsScrollWrap.FindElements(By.ClassName("m-toggle__button"));
                                    // Second scroll button - 2nd occurrence of this class
                                    IWebElement scrollButton2nd = scrollButtons.ElementAt(1);

                                    for (int i = 0; i < 9; i++)
                                    {
                                        // Scroll_Button_2nd.Click();
                                        // If using normal Selenium WebDriver (rather than uc), this JavaScript is req'd
                                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click()", scrollButton2nd);
                                    }

                                    tmpMonth.Click();
                                }
                                // Setting the bounds of the flight_date
                                else if (flight.flightDate >= startTime.AddYears(1).AddDays(1) || flight.flightDate <= startTime)
                                {
                                    throw new ArgumentException();
                                }
                                else
                                {
                                    tmpMonth.Click();
                                }
                                break;
                            }
                        }

                        // Choose Day (from left-hand calendar)
                        IWebElement calendarContainer = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                            .Until(ExpectedConditions.ElementExists(By.TagName("calendar-body")));
                        IWebElement calendarDay = calendarContainer.FindElement(By.CssSelector($"div[data-value='{flight.day}']"));
                        calendarDay.Click();

                        // Click Search Button
                        // Note, tag_name must = button (<button _ngcontent...)
                        IWebElement searchButton = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                            .Until(ExpectedConditions.ElementToBeClickable(By.ClassName("flight-search-widget__start-search-cta")));
                        //searchButton.Click(); // Uncomment this line if the popup issue is not present
                        driver.ExecuteScript("arguments[0].click()", searchButton);

                        // Get price of specified flight number
                        IReadOnlyCollection<IWebElement> flightCardContainers = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                            .Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.TagName("flight-card-new")));

                        foreach (IWebElement flightCard in flightCardContainers)
                        {
                            if (flightCard.FindElement(By.ClassName("card-flight-num__content")).Text == flight.flightNumber)
                            {
                                // Get flightPrice
                                flight.flightPriceStr = flightCard.FindElement(By.ClassName("flight-card-summary__new-value")).Text;
                                isFlightPriceObtained = true;
                                flight.ParseFlightPriceStr();
                                // Get flightTime (assume this gets first instance of this class name)
                                flight.sFlightTime = flightCard.FindElement(By.ClassName("flight-info__hour")).Text;

                                // Seat availability check
                                try
                                {
                                    flight.seatAvailability = flightCard.FindElement(By.ClassName("flight-card-summary__fares-left")).Text;
                                }
                                catch (NoSuchElementException)
                                {
                                    // 'Seats Remaining' is not displayed on the Flight Card.
                                    flight.seatAvailability = "N/A";
                                }

                                break;
                            }
                        }
                        // Exit the loop if WebDriver initialization is successful
                        break;

                    }

                    // Sleep for 5 seconds to view the webpage - debugging purposes? - doesnt work now?
                    //Thread.Sleep(3000);

                }
                catch (Exception)
                {
                    // Sleep for 1 second before retrying
                    //Thread.Sleep(1000);
                }
            }

            if (!isFlightPriceObtained)
            {
                logger.Error($"Price could not be obtained for flight: {flight.flightNumber}");
            }
        }
    }
}
