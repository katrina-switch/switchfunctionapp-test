using System;
using System.Runtime.Caching;

namespace WeatherUtility.Helpers
{

    public static class CacheHelper
    {

        private static readonly ObjectCache cache = MemoryCache.Default;

        public static void AddToCache( string key, object value, Priority priority = Priority.Default, int expiryInMinutes = 720 /* 1440 = 1 day */)
        {
            cache.Set( key, value, new CacheItemPolicy
            {
                Priority = ( priority == Priority.Default ) ? CacheItemPriority.Default : CacheItemPriority.NotRemovable,
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes( expiryInMinutes )
            } );
        }

        public static object GetCachedItem( string key )
        {
            return cache[key];
        }

        //public static void RemoveMyCachedItem( String key )
        //{
        //    if ( cache.Contains( key ) )
        //    {
        //        cache.Remove( key );
        //    }
        //}

        public enum Priority
        {
            Default
            
        }

    }

}