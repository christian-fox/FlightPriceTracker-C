# RyanairFlightTrackBot

GitHub compatability!!
 - Can sync (pull then push) whole project from VS2022. 
    1. select Git from the top tab
    2. select Sync
    alternatively:
    1. There is a Git Changes tab where solution explorer usually is. 
    2. can pull and push from here


ToDo:
1. Implement timer
   a. System.Timers.Timer - simple, lightweight, in-app
        improve by adding a 'schedulled time' in a table or config file that, upon completion of the flightBackGroundChecksService, is updated to a certain time tomorrow.
   b. Quartz.NET - 'More feature-rich and flexible scheduling options'
      more sophisticated, but requires an extra library. These features dont seem to be of benefit to me
   c. Windows Task Manager App
      Mor reliable, as in-app timers could fail if the app fails

2. Finish GUI click events

3. Sort out top-level App architecture. eg. when will/should GUI appear?
   a. If i have my timier in another thread, will the GUI appear, then can just leave on inside the Virtual Environemnt.





Thoughts
What assumptions can i make about the nature of an error?
 - Invalid flight details vs out-dated html web-elements
     - If the search button is clicked successully and the flight-card page loads, then html is OK. In this case, it is not likely that the html will have changed on the flight-card page either. If the flight-cards list/array is found, but the flight number is not found, then invalid flight number.
     - Here, for each flight card, a flight number in the specific ([Letter][Letter][space][4 numbers]) format should be checked. If flight numbers are found, but the given one is not, then invalid flight number.
           - I can distinguish internally whether the error is occuring due to an invalid departure or destination airport (or invalid html) by when the error occurs.
               - Stimulate each kind of error, for each step (departure, destination, date, ...)
       - 
        
