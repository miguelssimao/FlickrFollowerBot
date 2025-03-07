using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace FlickrFollowerBot
{
    public partial class FollowerBot
    {
        private class Configuration
        {
            internal string AddPhotosToFav;
            internal string AddContactsToFav;
            internal string AddContactsToFollow;
            internal string AddContactsToUnfollow;
            internal string BotUserSaveFolder;
            internal bool BotCacheMyContacts;
            internal bool BotSaveAfterEachAction;
            internal bool BotSaveOnEnd;
            internal bool BotSaveOnLoop;
            internal bool BotUsePersistence;
            internal float BotSeleniumTimeoutSec;
            internal int BotCacheTimeLimitHours;
            internal int BotExploreScrools;
            internal int BotSearchScrools;
            internal int BotFavPictsPerContactMin;
            internal int BotFavPictsPerContactMax;
            internal int BotUnfollowTaskBatchMinLimit;
            internal int BotUnfollowTaskBatchMaxLimit;
            internal int BotUsePersistenceLimitHours;
            internal int BotContactsFavTaskBatchMinLimit;
            internal int BotPhotoFavTaskBatchMinLimit;
            internal int BotFollowTaskBatchMinLimit;
            internal int BotContactsFavTaskBatchMaxLimit;
            internal int BotPhotoFavTaskBatchMaxLimit;
            internal int BotFollowTaskBatchMaxLimit;
            internal int BotRecentContactPostScrools;
            internal int BotStepMaxWaitMs;
            internal int BotStepMinWaitMs;
            internal int BotWaitTaskMaxWaitMs;
            internal int BotWaitTaskMinWaitMs;
            internal int BotKeepSomeUnfollowerContacts;
            internal string BotTasks;
            internal string BotUserEmail;
            internal string BotUserPassword;
            internal string BotSearchKeywords;
            internal string CssContactFollow;
            internal string CssContactFollowed;
            internal string CssContactPhotos;
            internal string CssContactUnfollow;
            internal string CssError500;
            internal string CssPhotosError404;
            internal string CssExploreContact;
            internal string CssLoginEmail;
            internal string CssLoginMyself;
            internal string CssLoginPassword;
            internal string CssLoginWarning;
            internal string CssModalWaiterBalls;
            internal string CssPhotoFave;
            internal string CssPhotoFaved;
            internal string CssPhotos;
            internal string CssPhotosFaved;
            internal string CssRecentContactPost;
            internal string CssWaiterBalls;
            internal string SeleniumRemoteServer;
            internal int SeleniumWindowMaxH;
            internal int SeleniumWindowMaxW;
            internal int SeleniumWindowMinH;
            internal int SeleniumWindowMinW;
            internal IEnumerable<string> SeleniumBrowserArguments;
            internal int BotLoopTaskLimit;
            internal int SeleniumRemoteServerWarmUpWaitMs;
            internal string UrlContacts;
            internal string UrlContactsBlocked;
            internal string UrlContactsMutual;
            internal string UrlContactsNotFriendAndFamily;
            internal string UrlContactsOneWay;
            internal string UrlExplore;
            internal string UrlLogin;
            internal string UrlRecentContactPost;
            internal string UrlRoot;
            internal string UrlSearch;
            internal string UrlContactsInactive;
            internal string ChromeBinaryLocation;
            internal string ChromeDriverLocation;
            internal string CssAcceptCookies;
            internal string CssCookiesIframe;
            internal int BotUnfollowTaskInactiveMinLimit;
            internal int BotUnfollowTaskInactiveMaxLimit;
            internal int ContactsLastUpload;
            internal string CssContactTable;
            internal string CssContactLast;
            internal string CssContactUrl;
            internal string PortugueseMonths;
            internal string PortugueseYears;
            internal string CssContactEdit;
            internal string CssContactCheck;
            internal string CssContactRemove;
            internal string UrlFavorites;
            internal string CssFavoritePhotos;
            internal string CssContactBlocked;
            internal string UrlRootAlt;
            internal string UrlRootWww;
            internal string UrlRootWwwAlt;
        }

        private Configuration Config;

        private void LoadConfig(string[] args)
        {
            string configJsonPath = ExecPath + "/FlickrFollowerBot.json";
            if (File.Exists(configJsonPath))
            {
                IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile(configJsonPath) // default app config
                    .AddEnvironmentVariables()
                    .AddCommandLine(args) // priority
                    .Build();

                Config = new Configuration
                {
                    BotUserEmail = config["BotUserEmail"],
                    BotUserPassword = config["BotUserPassword"],
                    BotTasks = config["BotTasks"],
                    BotSearchKeywords = config["BotSearchKeywords"],
                    AddPhotosToFav = config["AddPhotosToFav"],
                    AddContactsToFav = config["AddContactsToFav"],
                    AddContactsToFollow = config["AddContactsToFollow"],
                    AddContactsToUnfollow = config["AddContactsToUnfollow"],
                    BotUserSaveFolder = config["BotUserSaveFolder"],
                    SeleniumRemoteServer = config["SeleniumRemoteServer"],
                    UrlRoot = config["UrlRoot"],
                    UrlRecentContactPost = config["UrlRecentContactPost"],
                    UrlLogin = config["UrlLogin"],
                    UrlExplore = config["UrlExplore"],
                    UrlContacts = config["UrlContacts"],
                    UrlContactsOneWay = config["UrlContactsOneWay"],
                    UrlContactsMutual = config["UrlContactsMutual"],
                    UrlContactsBlocked = config["UrlContactsBlocked"],
                    UrlContactsNotFriendAndFamily = config["UrlContactsNotFriendAndFamily"],
                    UrlSearch = config["UrlSearch"],
                    CssRecentContactPost = config["CssRecentContactPost"],
                    CssError500 = config["CssError500"],
                    CssPhotosError404 = config["CssPhotosError404"],
                    CssLoginEmail = config["CssLoginEmail"],
                    CssLoginPassword = config["CssLoginPassword"],
                    CssLoginMyself = config["CssLoginMyself"],
                    CssLoginWarning = config["CssLoginWarning"],
                    CssContactPhotos = config["CssContactPhotos"],
                    CssExploreContact = config["CssExploreContact"],
                    CssModalWaiterBalls = config["CssModalWaiterBalls"],
                    CssWaiterBalls = config["CssWaiterBalls"],
                    CssContactFollow = config["CssContactFollow"],
                    CssContactFollowed = config["CssContactFollowed"],
                    CssContactUnfollow = config["CssContactUnfollow"],
                    CssPhotosFaved = config["CssPhotosFaved"],
                    CssPhotos = config["CssPhotos"],
                    CssPhotoFave = config["CssPhotoFave"],
                    CssPhotoFaved = config["CssPhotoFaved"],
                    UrlContactsInactive = config["UrlContactsInactive"],
                    ChromeBinaryLocation = config["ChromeBinaryLocation"],
                    ChromeDriverLocation = config["ChromeDriverLocation"],
                    CssAcceptCookies = config["CssAcceptCookies"],
                    CssCookiesIframe = config["CssCookiesIframe"],
                    CssContactTable = config["CssContactTable"],
                    CssContactLast = config["CssContactLast"],
                    CssContactUrl = config["CssContactUrl"],
                    PortugueseMonths = config["PortugueseMonths"],
                    PortugueseYears = config["PortugueseYears"],
                    CssContactEdit = config["CssContactEdit"],
                    CssContactCheck = config["CssContactCheck"],
                    CssContactRemove = config["CssContactRemove"],
                    UrlFavorites = config["UrlFavorites"],
                    CssFavoritePhotos = config["CssFavoritePhotos"],
                    CssContactBlocked = config["CssContactBlocked"],
                    UrlRootAlt = config["UrlRootAlt"],
                    UrlRootWww = config["UrlRootWww"],
                    UrlRootWwwAlt = config["UrlRootWwwAlt"]
                };

                try
                {
                    // bool
                    Config.BotUsePersistence = int.Parse(config["BotUsePersistence"], CultureInfo.InvariantCulture) != 0;
                    Config.BotCacheMyContacts = int.Parse(config["BotCacheMyContacts"], CultureInfo.InvariantCulture) != 0;
                    Config.BotSaveAfterEachAction = int.Parse(config["BotSaveAfterEachAction"], CultureInfo.InvariantCulture) != 0;
                    Config.BotSaveOnLoop = int.Parse(config["BotSaveOnLoop"], CultureInfo.InvariantCulture) != 0;
                    Config.BotSaveOnEnd = int.Parse(config["BotSaveOnEnd"], CultureInfo.InvariantCulture) != 0;
                    // float
                    Config.BotSeleniumTimeoutSec = float.Parse(config["BotSeleniumTimeoutSec"], CultureInfo.InvariantCulture);
                    // int
                    Config.BotSearchScrools = int.Parse(config["BotSearchScrools"], CultureInfo.InvariantCulture);
                    Config.BotCacheTimeLimitHours = int.Parse(config["BotCacheTimeLimitHours"], CultureInfo.InvariantCulture);
                    Config.BotStepMinWaitMs = int.Parse(config["BotStepMinWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotStepMaxWaitMs = int.Parse(config["BotStepMaxWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotWaitTaskMinWaitMs = int.Parse(config["BotWaitTaskMinWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotWaitTaskMaxWaitMs = int.Parse(config["BotWaitTaskMaxWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotFavPictsPerContactMin = int.Parse(config["BotFavPictsPerContactMin"], CultureInfo.InvariantCulture);
                    Config.BotFavPictsPerContactMax = int.Parse(config["BotFavPictsPerContactMax"], CultureInfo.InvariantCulture);
                    Config.BotExploreScrools = int.Parse(config["BotExploreScrools"], CultureInfo.InvariantCulture);
                    Config.BotContactsFavTaskBatchMinLimit = int.Parse(config["BotContactsFavTaskBatchMinLimit"], CultureInfo.InvariantCulture);
                    Config.BotPhotoFavTaskBatchMinLimit = int.Parse(config["BotPhotoFavTaskBatchMinLimit"], CultureInfo.InvariantCulture);
                    Config.BotFollowTaskBatchMinLimit = int.Parse(config["BotFollowTaskBatchMinLimit"], CultureInfo.InvariantCulture);
                    Config.BotContactsFavTaskBatchMaxLimit = int.Parse(config["BotContactsFavTaskBatchMaxLimit"], CultureInfo.InvariantCulture);
                    Config.BotPhotoFavTaskBatchMaxLimit = int.Parse(config["BotPhotoFavTaskBatchMaxLimit"], CultureInfo.InvariantCulture);
                    Config.BotFollowTaskBatchMaxLimit = int.Parse(config["BotFollowTaskBatchMaxLimit"], CultureInfo.InvariantCulture);
                    Config.BotRecentContactPostScrools = int.Parse(config["BotRecentContactPostScrools"], CultureInfo.InvariantCulture);
                    Config.BotUnfollowTaskBatchMinLimit = int.Parse(config["BotUnfollowTaskBatchMinLimit"], CultureInfo.InvariantCulture);
                    Config.BotUnfollowTaskBatchMaxLimit = int.Parse(config["BotUnfollowTaskBatchMaxLimit"], CultureInfo.InvariantCulture);
                    Config.BotUsePersistenceLimitHours = int.Parse(config["BotUsePersistenceLimitHours"], CultureInfo.InvariantCulture);
                    Config.BotKeepSomeUnfollowerContacts = int.Parse(config["BotKeepSomeUnfollowerContacts"], CultureInfo.InvariantCulture);
                    Config.SeleniumWindowMaxH = int.Parse(config["SeleniumWindowMaxH"], CultureInfo.InvariantCulture);
                    Config.SeleniumWindowMaxW = int.Parse(config["SeleniumWindowMaxW"], CultureInfo.InvariantCulture);
                    Config.SeleniumWindowMinH = int.Parse(config["SeleniumWindowMinH"], CultureInfo.InvariantCulture);
                    Config.SeleniumWindowMinW = int.Parse(config["SeleniumWindowMinW"], CultureInfo.InvariantCulture);
                    Config.SeleniumRemoteServerWarmUpWaitMs = int.Parse(config["SeleniumRemoteServerWarmUpWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotUnfollowTaskInactiveMinLimit = int.Parse(config["BotUnfollowTaskInactiveMinLimit"], CultureInfo.InvariantCulture);
                    Config.BotUnfollowTaskInactiveMaxLimit = int.Parse(config["BotUnfollowTaskInactiveMaxLimit"], CultureInfo.InvariantCulture);
                    Config.ContactsLastUpload = int.Parse(config["ContactsLastUpload"], CultureInfo.InvariantCulture);

                    if (int.TryParse(config["BotLoopTaskLimit"], out int tmpBotLoopTaskLimit))
                    {
                        Config.BotLoopTaskLimit = tmpBotLoopTaskLimit;
                    }
                    else
                    {
                        Config.BotLoopTaskLimit = 0;
                    }

                    if (!string.IsNullOrWhiteSpace(config["SeleniumBrowserArguments"]))
                    {
                        Config.SeleniumBrowserArguments = config["SeleniumBrowserArguments"].Split('|', StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        Config.SeleniumBrowserArguments = Enumerable.Empty<string>();
                    }

                    if (!string.IsNullOrWhiteSpace(config["ChromeBinaryLocation"]))
                    {
                        if(config["ChromeBinaryLocation"][1] == ':')
                        {
                            Config.ChromeBinaryLocation = config["ChromeBinaryLocation"];
                        }
                        else if(config["ChromeBinaryLocation"][0] == '\\')
                        {
                            Config.ChromeBinaryLocation = ExecPath + config["ChromeBinaryLocation"];
                        }
                        else
                        {
                            Config.ChromeBinaryLocation = ExecPath + "\\" + config["ChromeBinaryLocation"];
                        }
                    }
                    else
                    {
                        Config.ChromeBinaryLocation = ExecPath;
                    }

                    if (!string.IsNullOrWhiteSpace(config["ChromeDriverLocation"]))
                    {
                        if(config["ChromeDriverLocation"][1] == ':')
                        {
                            Config.ChromeDriverLocation = config["ChromeDriverLocation"];
                        }
                        else if(config["ChromeDriverLocation"][0] == '\\')
                        {
                            Config.ChromeDriverLocation = ExecPath + config["ChromeDriverLocation"];
                        }
                        else
                        {
                            Config.ChromeDriverLocation = ExecPath + "\\" + config["ChromeDriverLocation"];
                        }
                    }
                    else
                    {
                        Config.ChromeDriverLocation = ExecPath;
                    }
                }
                catch (FormatException ex)
                {
                    throw new FormatException("Bot settings format error, check your settings", ex);
                }
            }
            else
            {
                throw new FormatException("Configuration file missing : " + configJsonPath);
            }
        }
    }
}
