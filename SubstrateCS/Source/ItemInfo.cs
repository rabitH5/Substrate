using System;
using System.Collections.Generic;
using Substrate.Nbt;

namespace Substrate
{

    /// <summary>
    /// Provides information on a specific type of item.
    /// </summary>
    /// <remarks>By default, all known MC item types are already defined and registered, assuming Substrate
    /// is up to date with the current MC version.
    /// New item types may be created and used at runtime, and will automatically populate various static lookup tables
    /// in the <see cref="ItemInfo"/> class.</remarks>
    public class ItemInfo
    {
        private static Random _rand = new Random();

        private class CacheTableDict<T> : ICacheTable<T>
        {
            private Dictionary<int, T> _cache;

            public T this[int index]
            {
                get
                {
                    T val;
                    if (_cache.TryGetValue(index, out val))
                    {
                        return val;
                    }
                    return default(T);
                }
            }

            public CacheTableDict(Dictionary<int, T> cache)
            {
                _cache = cache;
            }
        }

        private static Dictionary<int, ItemInfo> _itemTable;

        private int _id = 0;
        private string _name = "";
        private int _stack = 1;

        private static CacheTableDict<ItemInfo> _itemTableCache;

        /// <summary>
        /// Gets the lookup table for id-to-info values.
        /// </summary>
        public static ICacheTable<ItemInfo> ItemTable
        {
            get { return _itemTableCache; }
        }

        /// <summary>
        /// Gets the id of the item type.
        /// </summary>
        public int ID
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the name of the item type.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the maximum stack size allowed for this item type.
        /// </summary>
        public int StackSize
        {
            get { return _stack; }
        }

        /// <summary>
        /// Constructs a new <see cref="ItemInfo"/> record for the given item id.
        /// </summary>
        /// <param name="id">The id of an item type.</param>
        public ItemInfo(int id)
        {
            _id = id;
            _itemTable[_id] = this;
        }

        /// <summary>
        /// Constructs a new <see cref="ItemInfo"/> record for the given item id and name.
        /// </summary>
        /// <param name="id">The id of an item type.</param>
        /// <param name="name">The name of an item type.</param>
        public ItemInfo(int id, string name)
        {
            _id = id;
            _name = name;
            _itemTable[_id] = this;
        }

        /// <summary>
        /// Sets the maximum stack size for this item type.
        /// </summary>
        /// <param name="stack">A stack size between 1 and 64, inclusive.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        public ItemInfo SetStackSize(int stack)
        {
            _stack = stack;
            return this;
        }

        /// <summary>
        /// Chooses a registered item type at random and returns it.
        /// </summary>
        /// <returns></returns>
        public static ItemInfo GetRandomItem()
        {
            List<ItemInfo> list = new List<ItemInfo>(_itemTable.Values);
            return list[_rand.Next(list.Count)];
        }

        //> @rabitH5
        public static void ResetAllData()
        {
            _itemTable = new Dictionary<int, ItemInfo>();
            _itemTableCache = new CacheTableDict<ItemInfo>(_itemTable);
        }

        public void SetName(string newName)
        {
            _name = newName;
        }

        // -------------------------------------------------------------------------------------------------
        //< @rabitH5

    }
}
