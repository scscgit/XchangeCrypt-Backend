using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using System.Threading;

namespace XchangeCrypt.Backend.ConvergenceBackend.Models.Authentication
{
    public class MSALSessionCache
    {
        private static ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private string UserId = string.Empty;
        private string CacheId = string.Empty;
        private HttpContext httpContext = null;

        private TokenCache cache = new TokenCache();

        public MSALSessionCache(string userId, HttpContext httpcontext)
        {
            // not object, we want the SUB
            UserId = userId;
            CacheId = UserId + "_TokenCache";
            httpContext = httpcontext;
            Load();
        }

        public TokenCache GetMsalCacheInstance()
        {
            cache.SetBeforeAccess(BeforeAccessNotification);
            cache.SetAfterAccess(AfterAccessNotification);
            Load();
            return cache;
        }

        public void SaveUserStateValue(string state)
        {
            SessionLock.EnterWriteLock();
            httpContext.Session.SetString(CacheId + "_state", state);
            SessionLock.ExitWriteLock();
        }

        public string ReadUserStateValue()
        {
            string state = string.Empty;
            SessionLock.EnterReadLock();
            state = (string)httpContext.Session.GetString(CacheId + "_state");
            SessionLock.ExitReadLock();
            return state;
        }

        public void Load()
        {
            SessionLock.EnterReadLock();
            cache.Deserialize(httpContext.Session.Get(CacheId));
            SessionLock.ExitReadLock();
        }

        public void Persist()
        {
            SessionLock.EnterWriteLock();

            // Optimistically set HasStateChanged to false. We need to do it early to avoid losing changes made by a concurrent thread.
            cache.HasStateChanged = false;

            // Reflect changes in the persistent store
            httpContext.Session.Set(CacheId, cache.Serialize());
            SessionLock.ExitWriteLock();
        }

        // Triggered right before MSAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        // Triggered right after MSAL accessed the cache.
        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (cache.HasStateChanged)
            {
                Persist();
            }
        }
    }
}
