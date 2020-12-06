# Microsoft Teams Bot

### Introduction
Poorly written Ms teams attendance bot in C#

[This](https://www.youtube.com/watch?v=7neSueHsyY0 "This") video inspired me on making one in C#

### How to use
Just change the field values accordingly and you should be good.
Do not minimize the browser and keep the browser window active to avoid some errors.
```cs
static string Email = "Your Email"; //MS Teams Email
static string Password = "Your Password"; //MS Teams Password
static string StudentName = "None"; //Default value. Do not change
static int JoinWaitingTime = 3; //If Teacher is not in the call for 3 minutes, the bot leaves.
static int OvertimeWaitingTime = 1; //If the Teacher decided to overtime, bot checks whether Teacher is in the classroom. If teacher is not in the classroom for 1 minute, the bot leaves.
static string userID = "Discord UserID"; //Discord UserID for Ping (optional)
static string Webhook = "Discord Webhook link"; //Discord webhook link (Optional)
static bool SendlogstoDiscord = false; //Send bot activity to discord
```
### Credits
[Nikolalv](https://github.com/nikolalv/DiscordWebhook/ "Nikolalv") - Discord Embed Webhook
