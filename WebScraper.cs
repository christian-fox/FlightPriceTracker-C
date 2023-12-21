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
        private LoadingWindow loadingWindow;

        internal WebScraper(Flight flight, LoadingWindow loadingWindow)
        {
            this.flight = flight;
            this.loadingWindow = loadingWindow;
        }

        internal void SimulateCheckpointCompletion()
        {
            // Call the HandleCheckpointCompletion method from another class
            loadingWindow.Dispatcher.Invoke(() => loadingWindow.HandleCheckpointCompletion());
        }

        /// <summary>
        /// Top-level procedure that calls all other sub-procedures in this class.
        /// </summary>
        internal void GetFlightPrice(string operatingSystem)
        {
            bool flightPriceObtained = false;
            int getPriceAttempts = 0;

            while (!flightPriceObtained && getPriceAttempts < 10)
            {
                getPriceAttempts++;
                try
                {
                    using (WebDriver driver = new EdgeDriver())
                    {
                        // Initialize the WebDriver
                        NavigateToRyanairHomepage(driver);
                        HandleCookieAgreement(driver);
                        CheckOneWayCheckBox(driver);
                        PopulateAirportSearchBoxes(driver, operatingSystem);
                        SelectFlightDate(driver);
                        ClickSearchButton(driver);
                        flightPriceObtained = GetFlightDetails(driver);
                    }
                }
                // Handle more specific exceptions first. NoSuchElementException is a subtype of the overarching supertype; WebDriverException.
                catch (NoSuchElementException ex)
                {
                    // Handle missing HTML elements (e.g., missing cookie agreement button, one-way checkbox, etc.)
                    logger.Error($"Missing HTML element: {ex.Message}");
                }
                catch (WebDriverException ex)
                {
                    // Handle WebDriver initialization issues (e.g., Selenium or Edge version mismatch)
                    logger.Error($"WebDriver initialization failed: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Handle other generic exceptions
                    logger.Error($"An unexpected error occurred: {ex.Message}");
                }
            }
            // Log an error if the flight price is not obtained.
            if (!flightPriceObtained)
            {
                logger.Error($"Price could not be obtained for flight: {flight.flightNumber}");
            }

            // Could decipher the nature of the exception - why was the flight price not obtained?
            //      - html error
            //      - invalid flight details (number, airport, date...)
            //      - flight date in the past
            //

        }

        private void NavigateToRyanairHomepage(WebDriver driver)
        {
            driver.Navigate().GoToUrl("https://www.ryanair.com/gb/en");
            SimulateCheckpointCompletion();
        }

        /// <summary>
        /// Click 'Agree' on Cookie Window
        /// Legacy: className="cookie-popup-with-overlay__button" - changed to find by data-ref
        /// </summary>
        private void HandleCookieAgreement(WebDriver driver)
        {
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
            if (agreeButton == null) logger.Error("Cookie window agree button not found.");
            agreeButton.Click();
            SimulateCheckpointCompletion();
        }

        /// <summary>
        /// Check 'One Way' Checkbox
        /// </summary>
        private void CheckOneWayCheckBox(WebDriver driver)
        {
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
            if (oneWayCheckBox == null) logger.Error("One-way Check-box button not found.");
            oneWayCheckBox.Click();
            SimulateCheckpointCompletion();
        }

        /// <summary>
        /// Populates the Departure & Destination search-boxes, then clicks the correct destination from the drop-down list.
        /// </summary>
        private void PopulateAirportSearchBoxes(WebDriver driver, string operatingSystem)
        {
            IWebElement flightSearch = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.TagName("fsw-flight-search-widget-controls")));
            IWebElement departureSearchBox = flightSearch.FindElement(By.CssSelector($"input[id=input-button__departure]"));
            // Populate Departure Searchbox
            if (operatingSystem.ToUpper() == "MAC") departureSearchBox.SendKeys(Keys.Command + "a"); 
            else departureSearchBox.SendKeys(Keys.Control + "a");
            departureSearchBox.SendKeys(Keys.Backspace);
            departureSearchBox.SendKeys(flight.originAirport);
            SimulateCheckpointCompletion();

            // Populate Destination Searchbox
            IWebElement destinationSearchBox = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id("input-button__destination")));
            DateTime startTime = DateTime.Now;
            while ((DateTime.Now - startTime) < TimeSpan.FromSeconds(10) && destinationSearchBox.GetAttribute("value") != flight.destinationAirport)
            {
                destinationSearchBox.SendKeys(flight.destinationAirport);
            }
            //////// Unsure why i am doing this - could just press return/enter? ////////
            // Find correct Destination in airports list
            IWebElement airportsListContainer = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.ClassName("list__airports-scrollable-container")));
            IReadOnlyCollection<IWebElement> destinationOptions = airportsListContainer.FindElements(By.ClassName("ng-star-inserted"));
            // find and click the destination option for the Destination_Airport
            bool destOptionFound = false;
            foreach (IWebElement destOption in destinationOptions)
            {
                if (flight.destinationAirport == destOption.Text)
                {
                    destOption.Click();
                    break;
                }
            }
            if (!destOptionFound)
            {
                logger.Error("Destination Airport not found.");
                throw new Exception("Destination Airport not found.");
            }
            SimulateCheckpointCompletion();
        }

        /// <summary>
        /// Select the flight object 'flightDate' attribute from the calendar 
        /// Bounds of flightDate must be: DateTime.Now.Date <= flightDate <= nineMonthsFromNow.
        /// </summary>
        private void SelectFlightDate(WebDriver driver)
        {
            IWebElement monthsContainer = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.ClassName("m-toggle__scrollable-items")));
            IReadOnlyCollection<IWebElement> monthList = monthsContainer.FindElements(By.ClassName("m-toggle__month"));
            foreach (IWebElement tmpMonth in monthList)
            {
                if (tmpMonth.GetAttribute("data-id") == flight.month)
                {
                    // Calculate the date 9 months from now with day set to 1
                    DateTime nineMonthsFromNow = DateTime.Now.AddMonths(9).AddDays(1);
                    if (flight.flightDate >= nineMonthsFromNow)
                    {
                        // Scroll past the current visible 9 months by clicking scroll 9 times
                        IWebElement monthsScrollWrap = driver.FindElement(By.ClassName("m-toggle__wrap"));
                        IReadOnlyCollection<IWebElement> scrollButtons = monthsScrollWrap.FindElements(By.ClassName("m-toggle__button"));
                        // Second scroll button - 2nd occurrence of this class
                        IWebElement scrollButton2nd = scrollButtons.ElementAt(1);
                        for (int i = 0; i < 9; i++)
                        {
                            // scrollButton2nd.Click();
                            // If using normal Selenium WebDriver (rather than uc), this JavaScript is req'd
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click()", scrollButton2nd);
                        }
                        tmpMonth.Click();
                    }
                    // If flight date is in the past
                    else if (flight.flightDate < DateTime.Now.Date)
                    {
                        throw new ArgumentException();
                        ///////////////////////////////////////////////////////////
                        /// Remove flight object from flightList /////////////////
                        /// Move the flight table in the database ///////////////
                        /// Return from this method & 
                        ////////////////////////////////////////////////////////
                    }
                    else
                    {
                        tmpMonth.Click();
                    }
                    break;
                }
                SimulateCheckpointCompletion();
            }
            // Choose Day (from left-hand calendar)
            IWebElement calendarContainer = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                .Until(ExpectedConditions.ElementExists(By.TagName("calendar-body")));
            IWebElement calendarDay = calendarContainer.FindElement(By.CssSelector($"div[data-value='{flight.day}']"));
            calendarDay.Click();
        }

        /// <summary>
        /// Click Search Button
        /// Legacy: tag_name must = button (<button)
        /// </summary>
        private void ClickSearchButton(WebDriver driver)
        {
            IWebElement searchButton = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                .Until(ExpectedConditions.ElementToBeClickable(By.ClassName("flight-search-widget__start-search-cta")));
            //searchButton.Click(); // Subscribe popup issue
            driver.ExecuteScript("arguments[0].click()", searchButton);
        }

        /// <summary>
        /// Based on the specified flight #, get flight price, seat availability (if present) and flight time.
        /// Returns flightPriceObtained
        /// </summary>
        private bool GetFlightDetails(WebDriver driver)
        {
            bool flightPriceObtained = false;
            IReadOnlyCollection<IWebElement> flightCardContainers = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                .Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.TagName("flight-card-new")));

            foreach (IWebElement flightCard in flightCardContainers)
            {
                if (flightCard.FindElement(By.ClassName("card-flight-num__content")).Text == flight.flightNumber)
                {
                    // Get flightPrice
                    flight.flightPriceStr = flightCard.FindElement(By.ClassName("flight-card-summary__new-value")).Text;
                    flightPriceObtained = true;
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
                    // Break out the foreach loop once the flight is found
                    break;
                }
            }
            return flightPriceObtained;
        }
    }
}