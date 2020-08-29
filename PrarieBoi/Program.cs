using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace PrarieBoi
{
    class Program
    {
        static string[] Scopes = { CalendarService.Scope.Calendar, CalendarService.Scope.CalendarEvents };
        static string ApplicationName = "PrarieBot";

        static void Main(string[] args)
        {
            XDocument config = XDocument.Load("Config.xml");
            List<ClassInfo> classes = new List<ClassInfo>();
            foreach (var classInfo in config.Element("Classes").Elements())
            {
                classes.Add(new ClassInfo((string)classInfo.Attribute("Id"), (string)classInfo.Attribute("username"), (string)classInfo.Attribute("password"), (string)classInfo.Attribute("calendarId")));
            }

            #region create google calendar instances
            UserCredential credential;
            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            // Create Google Calendar API service.
            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            #endregion

            Regex dateFinder = new Regex("(?<=until).*", RegexOptions.Compiled);
            foreach (var classInfo in classes)
            {
                #region login
                IWebDriver driver = new FirefoxDriver();
                driver.Url = "https://prairielearn.engr.illinois.edu/pl/course_instance/" + classInfo.Id + "/assessments";
                driver.Url = "https://prairielearn.engr.illinois.edu/pl/shibcallback";
                driver.FindElement(By.Id("j_username")).SendKeys(classInfo.Username);
                driver.FindElement(By.Id("j_password")).SendKeys(classInfo.Password);
                var hi = driver.FindElement(By.XPath("//input[@type='submit' and @value='Login']"));
                hi.Click();
                #endregion

                #region read table
                List<Event> prarieEvents = new List<Event>();

                IWebElement tableElement = driver.FindElement(By.XPath("/html[1]/body[1]/div[2]/div[1]/table[1]/tbody[1]"));
                IList<IWebElement> tableRow = tableElement.FindElements(By.TagName("tr"));
                IList<IWebElement> rowTD;
                foreach (IWebElement row in tableRow)
                {
                    rowTD = row.FindElements(By.TagName("td"));

                    if (rowTD.Count == 4)
                    {
                        var ev = new Event();
                        EventDateTime dueTime = new EventDateTime();
                        try
                        {
                            if (rowTD[2].Text == "None") return;

                            dueTime.DateTime = DateTime.Parse(dateFinder.Match(rowTD[2].Text).Value);
                            ev.Start = dueTime;
                            ev.Start.TimeZone = "America/Chicago";
                            ev.End = ev.Start;
                            ev.Summary = rowTD[1].FindElement(By.TagName("a")).Text;

                            prarieEvents.Add(ev);
                            Console.WriteLine(rowTD[1].FindElement(By.TagName("a")).Text + " : " + rowTD[2].Text);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }
                }
                driver.Close();
                #endregion


                var calendarId = classInfo.CalendarId;

                // Define parameters of request.
                EventsResource.ListRequest request = service.Events.List(calendarId);
                request.SingleEvents = true;
                request.MaxResults = 2500;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
                request.TimeZone = "America/Chicago";

                Events events = request.Execute();
                
                foreach (var prarieEvent in prarieEvents)
                {
                    var foundEvent = events.Items.FirstOrDefault(x => x.Summary == prarieEvent.Summary);
                    if(foundEvent != null)
                    {
                        if (foundEvent.Start.DateTime == prarieEvent.Start.DateTime)
                            continue;
                        else
                            service.Events.Delete(calendarId, foundEvent.Id);
                    }

                    service.Events.Insert(prarieEvent, calendarId).Execute();
                }
                
            }
        }
    }
}
