# RyanairFlightTrackBot

GitHub compatability!!
 - Can sync (pull then push) whole project from VS2022. 
    1. select Git from the top tab
    2. select Sync
    alternatively:
    1. There is a Git Changes tab where solution explorer usually is. 
    2. can pull and push from here


----------
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


----------
Thoughts:
What assumptions can i make about the nature of an error?
 - Invalid flight details vs out-dated html web-elements
     - If the search button is clicked successully and the flight-card page loads, then html is OK. In this case, it is not likely that the html will have changed on the flight-card page either. If the flight-cards list/array is found, but the flight number is not found, then invalid flight number.
     - Here, for each flight card, a flight number in the specific ([Letter][Letter][space][4 numbers]) format should be checked. If flight numbers are found, but the given one is not, then invalid flight number.
         - I can distinguish internally whether the error is occuring due to an invalid departure or destination airport (or invalid html) by when the error occurs.
               - Stimulate each kind of error, for each step (departure, destination, date, ...)


----------
Top-level (FlightTrackerApp.cs) Architecture:
1. Initialisation procedure
   - Create list of Flgiht objects from DataBase
2. Start the background service (24hr timer between FlightCheck method calls) on a separate thread (so the GUI will appear instantly).
   - Call RunFlightChecks()
      - Create Logger object (file name with todays date)
3. Listen/Wait for TrackButton_Click event from GUI (MainWindow class)
   - Create a an instance of the flight from the user-inputted TextBoxes
   - Pass this instance into the RunNewFlightCheck() method
      - Note, I am initialising a new logger here, rather than using the current logger initialised in the BackgroundService
      - 
4. 

Note, with this architecture, cannot log errors that occur in the initialisation procedure.


----------
Features:
1. Handle blatant user-input errors (caught at track-button-press) (incorrect format) (could create a matrix/table of valid flight routes)
2. Handle inconspicuus user-input errors (correct format but enetered incorrectly) (caught during flight 'validity' check) 
   - Decipher the nature of the exception/error;
      - if flight details incorrect,
         - delete flight object (& table if necessary)
         - inform user (more accurate the error can be pinpointed, the better)
      - if error with HTML web-element
         - try again (10 times)?
         - email Developer/Admin of bug
         - Implement a html element-tree reader/parser that starts from a higher-level html tag/node and trace which tag has changed.
      
3.  Handle case where flight date is now in the past.
    - Flight-cards will appear. Flight # will not be found.
       - need to decipher between user-inputted (first/initial check) and this case.
    - write to Database table with this info
    - move table into a different database (or diffeent area of the same database)
    - remove instance of flight object from flightList
    
4. 

5.  
6.  




        
