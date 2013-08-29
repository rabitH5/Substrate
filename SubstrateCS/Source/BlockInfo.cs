using System;
using System.Collections.Generic;
using Substrate.Nbt;
using org.apache.commons.lang;
using org.bukkit.material;
using org.bukkit;

namespace Substrate
{


    /// <summary>
    /// Represents the physical state of a block, such as solid or fluid.
    /// </summary>
    public enum BlockState
    {
        /// <summary>
        /// A solid state that stops movement.
        /// </summary>
        SOLID,

        /// <summary>
        /// A nonsolid state that can be passed through.
        /// </summary>
        NONSOLID,

        /// <summary>
        /// A fluid state that flows and impedes movement.
        /// </summary>
        FLUID
    }

    /// <summary>
    /// Provides information on a specific type of block.
    /// </summary>
    /// <remarks>By default, all known MC block types are already defined and registered, assuming Substrate
    /// is up to date with the current MC version.  All unknown blocks are given a default type and unregistered status.
    /// New block types may be created and used at runtime, and will automatically populate various static lookup tables
    /// in the <see cref="BlockInfo"/> class.</remarks>
    public class BlockInfo : Material
    {
        /// <summary>
        /// The maximum number of sequential blocks starting at 0 that can be registered.
        /// </summary>
        public const int MAX_BLOCKS = 4096;

        /// <summary>
        /// The maximum opacity value that can be assigned to a block (fully opaque).
        /// </summary>
        public const int MAX_OPACITY = 15;

        /// <summary>
        /// The minimum opacity value that can be assigned to a block (fully transparent).
        /// </summary>
        public const int MIN_OPACITY = 0;

        /// <summary>
        /// The maximum luminance value that can be assigned to a block.
        /// </summary>
        public const int MAX_LUMINANCE = 15;

        /// <summary>
        /// The minimum luminance value that can be assigned to a block.
        /// </summary>
        public const int MIN_LUMINANCE = 0;

        private static BlockInfo[] _blockTable; //> @rabitH5  readonly
        private static int[] _opacityTable; //> @rabitH5  readonly
        private static int[] _luminanceTable; //> @rabitH5  readonly

        private class CacheTableArray<T> : ICacheTable<T>
        {
            private T[] _cache;

            public T this[int index]
            {
                get { return _cache[index]; }
            }

            public CacheTableArray(T[] cache)
            {
                _cache = cache;
            }
        }

        public class DataLimits
        {
            private int _low;
            private int _high;
            private int _bitmask;

            public int Low
            {
                get { return _low; }
            }

            public int High
            {
                get { return _high; }
            }

            public int Bitmask
            {
                get { return _bitmask; }
            }

            public DataLimits(int low, int high, int bitmask)
            {
                _low = low;
                _high = high;
                _bitmask = bitmask;
            }

            public bool Test(int data)
            {
                int rdata = data & ~_bitmask;
                return rdata >= _low && rdata <= _high;
            }
        }

        private int _id = 0;
        private string _name = "";
        private int _tick = 0;
        private int _opacity = MAX_OPACITY;
        private int _luminance = MIN_LUMINANCE;
        private bool _transmitLight = false;
        private bool _stopFluid = true;
        private bool _registered = false;

        private BlockState _state = BlockState.SOLID;

        private DataLimits _dataLimits = new DataLimits(0,0,0);

        private static CacheTableArray<BlockInfo> _blockTableCache; //> @rabitH5  readonly
        private static CacheTableArray<int> _opacityTableCache; //> @rabitH5  readonly
        private static CacheTableArray<int> _luminanceTableCache;//> @rabitH5  readonly

        /// <summary>
        /// Gets the lookup table for id-to-info values.
        /// </summary>
        public static ICacheTable<BlockInfo> BlockTable
        {
            get { return _blockTableCache; }
        }

        /// <summary>
        /// Gets the lookup table for id-to-opacity values.
        /// </summary>
        public static ICacheTable<int> OpacityTable
        {
            get { return _opacityTableCache; }
        }

        /// <summary>
        /// Gets the lookup table for id-to-luminance values.
        /// </summary>
        public static ICacheTable<int> LuminanceTable
        {
            get { return _luminanceTableCache; }
        }

        /// <summary>
        /// Get's the block's Id.
        /// </summary>
        public int ID
        {
            get { return _id; }
            set
            {
                if (OnIdChange != null) {
                    OnIdChange(_id, value);
                }

                _id = value;
                _blockTable[_id] = this;
                _dropId = _id;
            }
        }

        /// <summary>
        /// Get's the name of the block type.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                _registered = true;
                UpdateNameLookup(_id);

                if (OnNameChange != null)
                    OnNameChange(_id, _name);
            }
        }

        /// <summary>
        /// Gets the block's opacity value.  An opacity of 0 is fully transparent to light.
        /// </summary>
        public int Opacity
        {
            get { return _opacity; }
            set { SetOpacity(value); }
        }

        /// <summary>
        /// Gets the block's luminance value.
        /// </summary>
        /// <remarks>Blocks with luminance act as light sources and transmit light to other blocks.</remarks>
        public int Luminance
        {
            get { return _luminance; }
            set { SetLuminance(value); }
        }

        /// <summary>
        /// Checks whether the block transmits light to neighboring blocks.
        /// </summary>
        /// <remarks>A block may stop the transmission of light, but still be illuminated.</remarks>
        public bool TransmitsLight
        {
            get { return _transmitLight; }
            set { SetLightTransmission(value); }
        }

        /// <summary>
        /// Checks whether the block partially or fully blocks the transmission of light.
        /// </summary>
        public bool ObscuresLight
        {
            get { return _opacity > MIN_OPACITY || !_transmitLight; }
        }

        /// <summary>
        /// Checks whether the block stops fluid from passing through it.
        /// </summary>
        /// <remarks>A block that does not block fluids will be destroyed by fluid.</remarks>
        public bool StopFluid
        {
            get { return _stopFluid; }
            set { SetBlocksFluid(value); }
        }

        /// <summary>
        /// Gets the block's physical state type.
        /// </summary>
        public BlockState State
        {
            get { return _state; }
            set { SetState(value); }
        }

        public DataLimits Data
        {
            get { return _dataLimits; }
            set { _dataLimits = value; }
        }

        /// <summary>
        /// Checks whether this block type has been registered as a known type.
        /// </summary>
        public bool Registered
        {
            get { return _registered; }
            set { _registered = value; }
        }

        public int Tick
        {
            get { return _tick; }
            set { SetTick(value); }
        }

        public BlockInfo()
        {
        }

        public BlockInfo(int id)
        {
            _name = "Unknown Block";
            ID = id;
        }

        /// <summary>
        /// Constructs a new <see cref="BlockInfo"/> record for a given block id and name.
        /// </summary>
        /// <param name="id">The id of the block.</param>
        /// <param name="name">The name of the block.</param>
        /// <remarks>All user-constructed <see cref="BlockInfo"/> objects are registered automatically.</remarks>
        public BlockInfo(int id, string name)
        {
            ID = id;
            Name = name;
        }

        /// <summary>
        /// Sets a new opacity value for this block type.
        /// </summary>
        /// <param name="opacity">A new opacity value.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        /// <seealso cref="AlphaBlockCollection.AutoLight"/>
        public BlockInfo SetOpacity(int opacity)
        {
            _opacity = MIN_OPACITY + opacity;
            _opacityTable[_id] = _opacity;
            /*
            if (opacity == MAX_OPACITY)
            {
                _transmitLight = false;
            }
            else
            {
                _transmitLight = true;
            }*/

            UpdateOpacityLookup(_id);

            return this;
        }

        /// <summary>
        /// Sets a new luminance value for this block type.
        /// </summary>
        /// <param name="luminance">A new luminance value.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        /// <seealso cref="AlphaBlockCollection.AutoLight"/>
        public BlockInfo SetLuminance(int luminance)
        {
            _luminance = luminance;
            _luminanceTable[_id] = _luminance;
            return this;
        }

        /// <summary>
        /// Sets whether or not this block type will transmit light to neigboring blocks.
        /// </summary>
        /// <param name="transmit">True if this block type can transmit light to neighbors, false otherwise.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        /// <seealso cref="AlphaBlockCollection.AutoLight"/>
        public BlockInfo SetLightTransmission(bool transmit)
        {
            _transmitLight = transmit;
            return this;
        }

        /// <summary>
        /// Sets limitations on what data values are considered valid for this block type.
        /// </summary>
        /// <param name="low">The lowest valid integer value.</param>
        /// <param name="high">The highest valid integer value.</param>
        /// <param name="bitmask">A mask representing which bits are interpreted as a bitmask in the data value.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        public BlockInfo SetDataLimits(int low, int high, int bitmask)
        {
            _dataLimits = new DataLimits(low, high, bitmask);
            return this;
        }

        /// <summary>
        /// Sets the physical state of the block type.
        /// </summary>
        /// <param name="state">A physical state.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        public BlockInfo SetState(BlockState state)
        {
            _state = state;

            /* if (_state == BlockState.SOLID)
             {
                 _blocksFluid = true;
             }
             else
             {
                 _blocksFluid = false;
             }*/

            return this;
        }

        /// <summary>
        /// Sets whether or not this block type blocks fluids.
        /// </summary>
        /// <param name="blocks">True if this block type blocks fluids, false otherwise.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        /// <seealso cref="AlphaBlockCollection.AutoFluid"/>
        public BlockInfo SetBlocksFluid(bool blocks)
        {
            _stopFluid = blocks;
            return this;
        }

        /// <summary>
        /// Sets the default tick rate/delay used for updating this block.
        /// </summary>
        /// <remarks>Set <paramref name="tick"/> to <c>0</c> to indicate that this block is not processed by tick updates.</remarks>
        /// <param name="tick">The tick rate in frames between scheduled updates on this block.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        /// <seealso cref="AlphaBlockCollection.AutoTileTick"/>
        public BlockInfo SetTick(int tick)
        {
            _tick = tick;
            return this;
        }

        /// <summary>
        /// Tests if the given data value is valid for this block type.
        /// </summary>
        /// <param name="data">A data value to test.</param>
        /// <returns>True if the data value is valid, false otherwise.</returns>
        /// <remarks>This method uses internal information set by <see cref="SetDataLimits"/>.</remarks>
        public bool TestData(int data)
        {
            if (_dataLimits == null)
            {
                return true;
            }
            return _dataLimits.Test(data);
        }

        //> @rabitH5


        public static System.Action<int,string> OnNameChange;
        public static System.Action<int, int> OnIdChange;

        // -------------------------------------------------------------------------------------------------
        // Bukkit values
        // -------------------------------------------------------------------------------------------------

        private int _maxStack = 50;
        private short _durability = 100;
        private bool _isEdible = false;
        private bool _isFlammable= false;
        private bool _isBurnable= false;
        private bool _hasGravity = true;
        private bool _isWalkable = true;
        private bool _isPassable = true;
        private bool _isTeleportable = true;
        private bool _breakNaturally = false;

        public int MaxStack
        {
            get { return _maxStack; }
            set { _maxStack = value; }
        }

        public short Durability
        {
            get { return _durability; }
            set { _durability = value; }
        }

        public bool IsEdible
        {
            get { return _isEdible; }
            set { _isEdible = value; }
        }

        public bool IsFlammable
        {
            get { return _isFlammable; }
            set { _isFlammable = value; }
        }
        public bool IsBurnable
        {
            get { return _isBurnable; }
            set { _isBurnable = value; }
        }
        public bool HasGravity
        {
            get { return _hasGravity; }
            set { _hasGravity = value; }
        }
        public bool IsWalkable
        {
            get { return _isWalkable; }
            set { _isWalkable = value; }
        }
        public bool IsPassable
        {
            get { return _isPassable; }
            set { _isPassable = value; }
        }
        public bool IsTeleportable
        {
            get { return _isTeleportable; }
            set { _isTeleportable = value; }
        }
        public bool BreakNaturally
        {
            get { return _breakNaturally; }
            set { _breakNaturally = value; }
        }

        // -------------------------------------------------------------------------------------------------
        // Bukkit values
        // -------------------------------------------------------------------------------------------------

        private GeometryType _geometryType = GeometryType.Cube;
        private int _hitPoints = 100;
        private bool _dropItems = false;
        private PropType _dropType = PropType.Block;
        private int _dropId = 0;
        private int _dropChance = 100;

        public GeometryType Geometry
        {
            get { return _geometryType; }
            set { _geometryType = value; }
        }

        public int HitPoints
        {
            get { return _hitPoints; }
            set { _hitPoints = value; }
        }

        public bool DropItems
        {
            get { return _dropItems; }
            set { _dropItems = value; }
        }

        public PropType DropType
        {
            get { return _dropType; }
            set { _dropType = value; }
        }

        public int DropId
        {
            get { return _dropId; }
            set {
                int newValue = value;

                if (newValue < 0)
                    newValue = 0;

                if (newValue > 100)
                    newValue = 100;

                    _dropId = newValue;
            }
        }

        public int DropChance
        {
            get { return _dropChance; }
            set { _dropChance = value; }
        }

        // -------------------------------------------------------------------------------------------------
        // Bukkit implementation
        // -------------------------------------------------------------------------------------------------

        private static Dictionary<string, Material> BY_NAME = new Dictionary<string, Material>();

        public int getId()
        {
            return _id;
        }

        public int getMaxStackSize()
        {
            return _maxStack;
        }

        public short getMaxDurability()
        {
            return _durability;
        }

        public MaterialData getData()
        {
            throw new NotImplementedException();
        }

        public MaterialData getNewData(byte raw)
        {
            throw new NotImplementedException();
        }

        public bool isBlock() { return true; }

        public bool isEdible() { return _isEdible; }

        public bool isSolid()
        {
            if (ID == BlockType.AIR)
                return false;
            if (State == BlockState.SOLID)
                return true;
            if (State == BlockState.FLUID || State == BlockState.NONSOLID)
                return false;

            return false;
        }

        public bool isLiquid()
        {
            if (State == BlockState.FLUID)
                return true;
            return false;
        }

        public bool isEmpty()
        {
            if (ID == BlockType.AIR)
                return true;

            return false;
        }

        public bool isTransparent()
        {
            return !isOccluding();
        }

        public bool isFlammable()
        {
            return _isFlammable;
        }

        public bool isBurnable()
        {
            return _isBurnable;
        }

        public bool isOccluding()
        {
            return _isOpaqueTable[ID];
        }

        public bool hasGravity() { return _hasGravity; }

        public bool isWalkable() { return _isWalkable; }

        public bool isPassable() { return _isPassable; }

        public bool isTeleportable() { return _isTeleportable; }

        public bool breakNaturally() { return _breakNaturally; }

        public bool equals(int blockId)
        {
            if (ID == blockId)
                return true;
            return false;
        }

        public bool equals(Material material)
        {
            if (ID == material.getId())
                return true;
            return false;
        }

        // -------------------------------------------------------------------------------------------------
        // Utilities
        // -------------------------------------------------------------------------------------------------

        public static BlockInfo getBlockInfo(int id)
        {
            if (id < MAX_BLOCKS && id >= 0)
                return _blockTable[id];
            else
                return null;
        }

        public static bool IsRegistred(int id)
        {
            return _blockTable[id].Registered;
        }

        public static BlockInfo getBlockInfoDirect(int id)
        {
            return _blockTable[id];
        }

        public static Material getMaterial(int id)
        {
            if (id < MAX_BLOCKS && id >= 0)
                return _blockTable[id];
            else
                return null;
        }

        public static Material getMaterial(string name)
        {
            return BY_NAME[name];
        }

        public static Material matchMaterial(string name)
        {
            Validate.notNull(name, "Name cannot be null");

            Material result = null;

            try
            {
                result = getMaterial(Convert.ToInt32(name));
            }
            catch (System.FormatException ex) { }

            if (result == null)
            {
                String filtered = name.ToUpper();

                filtered = filtered.Replace("\\s+", "_").Replace("\\W", "");
                result = BY_NAME[filtered];
            }

            return result;
        }

        public bool canBeReplacedByLeaves()
        {
            return isTransparent();
        }

        public bool isLeaves()
        {
            return this.ID == BlockType.LEAVES;
        }

        public static int FindNextAvailableId(int min, int max)
        {
            if (min < 0 || min > MAX_BLOCKS) min = 0;
            if (max < 0 || max > MAX_BLOCKS) max = MAX_BLOCKS;

            for (int i = min; i <= max; i++)
            {
                if (!_blockTable[i].Registered)
                    return i;
            }
            return -1;
        }

        public static void AddBlock(int id, string name)
        {
            _blockTable[id] = new BlockInfo(id, name);
            UpdateLookups();
        }

        public static void RemoveBlock(int id)
        {
            _blockTable[id] = new BlockInfo(id);
            UpdateLookups();
        }

        // -------------------------------------------------------------------------------------------------
        // Setup
        // -------------------------------------------------------------------------------------------------

        public static string nullBlockName = "Unknown Block";
        private static bool[] _isOpaqueTable;

        public static void ResetAllData()
        {
            _blockTable = new BlockInfo[MAX_BLOCKS];
            _opacityTable = new int[MAX_BLOCKS];
            _luminanceTable = new int[MAX_BLOCKS];

            _blockTableCache = new CacheTableArray<BlockInfo>(_blockTable);
            _opacityTableCache = new CacheTableArray<int>(_opacityTable);
            _luminanceTableCache = new CacheTableArray<int>(_luminanceTable);

            //> @rabitH5
            // ------------------------------
            // Custom setup
            // ------------------------------

            _isOpaqueTable = new bool[MAX_BLOCKS];

        }

        /// <summary>
        /// Must be called whenever all the blocks have been defined.
        /// </summary>
        public static void FinishSetup()
        {

            for (int i = 0; i < MAX_BLOCKS; i++)
            {
                if (_blockTable[i] == null)
                {
                    _blockTable[i] = new BlockInfo(i);
                }
            }

            FillEmptyBlockData();

            UpdateLookups();
        }

        private static void FillEmptyBlockData()
        {
            for (int i = 0; i < MAX_BLOCKS; i++)
            {
                if (_blockTable[i] == null)
                {
                    _blockTable[i] = new BlockInfo(i);
                }
            }
        }


        private static void UpdateLookups()
        {
            UpdateOpacityLookup();
            UpdateNameLookup();
        }

        private static void UpdateOpacityLookup()
        {
            for (int i = 0; i < MAX_BLOCKS; i++)
            {
                UpdateOpacityLookup(i);
            }
        }

        private static void UpdateOpacityLookup(int blockId)
        {
            if (_blockTable[blockId].Opacity == BlockInfo.MAX_OPACITY)
                _isOpaqueTable[blockId] = true;
            else
                _isOpaqueTable[blockId] = false;
        }

        private static void UpdateNameLookup()
        {

            for (int i = 0; i < MAX_BLOCKS; i++)
            {
                UpdateNameLookup(i);
            }
        }

        private static void UpdateNameLookup(int blockId)
        {

            BlockInfo info = _blockTable[blockId];

            if (info.Name == nullBlockName)
                return;

            string formatedName = info.Name.ToUpper();

            if (BY_NAME.ContainsKey(formatedName))
                BY_NAME[formatedName] = info;
            else
                BY_NAME.Add(formatedName, info);

        }
    }

    public enum GeometryType : byte
    {
        None,
        Cube,
        Slab,
        X,
        Crust,
        Liquid
    }

    // -------------------------------------------------------------------------------------------------
    //< @rabitH5

    /// <summary>
    /// An extended <see cref="BlockInfo"/> that includes <see cref="TileEntity"/> information.
    /// </summary>
    public class BlockInfoEx : BlockInfo
    {
        private string _tileEntityName;

        /// <summary>
        /// Gets the name of the <see cref="TileEntity"/> type associated with this block type.
        /// </summary>
        public string TileEntityName
        {
            get { return _tileEntityName; }
        }

        internal BlockInfoEx(int id) : base(id) { }

        /// <summary>
        /// Constructs a new <see cref="BlockInfoEx"/> with a given block id and name.
        /// </summary>
        /// <param name="id">The id of the block type.</param>
        /// <param name="name">The name of the block type.</param>
        public BlockInfoEx(int id, string name) : base(id, name) { }

        /// <summary>
        /// Sets the name of the <see cref="TileEntity"/> type associated with this block type.
        /// </summary>
        /// <param name="name">The name of a registered <see cref="TileEntity"/> type.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        /// <seealso cref="TileEntityFactory"/>
        public BlockInfo SetTileEntity(string name)
        {
            _tileEntityName = name;
            return this;
        }
    }
}
