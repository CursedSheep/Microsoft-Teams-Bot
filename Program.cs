using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MsTeamsBot
{
    class Program
    {
        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        static string Email = "Your Email"; //MS Teams Email
        static string Password = "Your Password"; //MS Teams Password
        static string StudentName = "None"; //Default value. Do not change
        static int JoinWaitingTime = 3; //If Teacher is not in the call for 3 minutes, the bot leaves.
        static int OvertimeWaitingTime = 1; //If the Teacher decided to overtime, bot checks whether Teacher is in the classroom. If teacher is not in the classroom for 1 minute, the bot leaves.
        static string userID = "Discord UserID"; //Discord UserID for Ping (optional)
        static string Webhook = "Discord Webhook link"; //Discord webhook link (Optional)
        static bool SendlogstoDiscord = false; //Send bot activity to discord
        static void Main(string[] args)
        {
            //CursedSheep was here ;D
            SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_AWAYMODE_REQUIRED
| EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED); //Prevent computer from sleeping
            Console.ForegroundColor = ConsoleColor.Green;
            Task.Factory.StartNew(RunTaskThread);

            Thread.Sleep(Timeout.Infinite);
        }
        static async void LogMessage(string Message)
        {
            if(SendlogstoDiscord)
            {
                var discord = new DiscordWebhook(Webhook);
                var testEmbed = new EmbedBuilder()
                    .WithTitle("MsTeams ShitBot Logs")
                    .WithDescription(DiscordMessageConstructor(Message))
                    .WithColor(224, 0, 255)
                    .WithFooter("Timestamp:", "")
                    .WithTimestamp();
                await discord.SendMessageAsync(content: "<@!" + userID + ">", embeds: testEmbed.Build());
            }
            Console.WriteLine($"[{DateTime.Now}] {ComposeLogMessage(Message)}");
        }
        static string DiscordMessageConstructor(string msg)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("```yaml" + Environment.NewLine);
            sb.Append(ComposeLogMessage(msg) + Environment.NewLine);
            sb.Append("```" + Environment.NewLine);
            return sb.ToString();
        }
        static string ComposeLogMessage(string msg) => "- " + StudentName + " " + msg;
        static ChromeDriver driver;
        static List<ClassSchedule> sched;
        static IWebElement calendarBtn;
        static async Task<bool> StartBrowser()
        {
            var options = new ChromeOptions();
            options.AddArgument("chrome_options=opt,service_log_path='NUL'");
            options.AddArgument("--disable-infobars");
            options.AddArgument("start-maximized");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--start-maximized");
            options.AddUserProfilePreference("profile.default_content_setting_values.media_stream_mic", 1);
            options.AddUserProfilePreference("profile.default_content_setting_values.media_stream_camera", 1);
            driver = new ChromeDriver(options);
            string URL = "https://teams.microsoft.com";
            driver.Navigate().GoToUrl(URL);
            await Task.Delay(5000);
            if (driver.Url.Contains("login.microsoftonline.com"))
                return await Login();

            return false;
        }
        static async void RunTaskThread()
        {
            bool result = await StartBrowser();
            if (!result)
            {
                Console.WriteLine("Failed to start browser!");
                Console.ReadKey();
            }
            else
            {
                for(; ; )
                {
                    foreach(var item in sched)
                    {
                        if(!item.Completed && DateTime.Now >= item.Start && DateTime.Now < item.End)
                        {
                            LogMessage($"joined {item.Teacher}'s {item.Name} class.");
                            await StartClass(item);
                            LogMessage($" left {item.Teacher}'s {item.Name} class.");
                            item.Completed = true;
                        }
                    }
                }
            }
        }
        static async Task<bool> StartClass(ClassSchedule c)
        {
            bool hasJoined = await JoinClass(c);
            if (!hasJoined) throw new UnhandledAlertException();
            DateTime joinDate = DateTime.Now;
            //wait for teacher to enter call
            bool TeacherIsInclass;
            for(; ; )
            {
                TeacherIsInclass = IsTeacherPresent(c.Teacher);
                if (!TeacherIsInclass && (DateTime.Now - joinDate).TotalMinutes > JoinWaitingTime) goto ILexit;
                if (TeacherIsInclass)
                {
                    joinDate = DateTime.Now;
                    break;
                }
            }
            //when teacher has entered call, and when the teacher left and 1 min passed, leave call.
            for (; ; )
            {
                TeacherIsInclass = IsTeacherPresent(c.Teacher);
                if (!TeacherIsInclass && (DateTime.Now - joinDate).TotalMinutes > OvertimeWaitingTime) goto ILexit;
                else if (TeacherIsInclass) joinDate = DateTime.Now;
            }
         ILexit:
            await hangUpcall();
            return true;
        }
        static async Task<bool> hangUpcall()
        {
            //hang up call func
            try
            {
                var ShowParticipants = driver.FindElementByXPath(GetID("id", "hangup-button"));
                ShowParticipants.Click();
            }
            catch
            {

            }
            await Task.Delay(4000);
            await GetAllClasses();
            return true;
        }
        static bool IsTeacherPresent(string teacherName)
        {
            //Function to check whether the teacher is among the participants
            int i = 0;
            checkagain:
            try
            {
                List<string> OnlinePariticipants = new List<string>();
                var participantsList = driver.FindElementsByXPath(GetID("ng-repeat", "participant in $vs_collection track by participant.userData.mri"));
                foreach (var element in participantsList)
                {
                    string str = element.GetAttribute("data-tid");
                    if (str.StartsWith("participantsInCall-"))
                    {
                        string[] split = str.Split('-');
                        OnlinePariticipants.Add(split[1]);
                    }
                }
                if (OnlinePariticipants.Exists(x => x.Contains(teacherName))) return true;
            }
            catch
            {
                if (i > 4) return false;
                Console.WriteLine($"[{DateTime.Now}] - Error getting participants");
                i++;
                goto checkagain;
            }
            return false;
        }
        static async Task<bool> Login()
        {
            //Login function
            string[] CREDS = { Email, Password };
            var emailField = driver.FindElementByXPath(GetID("id", "i0116"));
            var btn1 = driver.FindElementByXPath(GetID("id", "idSIButton9"));
            emailField.Click();
            emailField.SendKeys(CREDS[0]);
            btn1.Click(); //Next button
            await Task.Delay(5000);
            var passField = driver.FindElementByXPath(GetID("id", "i0118"));
            passField.Click();
            passField.SendKeys(CREDS[1]);
            var btn2 = driver.FindElementById("idSIButton9");
            btn2.Click(); //#Sign in button
            await Task.Delay(5000);

            driver.FindElementById("idSIButton9").Click();  //remember login
            await Task.Delay(5000);
            driver.FindElementByClassName("use-app-lnk").Click();
            await Task.Delay(5000);
            driver.FindElementByXPath(GetID("class", "user-picture")).Click();
            await Task.Delay(4000);
            againxcd:
            try
            {
                var elementStudentName = driver.FindElementByXPath(GetID("class", "profile-name-text single-line-truncation"));
                StudentName = elementStudentName.Text;
            }
            catch
            {
                goto againxcd;
            }
            await GetAllClasses();
            LogMessage("has logged in.");
            return true;

        }
        static async Task<bool> JoinClass(ClassSchedule c)
        {
            bool found = false;
            await ClickCalendar();
            var calendarItems = driver.FindElementsByXPath(GetID("class", "node_modules--msteams-bridges-components-calendar-grid-dist-es-src-renderers-calendar-multi-day-renderer-calendar-multi-day-renderer__eventCard--3NBeS"));
            foreach (var element in calendarItems)
            {
                try
                {
                    element.Click();
                    await Task.Delay(10);
                    var titleElement = driver.FindElementByXPath(GetID("class", "node_modules--msteams-bridges-components-calendar-grid-dist-es-src-renderers-peek-renderer-peek-meeting-header-peek-meeting-header__subject--24TzV"));
                    var dateElement = driver.FindElementByXPath(GetID("data-tid", "calv2-peek-date"));
                    IWebElement SubjectChannelElement = null;
                    try
                    {
                        SubjectChannelElement = driver.FindElementByXPath(GetID("data-tid", "calv2-peek-channel"));
                    }
                    catch { }
                    var TeacherNameElement = driver.FindElementByXPath(GetID("data-tid", "calv2-peek-organizer"));
                    IWebElement cancelledElement = null;
                    try
                    {
                        cancelledElement = driver.FindElementByXPath(GetID("class", "node_modules--msteams-bridges-components-calendar-grid-dist-es-src-renderers-peek-renderer-peek-meeting-header-peek-meeting-header__cancelled--16Gar"));
                    }
                    catch { }
                    if (cancelledElement != null) continue;
                    string title = titleElement.Text;
                    if (!dateElement.Text.Contains("-")) throw new NotSupportedException();
                    string[] split = dateElement.Text.Split('-');
                    split[0] = split[0].TrimEnd(' ');
                    split[1] = split[1].TrimStart(' ');
                    DateTime start = DateTime.ParseExact(split[0], "MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
                    DateTime end = DateTime.ParseExact($"{start.ToString("MMM d, yyyy")} {split[1]}", "MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
                    if(c.GetTuple() == (title, SubjectChannelElement == null ? "<Unknown>" : SubjectChannelElement.Text, TeacherNameElement.Text, start, end))
                    {
                        found = true;
                        break;
                    }
                }
                catch
                {

                }
                RightClick(calendarBtn);
                await Task.Delay(1000);
            }
            if (!found) return false;
            await Task.Delay(10);
            var joinbtn = driver.FindElementByXPath(GetID("data-tid", "calv2-peek-join-button"));
            joinbtn.Click();
            await Task.Delay(2000);
            var toggles = driver.FindElementsByXPath(GetID("class", "style-layer"));
            foreach(var e in toggles)
            {
                var txt = e.GetAttribute("title");
                if (txt.Contains("Turn camera off") || txt.Contains("Mute microphone")) e.Click();
                await Task.Delay(100);
            }
            await Task.Delay(500);
            var joinNow = driver.FindElementByXPath(GetID("class", "join-btn ts-btn inset-border ts-btn-primary"));
            joinNow.Click();
            await Task.Delay(5000);
            again:
            try
            {
                var ShowParticipants = driver.FindElementByXPath(GetID("id", "roster-button"));
                ShowParticipants.Click();
            }
            catch
            {
                goto again;
            }
            return true;
        }

        static async Task<bool> GetAllClasses() //Get Class Schedule from calendar
        {
            sched = new List<ClassSchedule>();
            await ClickCalendar();
            var calendarItems = driver.FindElementsByXPath(GetID("class", "node_modules--msteams-bridges-components-calendar-grid-dist-es-src-renderers-calendar-multi-day-renderer-calendar-multi-day-renderer__eventCard--3NBeS"));
            foreach(var element in calendarItems)
            {
                try
                {
                    element.Click();
                    await Task.Delay(10);
                    var titleElement = driver.FindElementByXPath(GetID("class", "node_modules--msteams-bridges-components-calendar-grid-dist-es-src-renderers-peek-renderer-peek-meeting-header-peek-meeting-header__subject--24TzV"));
                    var dateElement = driver.FindElementByXPath(GetID("data-tid", "calv2-peek-date"));
                    IWebElement SubjectChannelElement = null;
                    try
                    {
                        SubjectChannelElement = driver.FindElementByXPath(GetID("data-tid", "calv2-peek-channel"));
                    }
                    catch { }
                    var TeacherNameElement = driver.FindElementByXPath(GetID("data-tid", "calv2-peek-organizer"));
                    IWebElement cancelledElement = null;
                    try
                    {
                        cancelledElement = driver.FindElementByXPath(GetID("class", "node_modules--msteams-bridges-components-calendar-grid-dist-es-src-renderers-peek-renderer-peek-meeting-header-peek-meeting-header__cancelled--16Gar"));
                    }
                    catch { }
                    string title = titleElement.Text;
                    if (!dateElement.Text.Contains("-")) throw new NotSupportedException();
                    string[] split = dateElement.Text.Split('-');
                    split[0] = split[0].TrimEnd(' ');
                    split[1] = split[1].TrimStart(' ');
                    DateTime start = DateTime.ParseExact(split[0], "MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
                    DateTime end = DateTime.ParseExact($"{start.ToString("MMM d, yyyy")} {split[1]}", "MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
                    var classsched = new ClassSchedule((title, SubjectChannelElement == null ? "<Unknown>" : SubjectChannelElement.Text, TeacherNameElement.Text, start, end));
                    if (cancelledElement == null) sched.Add(classsched);
                }
                catch
                {

                }
                RightClick(calendarBtn);
                await Task.Delay(1000);
            }
            return true;
        }
        static async Task<bool> ClickCalendar()
        {
            calendarBtn = driver.FindElementByXPath(GetID("id", "app-bar-ef56c0de-36fc-4ef8-b417-3d82ba9d073c"));
            calendarBtn.Click();
            ReadOnlyCollection<IWebElement> chkbtn = null;
            do
            {
                try
                {
                    chkbtn = driver.FindElementsByXPath(GetID("class", "ms-Button-flexContainer flexContainer-41"));

                }
                catch
                {

                }
            } while (chkbtn == null || chkbtn.Count == 0);
            await Task.Delay(5000);
            return true;
        }
        static void RightClick(IWebElement target)
        {
            var builder = new Actions(driver);
            builder.ContextClick(target);
            builder.Perform();
        }
        static string GetID(string objID, string id) => @"//*[@" + objID + "=" + '"' + id + '"' + "]";
    }
}
