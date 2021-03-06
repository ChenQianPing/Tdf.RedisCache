﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Redis;

namespace Tdf.RedisCache
{
    /// <summary>
    /// 基于ServiceStack.Redis，Redis缓存读取设置封装
    /// </summary>
    public class RedisBase
    {
        // Redis写入服务器地址,可以添加多个服务器通过;分隔
        private static readonly string[] ReadWriteHosts = ConfigurationManager.AppSettings["readWriteHosts"].Split(new char[] { ';' });
        // Redis读服务器地址,可以添加多个服务器通过;分隔
        private static readonly string[] ReadOnlyHosts = ConfigurationManager.AppSettings["readOnlyHosts"].Split(new char[] { ';' });

        #region -- 连接信息 --
        public static PooledRedisClientManager Prcm = CreateManager(ReadWriteHosts, ReadOnlyHosts);

        private static PooledRedisClientManager CreateManager(IEnumerable<string> readWriteHosts, IEnumerable<string> readOnlyHosts)
        {
            // 支持读写分离，均衡负载  
            return new PooledRedisClientManager(readWriteHosts, readOnlyHosts, new RedisClientManagerConfig
            {
                MaxWritePoolSize = 5, // “写”链接池链接数  
                MaxReadPoolSize = 5,  // “读”链接池链接数  
                AutoStart = true,
            });
        }
        #endregion

        #region -- Item --
        /// <summary>
        /// 设置单体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool Item_Set<T>(string key, T t)
        {
            try
            {
                using (var redis = Prcm.GetClient())
                {
                    return redis.Set<T>(key, t, new TimeSpan(1, 0, 0));
                }
            }
            catch (Exception)
            {
                // ignored
            }
            return false;
        }

        /// <summary>
        /// 获取单体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Item_Get<T>(string key) where T : class
        {
            using (var redis = Prcm.GetClient())
            {
                return redis.Get<T>(key);
            }
        }

        /// <summary>
        /// 移除单体
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Item_Remove(string key)
        {
            using (var redis = Prcm.GetClient())
            {
                return redis.Remove(key);
            }
        }
        #endregion

        #region -- List --

        public static void List_Add<T>(string key, T t)
        {
            using (var redis = Prcm.GetClient())
            {
                var redisTypedClient = redis.GetTypedClient<T>();
                redisTypedClient.AddItemToList(redisTypedClient.Lists[key], t);
            }
        }

        public static bool List_Remove<T>(string key, T t)
        {
            using (var redis = Prcm.GetClient())
            {
                var redisTypedClient = redis.GetTypedClient<T>();
                return redisTypedClient.RemoveItemFromList(redisTypedClient.Lists[key], t) > 0;
            }
        }
        public static void List_RemoveAll<T>(string key)
        {
            using (var redis = Prcm.GetClient())
            {
                var redisTypedClient = redis.GetTypedClient<T>();
                redisTypedClient.Lists[key].RemoveAll();
            }
        }

        public static int List_Count(string key)
        {
            using (var redis = Prcm.GetReadOnlyClient())
            {
                return redis.GetListCount(key);
            }
        }

        public static List<T> List_GetRange<T>(string key, int start, int count)
        {
            using (var redis = Prcm.GetReadOnlyClient())
            {
                var c = redis.GetTypedClient<T>();
                return c.Lists[key].GetRange(start, start + count - 1);
            }
        }


        public static List<T> List_GetList<T>(string key)
        {
            using (var redis = Prcm.GetReadOnlyClient())
            {
                var c = redis.GetTypedClient<T>();
                return c.Lists[key].GetRange(0, c.Lists[key].Count);
            }
        }

        public static List<T> List_GetList<T>(string key, int pageIndex, int pageSize)
        {
            var start = pageSize * (pageIndex - 1);
            return List_GetRange<T>(key, start, pageSize);
        }

        /// <summary> 
        /// 设置缓存过期 
        /// </summary> 
        /// <param name="key"></param> 
        /// <param name="datetime"></param> 
        public static void List_SetExpire(string key, DateTime datetime)
        {
            using (var redis = Prcm.GetClient())
            {
                redis.ExpireEntryAt(key, datetime);
            }
        }
        #endregion

        #region -- Set --
        public static void Set_Add<T>(string key, T t)
        {
            using (var redis = Prcm.GetClient())
            {
                var redisTypedClient = redis.GetTypedClient<T>();
                redisTypedClient.Sets[key].Add(t);
            }
        }

        public static bool Set_Contains<T>(string key, T t)
        {
            using (var redis = Prcm.GetClient())
            {
                var redisTypedClient = redis.GetTypedClient<T>();
                return redisTypedClient.Sets[key].Contains(t);
            }
        }

        public static bool Set_Remove<T>(string key, T t)
        {
            using (var redis = Prcm.GetClient())
            {
                var redisTypedClient = redis.GetTypedClient<T>();
                return redisTypedClient.Sets[key].Remove(t);
            }
        }
        #endregion

        #region -- Hash --
        /// <summary> 
        /// 判断某个数据是否已经被缓存 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="key"></param> 
        /// <param name="dataKey"></param> 
        /// <returns></returns> 
        public static bool Hash_Exist<T>(string key, string dataKey)
        {
            using (var redis = Prcm.GetReadOnlyClient())
            {
                return redis.HashContainsEntry(key, dataKey);
            }
        }

        /// <summary> 
        /// 存储数据到hash表 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="key"></param> 
        /// <param name="dataKey"></param> 
        /// <returns></returns> 
        public static bool Hash_Set<T>(string key, string dataKey, T t)
        {
            using (var redis = Prcm.GetClient())
            {
                var value = ServiceStack.Text.JsonSerializer.SerializeToString<T>(t);
                return redis.SetEntryInHash(key, dataKey, value);
            }
        }

        /// <summary> 
        /// 移除hash中的某值 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="key"></param> 
        /// <param name="dataKey"></param> 
        /// <returns></returns> 
        public static bool Hash_Remove(string key, string dataKey)
        {
            using (var redis = Prcm.GetClient())
            {
                return redis.RemoveEntryFromHash(key, dataKey);
            }
        }

        /// <summary>
        /// 移除整个hash
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Hash_Remove(string key)
        {
            using (var redis = Prcm.GetClient())
            {
                return redis.Remove(key);
            }
        }

        /// <summary> 
        /// 从hash表获取数据 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="key"></param> 
        /// <param name="dataKey"></param> 
        /// <returns></returns> 
        public static T Hash_Get<T>(string key, string dataKey)
        {
            using (var redis = Prcm.GetReadOnlyClient())
            {
                var value = redis.GetValueFromHash(key, dataKey);
                return ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(value);
            }
        }

        /// <summary> 
        /// 获取整个hash的数据 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="key"></param> 
        /// <returns></returns> 
        public static List<T> Hash_GetAll<T>(string key)
        {
            using (var redis = Prcm.GetReadOnlyClient())
            {
                var list = redis.GetHashValues(key);
                if (list != null && list.Count > 0)
                {
                    var result = new List<T>();
                    foreach (var item in list)
                    {
                        var value = ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(item);
                        result.Add(value);
                    }
                    return result;
                }
                return null;
            }
        }

        /// <summary> 
        /// 设置缓存过期 
        /// </summary> 
        /// <param name="key"></param> 
        /// <param name="datetime"></param> 
        public static void Hash_SetExpire(string key, DateTime datetime)
        {
            using (var redis = Prcm.GetClient())
            {
                redis.ExpireEntryAt(key, datetime);
            }
        }

        /// <summary> 
        /// 获取Hash集合数量 
        /// </summary> 
        /// <param name="key">Hashid</param> 
        public static int Hash_GetCount(string key)
        {
            using (var redis = Prcm.GetReadOnlyClient())
            {
                return redis.GetHashCount(key);
            }
        }
        #endregion

        #region -- SortedSet --
        /// <summary> 
        ///  添加数据到 SortedSet 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="key"></param> 
        /// <param name="t"></param> 
        /// <param name="score"></param> 
        public static bool SortedSet_Add<T>(string key, T t, double score)
        {
            using (var redis = Prcm.GetClient())
            {
                var value = ServiceStack.Text.JsonSerializer.SerializeToString<T>(t);
                return redis.AddItemToSortedSet(key, value, score);
            }
        }

        /// <summary> 
        /// 移除数据从SortedSet 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="key"></param> 
        /// <param name="t"></param> 
        /// <returns></returns> 
        public static bool SortedSet_Remove<T>(string key, T t)
        {
            using (var redis = Prcm.GetClient())
            {
                var value = ServiceStack.Text.JsonSerializer.SerializeToString<T>(t);
                return redis.RemoveItemFromSortedSet(key, value);
            }
        }

        /// <summary> 
        /// 修剪SortedSet 
        /// </summary> 
        /// <param name="key"></param> 
        /// <param name="size">保留的条数</param> 
        /// <returns></returns> 
        public static int SortedSet_Trim(string key, int size)
        {
            using (var redis = Prcm.GetClient())
            {
                return redis.RemoveRangeFromSortedSet(key, size, 9999999);
            }
        }

        /// <summary> 
        /// 获取SortedSet的长度 
        /// </summary> 
        /// <param name="key"></param> 
        /// <returns></returns> 
        public static int SortedSet_Count(string key)
        {
            using (var redis = Prcm.GetReadOnlyClient())
            {
                return redis.GetSortedSetCount(key);
            }
        }

        /// <summary> 
        /// 获取SortedSet的分页数据 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="key"></param> 
        /// <param name="pageIndex"></param> 
        /// <param name="pageSize"></param> 
        /// <returns></returns> 
        public static List<T> SortedSet_GetList<T>(string key, int pageIndex, int pageSize)
        {
            using (var redis = Prcm.GetReadOnlyClient())
            {
                var list = redis.GetRangeFromSortedSet(key, (pageIndex - 1) * pageSize, pageIndex * pageSize - 1);
                if (list != null && list.Count > 0)
                {
                    var result = new List<T>();
                    foreach (var item in list)
                    {
                        var data = ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(item);
                        result.Add(data);
                    }
                    return result;
                }
            }
            return null;
        }

        /// <summary> 
        /// 获取SortedSet的全部数据 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="key"></param> 
        /// <returns></returns> 
        public static List<T> SortedSet_GetListALL<T>(string key)
        {
            using (var redis = Prcm.GetReadOnlyClient())
            {
                var list = redis.GetRangeFromSortedSet(key, 0, 9999999);
                if (list != null && list.Count > 0)
                {
                    var result = new List<T>();
                    foreach (var item in list)
                    {
                        var data = ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(item);
                        result.Add(data);
                    }
                    return result;
                }
            }
            return null;
        }

        /// <summary> 
        /// 设置缓存过期 
        /// </summary> 
        /// <param name="key"></param> 
        /// <param name="datetime"></param> 
        public static void SortedSet_SetExpire(string key, DateTime datetime)
        {
            using (var redis = Prcm.GetClient())
            {
                redis.ExpireEntryAt(key, datetime);
            }
        }
        #endregion
    }
}
