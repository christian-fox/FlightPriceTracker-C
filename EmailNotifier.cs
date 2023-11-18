using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace RyanairFlightTrackBot
{
    internal class EmailNotifier
    {
        private ILogger logger = Log.ForContext<Flight>();

        // To increase security, could use a (xml) config file to set environment variables for email credentials and get the OS (cmd prompt) to read them in.
        private static readonly string adminEmail = "christianleonardfox@gmail.com";
        private static readonly string adminPassword = "ougkykfnpjmeians";
        private static readonly string recipient = "christianlfox@aol.com";




        private Flight flight;

        internal EmailNotifier(Flight flight)
        {
            this.flight = flight;
        }

        internal static void NotifyMissingPriceBug()
        {

            //List<Flight> flightList = Flight.GetFlightList();     // dont need flightList here anymore.
            bool bNullPrices = Flight.hasNullPrices();

            // If any flight price is null, send an email
            if (bNullPrices)
            {
                int emailAttempts = 0;
                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(adminEmail, adminPassword);
                smtpClient.EnableSsl = true;

                while (emailAttempts < 10)
                {
                    // Set up email content
                    MailMessage mailMessage = new MailMessage(adminEmail, recipient);
                    mailMessage.Subject = "BUG: Ryanair Flight Bot failed to obtain price";
                    mailMessage.Body = $@"<html>
                                       <body>
                                           <p>Dear <span style=""color:darkblue"">{recipient}</span>,</p>
                                           <p>Issue getting flight prices!</p>
                                       </body>
                                   </html>";
                    mailMessage.IsBodyHtml = true;

                    try
                    {
                        // Send email
                        smtpClient.Send(mailMessage);
                        Console.WriteLine($"Email sent successfully to {recipient}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send email. Error message: {ex.Message}");
                        emailAttempts++;
                    }
                }
            }
        }


        // Method to send the email to the recipient list
        /// <summary>
        /// Sends an email to the email list when the specific flights price reduces from yesterday.
        /// </summary>
        internal void NotifyRecipientsOfPriceReduction()
        {
            foreach (var recipient in flight.recipientList)
            {
                int emailAttempts = 0;
                object sendResult = recipient;

                while (sendResult != null)
                {
                    if (emailAttempts > 10)
                    {
                        Console.WriteLine($"Failed to send email to {recipient} with error message: {sendResult}");
                        break;
                    }

                    // Set up email content
                    MailMessage msg = new MailMessage(adminEmail, recipient.ToString())
                    {
                        Subject = $"Ryanair {flight.destinationAirport} Flight Price Alert",
                        IsBodyHtml = true,
                        Body = $@"
                                <html>
                                <body>
                                    <p>Dear <span style='color:darkblue'>{recipient}</span>,
                                    <br>
                                    <p>This is an automated email from Fox's Flight Price Tracker Bot. I am excited to inform you that the price of your RyanAir flight, from <span style='color:darkblue'>{flight.originAirport}</span> to <span style='color:darkblue'>{flight.destinationAirport}</span> on <span style='color:darkblue'>{flight.day} {flight.month}</span> \
                                    has decreased from <span style='color:darkblue'>{flight.currency}{flight.previousPrice}</span> to <span style='color:darkblue'>{flight.currency}{flight.flightPrice}</span> as of the last time I checked (<span style='color:darkblue'>{flight.sPrevDateAndTime}</span>). You have saved <span style='color:darkblue'>{flight.currency}{Math.Round((double)flight.previousPrice - (double)flight.flightPrice, 2)}</span>!</p>
                                    <p>Please note that this email is only a notification and not a guarantee of the current price. I recommend that you book your flight as soon as possible to secure the best price. Nevertheless, I will continue to keep you updated if the price drops again.</p>
                                    <br>
                                    <p>Best Regards,<br><br>
                                    Fox's Flight Price Tracker Bot</p>
                                    <br>
                                    <br>
                                    <p><span style='color:grey; font-size:10px'>To unsubscribe from these email alerts, simply reply to this email with 'Thanks fox, I owe you one' in the subject field.</span></p>
                                    <p><span style='color:grey; font-size:10px'>Alternatively, if you would like to receive more frequent email alerts, the following options are available:</span></p>
                                    <ul>
                                        <li><span style='color:grey; font-size:10px'>For all fluctuations in the flight price, please reply with 'I want all changes' in the subject field.</span></li>
                                        <li><span style='color:grey; font-size:10px'>For daily updates, please reply with 'I want daily updates' in the subject field.</span></li>
                                    <ul>
                                    <br>
                                    <p><span style='color:grey; font-size:10px'>PS. You can thank Chris at a later date. Gratitude is best shown in the form of cherry ale and/or Guinness.</span></p>
                                </body>
                                </html>"
                    };

                    // Send email
                    using (SmtpClient server = new SmtpClient("smtp.gmail.com", 587))
                    {
                        server.EnableSsl = true;
                        server.Credentials = new NetworkCredential(adminEmail, adminPassword);

                        try
                        {
                            server.Send(msg);
                            Console.WriteLine($"Email to {recipient} sent successfully");
                            sendResult = null;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending email to {recipient}: {ex.Message}");
                            sendResult = ex.Message;
                        }
                    }

                    emailAttempts++;
                }
            }
        }

    }
}
