To run, add a new file called Config.xml, and set it to copy to output directory.

Use the following formatting:
```xml
<Classes>
  <class Id="" username="" password="" calendarId=""/>
</Classes>
```

Id is the numerical class Id, which can be found in the url when viewing the class

username/password are the UIUC netid credentials

calendarId is the google calendar calendarId


Next, you must generate your own credentials.json file, and set it to copy to output directory.
You can easily create one by following step 1 of this link
https://developers.google.com/calendar/quickstart/dotnet
