using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tdf.RedisCache;

namespace Tests.RedisCache
{
    public static class RedisBaseHelper
    {
        #region 字符串类型
        public static void TestRedis()
        {
            // 基本KEY/VALUE键值对操作；
            RedisBase.Item_Set("examCourseId", "00889750EE7448DB9B388E9146E5AC62");
            RedisBase.Item_Set("batchId", "89B0BD4B5CAC49259E60E1D8A6EDD858");

            var examCourseId = RedisBase.Item_Get<string>("examCourseId");
            var batchId = RedisBase.Item_Get<string>("batchId");

            Console.WriteLine("examCourseId：" + examCourseId);
            Console.WriteLine("batchId：" + batchId);

            // 存储对象；
            RedisBase.Item_Set<UserInfo>("userinfo", new UserInfo() {UserName = "李四", Age = 45});
            var userinfo = RedisBase.Item_Get<UserInfo>("userinfo");

            var id = userinfo.Id;
            var userName = userinfo.UserName;
            var age = userinfo.Age;

            Console.WriteLine("userinfo：" + userinfo);
            Console.WriteLine("id：" + id + "，userName：" + userName + ",age：" + age);

            // List；
            RedisBase.List_Add("userinfolist", new UserInfo() {Id = 1, UserName = "Bobby", Age = 36});
            RedisBase.List_Add("userinfolist", new UserInfo() {Id = 2, UserName = "Samba", Age = 30});

            var userList = RedisBase.List_GetList<UserInfo>("userinfolist");

            foreach (var user in userList)
            {
                Console.WriteLine("id：" + user.Id + "，userName：" + user.UserName + ",age：" + user.Age);
            }


            RedisBase.Item_Remove("examCourseId");

            Console.Read();
        }
        #endregion
    }


    [Serializable]
    public class UserInfo
    {
        public long Id { set; get; }
        public string UserName { get; set; }
        public int Age { get; set; }
    }
}


/*
 * redis是一个key-value存储系统。
 * 和Memcached类似，它支持存储的value类型相对更多，
 * 包括string(字符串)、list(链表)、set(集合)、zset(sorted set --有序集合)和hash（哈希类型）。
 * 这些数据类型都支持push/pop、add/remove及取交集并集和差集及更丰富的操作，
 * 而且这些操作都是原子性的。在此基础上，redis支持各种不同方式的排序。
 * 与memcached一样，为了保证效率，数据都是缓存在内存中。
 * 区别的是redis会周期性的把更新的数据写入磁盘或者把修改操作写入追加的记录文件，
 * 并且在此基础上实现了master-slave(主从)同步。
 * 
 * 可以使用redis desktop manager管理工具查看服务器缓存中的数据.
 * 过程: 数据写到master–>master存储到slave的rdb中–>slave加载rdb到内存。 
 * string是很好的存储方式，用来做计数存储。sets用于建立索引库非常棒；
 * 
 */
