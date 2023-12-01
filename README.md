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
        
