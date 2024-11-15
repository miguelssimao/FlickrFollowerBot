using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.Web;
using Microsoft.Extensions.Logging;

namespace FlickrFollowerBot
{
    public partial class FollowerBot
    {
        private IEnumerable<string> GetContactList(string subUrl)
        {
            MoveTo(Data.UserContactUrl + subUrl);
            List<string> ret = Selenium.GetAttributes(Config.CssContactPhotos)
                .ToList();
            int page = 2;
            while (Selenium.GetElements("a[href*=\"/?page=" + page.ToString(CultureInfo.InvariantCulture) + "\"]", true, true).Any())
            {
                MoveTo(Data.UserContactUrl + subUrl + "/?page=" + page.ToString(CultureInfo.InvariantCulture));
                ret.AddRange(Selenium.GetAttributes(Config.CssContactPhotos));
                page++;
            }
            return ret;
        }

        private IEnumerable<string> GetContactListBanned(string subUrl)
        {
            MoveTo(Data.UserContactUrl + subUrl);
            List<string> ret = Selenium.GetAttributes(Config.CssContactBlocked)
                .ToList();
            int page = 2;
            while (Selenium.GetElements("a[href*=\"/?page=" + page.ToString(CultureInfo.InvariantCulture) + "\"]", true, true).Any())
            {
                MoveTo(Data.UserContactUrl + subUrl + "/?page=" + page.ToString(CultureInfo.InvariantCulture));
                ret.AddRange(Selenium.GetAttributes(Config.CssContactBlocked));
                page++;
            }
            return ret;
        }

        private IEnumerable<string> GetContactListFromFavs(string subUrl)
        {
            MoveTo(subUrl);
            List<string> ret = Selenium.GetAttributes(Config.CssFavoritePhotos)
                .ToList();
            int page = 2;
            while (Selenium.GetElements("a[href*=\"/page" + page.ToString(CultureInfo.InvariantCulture) + "/\"]", true, true).Any())
            {
                MoveTo(subUrl + "/page" + page.ToString(CultureInfo.InvariantCulture) + "/");
                ret.AddRange(Selenium.GetAttributes(Config.CssFavoritePhotos));
                page++;
            }
            return ret;
        }

        private IEnumerable<string> GetMyself()
        {
            string[] Myself = Regex.Split(Data.UserContactUrl, @"(.*)\/people\/(.*)", RegexOptions.IgnoreCase);
            string MyselfURL = Myself[1] + "/photos/" + Myself[2] + "/";
            List<string> ret = new List<string>{MyselfURL};
            return ret;
        }

        private void AddForced(string configName, string configValue, Queue<string> queue)
        {
            if (!string.IsNullOrWhiteSpace(configValue))
            {
                int added = 0;
                foreach (string s in configValue.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (s.StartsWith(Config.UrlRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!queue.Contains(s))
                        {
                            added++;
                            queue.Enqueue(s);
                        }
                        else
                        {
                            Log.LogDebug("{0} already in ${1}", s, configName);
                        }
                    }
                    else
                    {
                        Log.LogWarning("Check {0} Url format for {1}", configName, s);
                    }
                }
                Log.LogDebug("${0} +{1} AddForced", configName, added);
            }
        }

        private void PostAuthInit()
        {
            ClickWaitIfPresent(Config.CssLoginWarning);
            ClickIframeElementIfPresent(Config.CssCookiesIframe, Config.CssAcceptCookies);

            if (!Data.MyContactsUpdate.HasValue
                || Config.BotCacheTimeLimitHours <= 0
                || DateTime.UtcNow > Data.MyContactsUpdate.Value.AddHours(Config.BotCacheTimeLimitHours))
            {
                Data.MyContacts = GetContactList(Config.UrlContacts).ToHashSet();
                Log.LogDebug("$MyContacts ={0}", Data.MyContacts.Count);

                Data.MyContactsBanned = GetContactListBanned(Config.UrlContactsBlocked).ToHashSet();
                Log.LogDebug("$MyContactsBanned ={0}", Data.MyContactsBanned.Count);

                Data.MyContactsUpdate = DateTime.UtcNow;
                Data.Myself = GetMyself().ToHashSet();
            }

            AddForced("AddContactsToFav", Config.AddContactsToFav, Data.ContactsToFav);
            AddForced("AddContactsToFollow", Config.AddContactsToFollow, Data.ContactsToFollow);
            AddForced("AddContactsToUnfollow", Config.AddContactsToUnfollow, Data.ContactsToUnfollow);
            AddForced("AddPhotosToFav", Config.AddPhotosToFav, Data.PhotosToFav);
        }

        private void AuthLogin()
        {
            if (!MoveTo(Config.UrlLogin))
            {
                throw new NotSupportedException("FLICKR RETURN ERROR 500 ON " + Config.UrlLogin);
            }

            if (!string.IsNullOrWhiteSpace(Config.BotUserEmail))
            {
                Selenium.InputWrite(Config.CssLoginEmail, Config.BotUserEmail);
                Selenium.EnterKey(Config.CssLoginEmail);

                if (!string.IsNullOrWhiteSpace(Config.BotUserPassword))
                {
                    Selenium.InputWrite(Config.CssLoginPassword, Config.BotUserPassword);
                    Selenium.EnterKey(Config.CssLoginPassword);
                }
                else
                {
                    Log.LogWarning("Waiting user manual password validation...");
                }

                WaitUrlStartsWith(Config.UrlRoot); // loading may take some time
                WaitHumanizer(); // after WaitUrlStartsWith because 1st loading may take extra time

                // who am i ?
                Data.UserContactUrl = Selenium.GetAttributes(Config.CssLoginMyself, "href", false)
                    .First(); // not single to be safe
                if (Data.UserContactUrl.EndsWith('/')) // standardize
                {
                    Data.UserContactUrl = Data.UserContactUrl.Remove(Data.UserContactUrl.Length - 1);
                }

                Data.CookiesInitDate = DateTime.UtcNow;
            }
            else
            {
                throw new FormatException("BotUserEmail required !");
            }
        }

        private bool TryAuthCookies()
        {
            if (Data.Cookies != null && Data.Cookies.Any())
            {
                if (!MoveTo(Config.UrlRoot))
                {
                    throw new NotSupportedException("FLICKR RETURN ERROR 500 ON " + Config.UrlRoot);
                }

                Selenium.Cookies = Data.Cookies; // need to have loaded the page 1st
                Selenium.SessionStorage = Data.SessionStorage; // need to have loaded the page 1st
                Selenium.LocalStorage = Data.LocalStorage; // need to have loaded the page 1st

                if (!MoveTo(Config.UrlRoot))
                {
                    throw new NotSupportedException("FLICKR RETURN ERROR 500 ON " + Config.UrlRoot);
                }

                //check cookie auth OK
                // who am i ?
                string curUserContactUrl = Selenium.GetAttributes(Config.CssLoginMyself, "href", false)
                    .FirstOrDefault(); // not single to be safe
                if (curUserContactUrl != null && curUserContactUrl.EndsWith('/')) // standardize
                {
                    curUserContactUrl = curUserContactUrl.Remove(curUserContactUrl.Length - 1);
                }

                if (Data.UserContactUrl.Equals(curUserContactUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else
                {
                    Log.LogWarning("Couldn't log user from cookie. Try normal auth");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void DetectExplored(bool doContact = true, bool doPost = true)
        {
            MoveTo(Config.UrlExplore); // 500 images so u can scrool some time...

            SchroolDownLoop(Config.BotExploreScrools);

            if (doContact)
            {
                string[] list = Selenium.GetAttributes(Config.CssExploreContact, "href", false)
                    .Distinct()
                    .Except(Data.MyContacts)
                    .Except(Data.MyContactsBanned)
                    .ToArray();// solve linq for multiple use

                int c = Data.ContactsToFollow.Count;
                foreach (string needToFollow in list.Except(Data.ContactsToFollow))
                {
                    Data.ContactsToFollow.Enqueue(needToFollow);
                }
                Log.LogDebug("$ContactsToFollow +{0}", Data.ContactsToFollow.Count - c);

                c = Data.ContactsToFav.Count;
                foreach (string needToFollow in list.Except(Data.ContactsToFav))
                {
                    Data.ContactsToFav.Enqueue(needToFollow);
                }
                Log.LogDebug("$ContactsToFav +{0}", Data.ContactsToFav.Count - c);
            }
            if (doPost)
            {
                string[] list = Selenium.GetAttributes(Config.CssPhotos, "href", false)
                    .Except(Data.PhotosToFav)
                    .ToArray();// solve linq for multiple use

                int c = Data.PhotosToFav.Count;
                foreach (string needToFav in list
                    .Except(Data.PhotosToFav))
                {
                    Data.PhotosToFav.Enqueue(needToFav);
                }
                Log.LogDebug("$PhotosToFav +{0}", Data.PhotosToFav.Count - c);
            }
        }

        private void SearchKeywords(bool doContact = true, bool doPhoto = true)
        {
            IEnumerable<string> keywords = Config.BotSearchKeywords?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();
            foreach (string keyword in keywords)
            {
                Log.LogDebug("Searching {0}", keyword);

                MoveTo(Config.UrlSearch + HttpUtility.UrlEncode(keyword));
                WaitHumanizer();

                SchroolDownLoop(Config.BotSearchScrools);

                if (doContact)
                {
                    string[] list = Selenium.GetAttributes(Config.CssExploreContact, "href", false)
                        .Distinct()
                        .Except(Data.MyContacts)
                        .Except(Data.MyContactsBanned)
                        .ToArray();// solve for multiple use

                    int c = Data.ContactsToFollow.Count;
                    foreach (string needToFollow in list.Except(Data.ContactsToFollow))
                    {
                        Data.ContactsToFollow.Enqueue(needToFollow);
                    }
                    Log.LogDebug("$ContactsToFollow +{0}", Data.ContactsToFollow.Count - c);

                    c = Data.ContactsToFav.Count;
                    foreach (string needToFollow in list.Except(Data.ContactsToFav))
                    {
                        Data.ContactsToFav.Enqueue(needToFollow);
                    }
                    Log.LogDebug("$ContactsToFav +{0}", Data.ContactsToFav.Count - c);
                }
                if (doPhoto)
                {
                    string[] list = Selenium.GetAttributes(Config.CssPhotos, "href", false)
                        .Except(Data.PhotosToFav)
                        .ToArray();// solve linq for multiple use

                    int c = Data.PhotosToFav.Count;
                    foreach (string needToFav in list
                        .Except(Data.PhotosToFav))
                    {
                        Data.PhotosToFav.Enqueue(needToFav);
                    }
                    Log.LogDebug("$PhotosToFav +{0}", Data.PhotosToFav.Count - c);
                }
            }
        }

        private void DetectContactsFollowBack()
        {
            IEnumerable<string> list = GetContactList(Config.UrlContactsOneWay)
                .Except(Data.MyContactsBanned)
                .ToList(); // Solve

            int c = Data.ContactsToFollow.Count;
            foreach (string needToFollow in list
                .Except(Data.ContactsToFollow))
            {
                Data.ContactsToFollow.Enqueue(needToFollow);
            }
            Log.LogDebug("$ContactsToFollow +{0}", Data.ContactsToFollow.Count - c);

            c = Data.ContactsToFav.Count;
            foreach (string needToFollow in list
                .Except(Data.ContactsToFav))
            {
                Data.ContactsToFav.Enqueue(needToFollow);
            }
            Log.LogDebug("$ContactsToFav +{0}", Data.ContactsToFav.Count - c);
        }

        private void DoContactsInactiveUnfollow()
        {
            int removed = 0;
            int stop = Config.ContactsLastUpload;
            string url = Data.UserContactUrl + Config.UrlContactsInactive;
            int todo = PseudoRand.Next(Config.BotUnfollowTaskInactiveMinLimit, Config.BotUnfollowTaskInactiveMaxLimit);
            while (todo > 0)
            {
                if (!MoveTo(url, true))
                {
                    Log.LogWarning("ACTION STOPPED : FLICKR RETURN ERROR 500 ON ({0})", url);
                    break; // no retry
                }
                try
                {
                    int resultInteger;
                    int rowsCount = Selenium.GetElementsCount(Config.CssContactTable);
                    for (int i = 0; i < rowsCount; ++i)
                    {
                        resultInteger = 0;
                        string lastUpload = Selenium.GetElementContent(Config.CssContactLast, i).Trim();
                        if (lastUpload.Any(char.IsDigit))
                        {
                            resultInteger = stop;
                            string[] periodOfTime = lastUpload.Split(' ');
                            if(periodOfTime[1] == Config.PortugueseMonths || periodOfTime[1] == "months" || 
                                periodOfTime[1] == Config.PortugueseYears || periodOfTime[1] == "years")
                            {
                                string resultString = Regex.Match(lastUpload, @"\d+").Value;
                                resultInteger = Int32.Parse(resultString);
                            }
                        }

                        if (resultInteger == 0 || resultInteger > stop)
                        {
                            Selenium.ClickThisIfClickable(Config.CssContactEdit, i);
                            Selenium.Click(Config.CssContactCheck);
                            Selenium.Click(Config.CssContactRemove);
                            Data.MyContacts.Remove(Selenium.GetElementHref(Config.CssContactUrl, i));
                            Log.LogDebug("REMOVED {0}", Selenium.GetElementHref(Config.CssContactUrl, i));
                            WaitHumanizer();
                            ++removed;
                        }
                        else
                        {
                            Log.LogWarning("ACTION STOPPED : THERE ARE NO MORE INACTIVE USERS TO UNFOLLOW");
                            todo = 0;
                            break; // no retry
                        }
                    }
                    todo--;
                }
                catch (Exception ex)
                {
                    Log.LogWarning(default, ex, "ACTION STOPPED : {0}", ex.GetBaseException().Message);
                    break; // stop this action
                }
            }
            Log.LogDebug("$InactiveContactsToUnfollow -{0}", removed);
        }

        private void DoContactsFollow()
        {
            int todo = PseudoRand.Next(Config.BotFollowTaskBatchMinLimit, Config.BotFollowTaskBatchMaxLimit);
            int c = Data.ContactsToFollow.Count;
            while (Data.ContactsToFollow.TryDequeue(out string uri) && todo > 0)
            {
                if (!MoveTo(uri))
                {
                    Log.LogWarning("ACTION STOPPED : FLICKR RETURN ERROR 500 ON ({0})", uri);
                    break; // no retry
                }
                try
                {
                    MyContactsInTryout.Add(uri);
                    if (Selenium.GetElements(Config.CssContactFollow).Any()) // manage the already followed like this
                    {
                        Selenium.Click(Config.CssContactFollow);
                        Data.MyContacts.Add(uri);
                        WaitHumanizer();// the url reload may break a waiting ball

                        // issue detection : Manage limit to 20 follow on a new account : https://www.flickr.com/help/forum/en-us/72157651299881165/  Then there seem to be another limit
                        if (Selenium.GetElements(Config.CssContactFollow, true, true).Any()) // will not wait
                        {
                            WaitHumanizer();// give a last chance
                            if (Selenium.GetElements(Config.CssContactFollow, true, true).Any())
                            {
                                Log.LogWarning("ACTION STOPPED : SEEMS USER CAN'T FOLLOW ({0}) ANYMORE", uri);
                                break; // no retry
                            }
                        }
                        // hang issue detection
                        if (Selenium.GetElements(Config.CssWaiterBalls, true, true).Any()) // will not wait
                        {
                            WaitHumanizer();// give a last chance...
                            if (Selenium.GetElements(Config.CssWaiterBalls, true, true).Any()) // will not wait
                            {
                                Log.LogWarning("ACTION STOPPED : SEEMS FLICKR HANG ON THIS CONTACT ({0})", uri);
                                break; // no retry
                            }
                        }
                        todo--;
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning(default, ex, "ACTION STOPPED : {0}", ex.GetBaseException().Message);
                    break; // stop this action
                }
            }
            Log.LogDebug("$ContactsToFollow -{0}", c - Data.ContactsToFollow.Count);
        }

        private void PhotoFav(string url, out bool wasFaved, out bool inError)
        {
            inError = wasFaved = false;
            if (!MoveTo(url))
            {
                WaitHumanizer();    // try again
                if (!MoveTo(url))
                {
                    Log.LogWarning("ACTION STOPPED : FLICKR RETURN ERROR 500 ON ({0})", url);
                    inError = true;
                }
            }
            //photo may have been deleted.
            if (!inError
                && !Selenium.GetElements(Config.CssPhotosError404, true, true).Any() // doesn't count as an error : user may have deleted his pictures
                && !Selenium.GetElements(Config.CssPhotoFaved, true, true).Any()) // may have been already faved
            {
                try
                {
                    Selenium.Click(Config.CssPhotoFave);
                }
                catch (OpenQA.Selenium.ElementClickInterceptedException ex)  // check if we are in a Flickr Freeze (fluid-modal-overlay/balls over all)
                {
                    Log.LogWarning(default, ex, "ACTION STOPPED : FLICKER SEEMS TO NEED A RELOAD ({0})", ex.GetBaseException().Message);
                    inError = true;
                }
                WaitHumanizer();// the url reload may break a waiting ball

                // fav issue detection
                if (!inError && !Selenium.GetElements(Config.CssPhotoFaved).Any()) // will wait a little if required because it s an expected state
                {
                    WaitHumanizer();// give a last chance...
                    if (!Selenium.GetElements(Config.CssPhotoFaved, true, true).Any()) // will not wait
                    {
                        Log.LogWarning("ACTION STOPPED : SEEMS USER ({0}) CAN'T FAV ANYMORE", url);
                        inError = true;
                    }
                }
                // hang issue detection
                if (!inError && Selenium.GetElements(Config.CssWaiterBalls, true, true).Any()) // will not wait
                {
                    WaitHumanizer();// give a last chance...
                    if (Selenium.GetElements(Config.CssWaiterBalls, true, true).Any()) // will not wait
                    {
                        Log.LogWarning("ACTION STOPPED : SEEMS FLICKR HANG ON THIS PICTURE ({0})", url);
                        inError = true;
                    }
                }
                wasFaved = true;
            }
        }

        private void DoContactsFav()
        {
            if (Config.BotFavPictsPerContactMin > 0)
            {
                int contactsTodo = PseudoRand.Next(Config.BotContactsFavTaskBatchMinLimit, Config.BotContactsFavTaskBatchMaxLimit);
                int c = Data.ContactsToFav.Count;
                while (Data.ContactsToFav.TryDequeue(out string uri) && 0 < contactsTodo)
                {
                    if (!MoveTo(uri))
                    {
                        Log.LogWarning("ACTION STOPPED : FLICKR RETURN ERROR 500 ON ({0})", uri);
                        break; // no retry
                    }
                    try
                    {
                        Selenium.ScrollToBottom(); // else will only find 1st picts

                        int favsTodo = PseudoRand.Next(Config.BotFavPictsPerContactMin, Config.BotFavPictsPerContactMax);
                        favsTodo -= Selenium.GetElements(Config.CssPhotosFaved, false, true).Count(); // mainly no picture already faved :-) don t wait uselessl
                        if (0 < favsTodo)
                        {
                            string[] array = Selenium.GetAttributes(Config.CssPhotos, "href", false)
                                .ToArray(); // page change so, need to solve
                            int i = 0;
                            while (favsTodo > 0 && i < array.Length)
                            {
                                PhotoFav(array[i], out bool hasFaved, out bool inError);
                                if (inError)
                                {
                                    break;
                                }
                                if (hasFaved)
                                {
                                    favsTodo--;
                                }
                                i++;
                            }
                            contactsTodo--;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning(default, ex, "ACTION STOPPED : {0}", ex.GetBaseException().Message);
                        break; // stop this action
                    }
                }
                Log.LogDebug("$ContactsToFav -{0}", c - Data.ContactsToFav.Count);
            }
        }

        private void DetectContactsFromPhoto(string currentTask)
        {
            if (!currentTask.Contains("="))
            {
                Log.LogWarning("ACTION STOPPED : NO URL WAS SPECIFIED FOR {0}", currentTask);
                return; // stop here
            }

            string[] url = currentTask.ToLower().Split("=");
            if (string.IsNullOrEmpty(url[1]))
            {
                Log.LogWarning("ACTION STOPPED : NO URL WAS SPECIFIED FOR {0}", url[0].ToUpper());
                return; // stop here
            }

            bool checkUri = Uri.IsWellFormedUriString(url[1], UriKind.Absolute);
            if (!checkUri)
            {
                Log.LogWarning("ACTION STOPPED : THIS URL ({0}) IS INVALID OR INCOMPLETE", url[1]);
                return; // stop here
            }

            string photoUrl = url[1];
            if (Regex.IsMatch(photoUrl, @".*\/([\d]+)\/"))
            {
                photoUrl = Regex.Match(url[1], @".*\/([\d]+)\/").ToString();
            }
            else
            {
                if (Regex.IsMatch(photoUrl, @".*\/([\d]+)"))
                {
                   photoUrl = Regex.Match(url[1], @".*\/([\d]+)").ToString() + "/"; 
                }
                else
                {
                    Log.LogWarning("ACTION STOPPED : THIS URL ({0}) IS NOT A PHOTO", photoUrl);
                    return; // stop here
                }
            }
            
            if (Regex.IsMatch(photoUrl, @".*\/([\d]+@n[\d]+)\/"))
            {
                string[] tempUrl = Regex.Split(photoUrl, @"(.*)\/([\d]+@n[\d]+)\/([\d]+)");
                photoUrl = tempUrl[1] + "/" + tempUrl[2].ToUpper() + "/" + tempUrl[3] + "/";
            }
            string favUrl = photoUrl.Remove(photoUrl.Length - 1, 1) + Config.UrlFavorites;

            IEnumerable<string> list = GetContactListFromFavs(favUrl)
                .Except(Data.MyContactsBanned)
                .Except(Data.MyContacts)
                .Except(Data.Myself)
                .ToList(); // Solve

            int c = Data.ContactsToFollow.Count;
            foreach (string needToFollow in list
                .Except(Data.ContactsToFollow))
            {
                Data.ContactsToFollow.Enqueue(needToFollow);
            }
            Log.LogDebug("$ContactsToFollow +{0}", Data.ContactsToFollow.Count - c);

            c = Data.ContactsToFav.Count;
            foreach (string needToFollow in list
                .Except(Data.ContactsToFav))
            {
                Data.ContactsToFav.Enqueue(needToFollow);
            }
            Log.LogDebug("$ContactsToFav +{0}", Data.ContactsToFav.Count - c);
        }

        private void DetectContactsUnfollowBack()
        {
            // contacts not family or friend
            IEnumerable<string> listMutual = GetContactList(Config.UrlContactsMutual);

            //mutual contacts
            string[] listNotClose = GetContactList(Config.UrlContactsNotFriendAndFamily)
                .ToArray(); // fix order by oldest 1st

            if (Data.ContactsToUnfollow.Any())    // all data will be retried, so clear cache if required
            {
                Data.ContactsToUnfollow.Clear();
            }

            string[] result = listNotClose
                .Except(listMutual)
                .Except(MyContactsInTryout)
                .ToArray(); // solve
            int r = result.Length;
            if (r > 0)    // all data will be retried, so clear cache if required
            {
                if (Config.BotKeepSomeUnfollowerContacts > 0 && r > Config.BotKeepSomeUnfollowerContacts)
                {
                    r -= Config.BotKeepSomeUnfollowerContacts;
                }
                foreach (string needToUnfollow in result
                    .Take(r))
                {
                    Data.ContactsToUnfollow.Enqueue(needToUnfollow);
                }
            }
            Log.LogDebug("$ContactsToUnfollow ={0}", Data.ContactsToUnfollow.Count);
        }

        private void DoContactsUnfollow()
        {
            int todo = PseudoRand.Next(Config.BotUnfollowTaskBatchMinLimit, Config.BotUnfollowTaskBatchMaxLimit);
            int c = Data.ContactsToUnfollow.Count;
            while (Data.ContactsToUnfollow.TryDequeue(out string uri) && todo > 0)
            {
                MoveTo(uri);
                try
                {
                    if (Selenium.GetElements(Config.CssContactFollowed).Any()) // manage the already unfollowed like this
                    {
                        Selenium.Click(Config.CssContactFollowed);
                        WaitHumanizer();// the url reload may break a waiting ball

                        Data.MyContacts.Remove(uri);
                        MyContactsInTryout.Remove(uri);
                        Data.MyContactsBanned.Add(uri);
                        todo--;
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning(default, ex, "ACTION STOPPED : {0}", ex.GetBaseException().Message);
                    break; // stop this action
                }
            }
            Log.LogDebug("$ContactsToUnfollow -{0}", c - Data.ContactsToUnfollow.Count);
        }

        private void DetectRecentContactPhotos()
        {
            MoveTo(Config.UrlRecentContactPost, true); // if 1st action post login, the home page may be loaded for your account

            SchroolDownLoop(Config.BotRecentContactPostScrools);

            string[] list = Selenium.GetAttributes(Config.CssRecentContactPost, "href")
                .ToArray();

            int c = Data.PhotosToFav.Count;
            foreach (string needToFav in list
                .Except(Data.PhotosToFav))
            {
                Data.PhotosToFav.Enqueue(needToFav);
            }
            Log.LogDebug("$PhotosToFav +{0}", Data.PhotosToFav.Count - c);
        }

        private void DoPhotosFav()
        {
            int todo = PseudoRand.Next(Config.BotPhotoFavTaskBatchMinLimit, Config.BotPhotoFavTaskBatchMaxLimit);
            int c = Data.PhotosToFav.Count;
            while (Data.PhotosToFav.TryDequeue(out string uri) && todo > 0)
            {
                try
                {
                    PhotoFav(uri, out bool wasFaved, out bool inError);
                    if (inError)
                    {
                        break;
                    }
                    else if (wasFaved)
                    {
                        todo--;
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning(default, ex, "ACTION STOPPED : {0}", ex.GetBaseException().Message);
                    break; // stop this action
                }
            }
            Log.LogDebug("$PhotosToFav -{0}", c - Data.PhotosToFav.Count);
        }
    }
}
