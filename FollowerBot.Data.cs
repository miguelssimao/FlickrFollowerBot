using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlickrFollowerBot
{
    public partial class FollowerBot
    {
        private class PersistenceData
        {
            public string UserContactUrl = null;

            public Queue<string> ContactsToFav = new Queue<string>();

            public Queue<string> ContactsToFollow = new Queue<string>();

            public Queue<string> PhotosToFav = new Queue<string>();

            public Queue<string> ContactsToUnfollow = new Queue<string>();

            public Nullable<DateTime> CookiesInitDate = null;

            public IEnumerable<object> Cookies = new List<object>();
            public IDictionary<string, string> SessionStorage = new Dictionary<string, string>();
            public IDictionary<string, string> LocalStorage = new Dictionary<string, string>();

            public Nullable<DateTime> MyContactsUpdate = null;

            public HashSet<string> MyContacts = new HashSet<string>();

            public HashSet<string> Myself = new HashSet<string>();

            public HashSet<string> MyContactsBanned = new HashSet<string>();
        }

        private readonly HashSet<string> MyContactsInTryout = new HashSet<string>();

        private string JsonPath;

        private PersistenceData Data;

        private void LoadData()
        {
            Data = new PersistenceData();
            if (Config.BotUsePersistence)
            {
                if (!string.IsNullOrWhiteSpace(Config.BotUserSaveFolder))
                {
                    JsonPath = Config.BotUserSaveFolder;
                    if (!JsonPath.EndsWith(Path.DirectorySeparatorChar))
                    {
                        JsonPath += Path.DirectorySeparatorChar;
                    }
                    if (!Directory.Exists(Config.BotUserSaveFolder))
                    {
                        Log.LogDebug("Create session directory {0}", JsonPath);
                        try
                        {
                            Directory.CreateDirectory(JsonPath);
                        }
                        catch (Exception ex)
                        {
                            Log.LogError(default, ex, "Coundn't create {0} directory, using current.", JsonPath);
                            JsonPath = ExecPath + Path.DirectorySeparatorChar + "PersistenceData_";
                        }
                    }
                }
                else
                {
                    JsonPath = ExecPath + Path.DirectorySeparatorChar + "PersistenceData_";
                }

                string fn = JsonPath + Config.BotUserEmail + ".json";
                if (File.Exists(fn))
                {
                    PersistenceData tmp = JsonConvert.DeserializeObject<PersistenceData>(File.ReadAllText(fn, Encoding.UTF8));
                    Data.CookiesInitDate = tmp.CookiesInitDate ?? tmp.MyContactsUpdate ?? DateTime.UtcNow; 

                    if (Config.BotUsePersistenceLimitHours > 0 && DateTime.UtcNow < Data.CookiesInitDate.Value.AddHours(Config.BotUsePersistenceLimitHours))
                    {
                        Log.LogDebug("LOADING USER JSON");
                        Data.UserContactUrl = tmp.UserContactUrl;
                        if (Config.BotCacheMyContacts)
                        {
                            Data.MyContactsUpdate = tmp.MyContactsUpdate;
                            if (tmp.MyContacts != null)
                            {
                                Data.MyContacts = tmp.MyContacts;
                                Log.LogDebug("$MyContacts #{0}", Data.MyContacts.Count);
                            }
                            if (tmp.MyContactsBanned != null)
                            {
                                Data.MyContactsBanned = tmp.MyContactsBanned;
                                Log.LogDebug("$MyContactsBanned #{0}", Data.MyContactsBanned.Count);
                            }
                            if (tmp.Myself != null)
                            {
                                Data.Myself = tmp.Myself;
                            }
                        }
                        if (tmp.ContactsToFollow != null)
                        {
                            Data.ContactsToFollow = new Queue<string>(tmp.ContactsToFollow
                                .Except(Data.MyContacts).Except(Data.MyContactsBanned)); 
                            Log.LogDebug("$ContactsToFollow #{0}", Data.ContactsToFollow.Count);
                        }
                        if (tmp.ContactsToFav != null)
                        {
                            Data.ContactsToFav = new Queue<string>(tmp.ContactsToFav
                                .Except(Data.MyContacts).Except(Data.MyContactsBanned)); 
                            Log.LogDebug("$ContactsToFav #{0}", Data.ContactsToFav.Count);
                        }
                        if (tmp.ContactsToUnfollow != null)
                        {
                            Data.ContactsToUnfollow = tmp.ContactsToUnfollow;
                            Log.LogDebug("$ContactsToUnfollow #{0}", Data.ContactsToUnfollow.Count);
                        }
                        if (tmp.PhotosToFav != null)
                        {
                            Data.PhotosToFav = tmp.PhotosToFav;
                            Log.LogDebug("$PhotosToFav #{0}", Data.PhotosToFav.Count);
                        }
                        if (tmp.Cookies != null)
                        {
                            Data.Cookies = tmp.Cookies;
                            Log.LogDebug("Cookies : {0}", Data.Cookies.Count());
                        }
                        if (tmp.SessionStorage != null)
                        {
                            Data.SessionStorage = tmp.SessionStorage;
                            Log.LogDebug("SessionStorage : {0}", Data.SessionStorage.Count);
                        }
                        if (tmp.LocalStorage != null)
                        {
                            Data.LocalStorage = tmp.LocalStorage;
                            Log.LogDebug("LocalStorage : {0}", Data.LocalStorage.Count);
                        }
                    }
                    else
                    {
                        Log.LogWarning("Persistence limit reached, starting a new session");
                    }
                }
                else
                {
                    Log.LogDebug("No existing session to load : {0}", fn);
                }
            }
        }

        private void SaveData()
        {
            if (Config.BotUsePersistence)
            {
                Log.LogDebug("SAVING USER JSON");
                PersistenceData tmp = new PersistenceData()
                {
                    UserContactUrl = Data.UserContactUrl,
                    ContactsToFollow = Data.ContactsToFollow,
                    ContactsToFav = Data.ContactsToFav,
                    ContactsToUnfollow = Data.ContactsToUnfollow,
                    PhotosToFav = Data.PhotosToFav,
                    CookiesInitDate = Data.CookiesInitDate,
                    Cookies = Selenium.Cookies,
                    SessionStorage = Selenium.SessionStorage,
                    LocalStorage = Selenium.LocalStorage
                };
                if (Config.BotCacheMyContacts)
                {
                    tmp.MyContactsUpdate = Data.MyContactsUpdate;
                    tmp.MyContacts = Data.MyContacts;
                    tmp.MyContactsBanned = Data.MyContactsBanned;
                    tmp.Myself = Data.Myself;
                }
                File.WriteAllText(JsonPath + Config.BotUserEmail + ".json", JsonConvert.SerializeObject(tmp, Formatting.Indented), Encoding.UTF8);
            }
        }
    }
}
