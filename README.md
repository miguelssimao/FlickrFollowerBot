# FlickrFollowerBot

An improved and updated version of the Flickr Follower Bot, originally made by [smf33](https://github.com/smf33) in .NET Core, using a Chrome client and Selenium.

**Changelog:**
```
- Added new settings
- Updated dependencies
- Updated CSS selectors
- Fixed the signing in process
- Fixed the list of blocked users
- Fixed the DoContactsUnfollow task
- Added the ability to accept cookies
- Added the DetectContactsFromPhoto task
- Added the DoContactsInactiveUnfollow task
- Updated Selenium arguments and some methods
```

*Tags: Flickr, Chrome, Selenium, C#, .Net, Core, bot, robot*

## Requirements

- [.NET Core SDK 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- [Chrome for Testing](https://googlechromelabs.github.io/chrome-for-testing/#stable) with chromedriver

## New commands

![.NET Core](https://github.com/smf33/FlickrFollowerBot/workflows/.NET%20Core/badge.svg) ![Docker](https://github.com/smf33/FlickrFollowerBot/workflows/Docker/badge.svg) ![Docker Compose](https://github.com/smf33/FlickrFollowerBot/workflows/Docker%20Compose/badge.svg)

- Follow users that faved a photo: 
```
dotnet run BotTasks=DetectContactsFromPhoto=PHOTO_URL_HERE,DoContactsFollow
```
- Unfollow inactive users from your contact list: 
```
dotnet run BotTasks=DoContactsInactiveUnfollow
```
For the full list of commands and instructions, please refer to the [original repository](https://github.com/smf33/FlickrFollowerBot#usage).

## New settings
This repository introduces quite a few new settings, but most of them are HTML or CSS selectors.
These are some of the new settings that influence new features:

| Parameter | Description | Default |
| :-------- | :-------- | :---------- |
| **ChromeBinaryLocation** | The relative or full path for Chrome.exe | chrome.exe |
| **ChromeDriverLocation** | The relative or full path where ChromeDriver.exe is located | chrome-win64 |
| **ContactsLastUpload** | How many months since your contacts last uploaded | 12 |
| **PortugueseMonths*** | Translated term for **months** | meses |
| **PortugueseYears*** | Translated term for **years** | anos |

*if you use Flickr in any language other than English or Portuguese, then you should change this setting to the equivalent translation

For the list of main settings, please refer to the [original repository](https://github.com/smf33/FlickrFollowerBot#main-settings).

## New tasks
Task names are case insensitive.  

| Name | Description |
| :--- | :---------- |
| **DoContactsInactiveUnfollow** | Search through contact list and unfollow users that have not uploaded any new photos since `ContactsLastUpload` months ago |
| **DetectContactsFromPhoto** | Push contacts for `DoContactsFollow` and `DoContactsFav` tasks queue. An URL must be specified for this task |

For the list of main tasks, please refer to the [original repository](https://github.com/smf33/FlickrFollowerBot#availeable-taks).

