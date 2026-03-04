<img src="EmergencyPager/pagerduty.ico" alt="EmergencyPager" height="24" /> EmergencyPager
===

[![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/EmergencyPager/dotnet.yml?branch=master&logo=github)](https://github.com/Aldaviva/EmergencyPager/actions/workflows/dotnet.yml) [![Testspace](https://img.shields.io/testspace/tests/Aldaviva/Aldaviva:EmergencyPager/master?passed_label=passing&failed_label=failing&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4NTkgODYxIj48cGF0aCBkPSJtNTk4IDUxMy05NCA5NCAyOCAyNyA5NC05NC0yOC0yN3pNMzA2IDIyNmwtOTQgOTQgMjggMjggOTQtOTQtMjgtMjh6bS00NiAyODctMjcgMjcgOTQgOTQgMjctMjctOTQtOTR6bTI5My0yODctMjcgMjggOTQgOTQgMjctMjgtOTQtOTR6TTQzMiA4NjFjNDEuMzMgMCA3Ni44My0xNC42NyAxMDYuNS00NFM1ODMgNzUyIDU4MyA3MTBjMC00MS4zMy0xNC44My03Ni44My00NC41LTEwNi41UzQ3My4zMyA1NTkgNDMyIDU1OWMtNDIgMC03Ny42NyAxNC44My0xMDcgNDQuNXMtNDQgNjUuMTctNDQgMTA2LjVjMCA0MiAxNC42NyA3Ny42NyA0NCAxMDdzNjUgNDQgMTA3IDQ0em0wLTU1OWM0MS4zMyAwIDc2LjgzLTE0LjgzIDEwNi41LTQ0LjVTNTgzIDE5Mi4zMyA1ODMgMTUxYzAtNDItMTQuODMtNzcuNjctNDQuNS0xMDdTNDczLjMzIDAgNDMyIDBjLTQyIDAtNzcuNjcgMTQuNjctMTA3IDQ0cy00NCA2NS00NCAxMDdjMCA0MS4zMyAxNC42NyA3Ni44MyA0NCAxMDYuNVMzOTAgMzAyIDQzMiAzMDJ6bTI3NiAyODJjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjY3IDE0LjY3LTEwNiA0NHMtNDQgNjUtNDQgMTA3YzAgNDEuMzMgMTQuNjcgNzYuODMgNDQgMTA2LjVTNjY2LjY3IDU4NCA3MDggNTg0em0tNTU3IDBjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjgzIDE0LjY3LTEwNi41IDQ0UzAgMzkxIDAgNDMzYzAgNDEuMzMgMTQuODMgNzYuODMgNDQuNSAxMDYuNVMxMDkuNjcgNTg0IDE1MSA1ODR6IiBmaWxsPSIjZmZmIi8%2BPC9zdmc%2B)](https://aldaviva.testspace.com/spaces/283429) [![Coveralls](https://img.shields.io/coveralls/github/Aldaviva/EmergencyPager?logo=coveralls)](https://coveralls.io/github/Aldaviva/EmergencyPager?branch=master)

When an alert is triggered in [PagerDuty](https://www.pagerduty.com), turn on [Kasa](https://www.kasasmart.com/us/products/smart-plugs) smart outlets and send push notifications that show toasts on Windows.

<ul>
🚨 Turn on a smart outlet when an incident is triggered.<br/>
🍞 Show an interactive desktop toast notification when an incident is triggered.
</ul>

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3,4" bullets="-,1.,-,-" -->

- [Prerequisites](#prerequisites)
- [PagerDuty Incidents → spinning red light](#pagerduty-incidents-%E2%86%92-spinning-red-light)
    1. [Installation](#installation)
    1. [Configuration](#configuration)
        - [Procedure](#procedure)
        - [Options](#options)
    1. [Execution](#execution)
    1. [Signal Flow](#signal-flow)
- [PagerDuty Incidents → Desktop Push Notifications](#pagerduty-incidents-%E2%86%92-desktop-push-notifications)
    1. [Installation](#installation-1)
    1. [Configuration](#configuration-1)
    1. [Execution](#execution-1)

<!-- /MarkdownTOC -->

## Prerequisites
- [ASP.NET Core Runtime 10 or later](https://dotnet.microsoft.com/en-us/download)
- [PagerDuty account](https://www.pagerduty.com/sign-up/) (the [free plan](https://www.pagerduty.com/sign-up-free/?type=free) is sufficient)
- [Kasa smart outlet](https://www.kasasmart.com/us/products/smart-plugs), such as the [EP10](https://www.kasasmart.com/us/products/smart-plugs/kasa-smart-plug-mini-ep10)
- Ability to listen on a public WAN TCP port for incoming HTTP requests from PagerDuty without being blocked by a NAT or firewall

## PagerDuty Incidents → spinning red light

### Installation
1. Download the ZIP file for your operating system and CPU architecture from the [latest release page](https://github.com/Aldaviva/EmergencyPager/releases/latest).
1. Extract the ZIP file to a directory of your choice, such as `C:\Program Files\EmergencyPager\` or `/opt/emergencypager/`.
    - When installing updates, don't overwrite `appsettings.json`.
1. Install the service so it will start automatically when your computer boots.
    - Windows: `& '.\Install service.ps1'`
        - If this PowerShell script doesn't run, try removing the Mark of the Web by unblocking the file or calling `Set-ExecutionPolicy RemoteSigned`.
    - Linux with systemd:
        ```sh
        sudo cp emergencypager.service /etc/systemd/system/
        sudo systemctl daemon-reload
        sudo systemctl enable emergencypager.service
        ```
        - If the installation directory is not `/opt/emergencypager/`, make sure to edit `emergencypager.service` to match.

### Configuration
#### Procedure
1. Create a Webhook integration in PagerDuty.
    1. Sign into your [PagerDuty account](https://app.pagerduty.com/).
    1. Go to **Integrations › Generic Webhooks (v3)**.
    1. Click **+ New Webhook**.
    1. Set the Webhook URL to the location of your EmergencyPager server, such as `http://myserver.example.com:37374/pagerduty`.
    1. Choose whether events should be fired for all Services in your account, or just one Service.
    1. Choose which Events should be fired, such as `incident.acknowledged`, `incident.escalated`, `incident.reassigned`, `incident.reopened`, `incident.resolved`, `incident.triggered`, and `incident.unacknowledged`
    1. Click **Add Webhook**.
    1. Copy the Signing Secret to the `pagerDutyWebhookSecrets` array in the `appsettings.json` configuration file.
1. Specify the smart outlets to turn on when PagerDuty incidents are triggered.
    1. In `appsettings.json`, add a new key-value pair to the `alarmLightUrlsByPagerDutySubdomain` property.
        - The key is the PagerDuty organization subdomain, also known as company name. For example, if your PagerDuty web admin UI URL is `https://mycompany.pagerduty.com`, then set this key to `mycompany`.
        - The value is an array of one or more URLs representing Kasa smart outlets. The hostname is the IP address or domain name of your outlet, and the optional path is an integer specifying the 0-indexed socket ID if your outlet has multiple sockets, such as the [EP40](https://www.kasasmart.com/us/products/smart-plugs/kasa-smart-wi-fi-outdoor-plug).
        - Example:
            ```json
            "alarmLightUrlsByPagerDutySubdomain": {
                "mycompany": ["tcp://192.168.1.100"]
            }
            ```

#### Options

These go in `appsettings.json`.

##### `alarmLightUrlsByPagerDutySubdomain`
```json
{ "mycompany": ["tcp://192.168.1.100", "tcp://192.168.1.101/0"] }
```
Object where each key is the company name or subdomain of a PagerDuty organization, and its value is an array of URLs of Kasa smart outlets to turn on when an incident in that organization is triggered. The URL path is a 0-indexed integer that identifies which socket on the outlet to control, which can be 0 or the empty string if the outlet only has one socket. The scheme doesn't matter.

##### `kestrel.endpoints.https.url`
```text
http://0.0.0.0:37374
```
URI specifying the hostname and TCP port on which to listen for HTTP requests from the EmergencyPager webhook client. Must be publicly accessible on the WAN.

##### `pagerDutyWebhookSecrets`
```json
["u565tpiQ3TnI…"]
```
Array of one or more PagerDuty webhook signing secrets, which are generated by PagerDuty when you create a new webhook subscription. Only required if you want to turn on smart outlets or show toast notifications on Windows.

### Execution
1. Start the service.
    - Windows: `Restart-Service EmergencyPager`
    - Linux with systemd: `sudo systemctl restart emergencypager.service`

### Signal Flow
1. Something triggers an alert in <img src="https://raw.githubusercontent.com/Aldaviva/PagerDuty/master/PagerDuty/icon.png" alt="PagerDuty" height="12" /> PagerDuty.
1. <img src="https://raw.githubusercontent.com/Aldaviva/PagerDuty/master/PagerDuty/icon.png" alt="PagerDuty" height="12" /> PagerDuty creates a new incident for this alert.
1. <img src="https://raw.githubusercontent.com/Aldaviva/PagerDuty/master/PagerDuty/icon.png" alt="PagerDuty" height="12" /> PagerDuty sends an HTTP POST request to <img src="https://gravatar.com/avatar/53218ea2108534d012156993e92f2e35?size=12" alt="Aldaviva" height="12" /> EmergencyPager with the newly triggered incident.
1. <img src="https://gravatar.com/avatar/53218ea2108534d012156993e92f2e35?size=12" alt="Aldaviva" height="12" /> EmergencyPager sends a TCP JSON object to a Kasa smart outlet, commanding it to turn on its electrical socket.
1. <img src="https://gravatar.com/avatar/53218ea2108534d012156993e92f2e35?size=12" alt="Aldaviva" height="12" /> EmergencyPager sends a WebSocket message containing the newly triggered incident to any connected <img src="https://gravatar.com/avatar/53218ea2108534d012156993e92f2e35?size=12" alt="Aldaviva" height="12" /> EmergencyPager.Toast clients, which show Windows toast notifications.
1. When the Incident is eventually resolved in <img src="https://raw.githubusercontent.com/Aldaviva/PagerDuty/master/PagerDuty/icon.png" alt="PagerDuty" height="12" /> PagerDuty, it sends another POST request to <img src="https://gravatar.com/avatar/53218ea2108534d012156993e92f2e35?size=12" alt="Aldaviva" height="12" /> EmergencyPager, which turns off the smart outlet and dismisses the toasts.

## PagerDuty Incidents → Desktop Push Notifications
PagerDuty makes it easy to receive push notifications for triggered incidents through their Android and iOS apps, SMS, email, and PSTN calls. However, it's not as easy to get an obvious, actionable notification if you're only on a computer while your phone isn't nearby. Phone Link and Google Messages for Web are very unreliable, and emails are easy to miss.

You can solve this with a background program that runs on Windows, connects to the EmergencyPager server, and receives push notifications for triggered incidents which it shows as native Windows toasts. These are rich notifications with buttons to acknowledge or resolve the incident with one click. You can also click on the body of the toast to open the incident web page in your browser.

<p align="center"><img src=".github/images/toast.png" alt="Test Service. Example incident. #55 Triggered" /></p>

### Installation
1. Download `EmergencyPager.Toast-win-x64` from the [latest release page](https://github.com/Aldaviva/EmergencyPager/releases/latest).
1. Extract the EXE from the ZIP file to your hard drive.
1. Register this program to start when you log into Windows by adding its absolute path to a new string value in the registry key `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`.

### Configuration
1. Create a configuration JSON file.
    1. Save the [example configuration file](https://github.com/Aldaviva/EmergencyPager/blob/master/EmergencyPager.Toast/appsettings.json) to `%appdata%\EmergencyPager\Toast.config.json`.
1. Set the `hubAddress` to the URL of your EmergencyPager server, with the path `/pagerduty/toasts`. *This connects the Toast client to the WebSocket server.*
1. Replace the `myorg` key in the `pagerDutyAccountsBySubdomain` object with the subdomain of your PagerDuty organization (the part of the hostname before the `.pagerduty.com` base domain in the web interface, not including that base domain). 
    - *This is used to determine which organization of the following information should be used for a given incident, if you are subscribed to webhooks from multiple organizations.*
1. Set the `userId` of the organization object to the ID of your user, which is the last path segment of your PagerDuty profile page accessible from People › Users.
    - *This is used to record which user acknowledged or resolved an incident.*
1. Set the `userEmailAddress` to your account's email address.
    - *This prevents you from receiving toasts for incidents which are assigned to different people.*
1. Create an API access key in PagerDuty.
    1. Sign into your [PagerDuty account](https://app.pagerduty.com/).
    1. Go to Integrations › API Access Keys.
    1. Click **+ Create New API Key**.
    1. Enter a descriptive name, and make sure **Read-only API Key** is unchecked.
    1. Click **Create Key**.
    1. Set the `apiAccessKey` in `Toast.config.json` to this new access key.
    - *This is used to acknowledge and resolve incidents, since Events V2 Integration Keys are not able to do this because they control alerts, not incidents.*

### Execution
1. Double-click the EXE file to start it. It will run in the background and not show any UI until an incident is triggered.