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

        private static readonly BlockInfo[] _blockTable;
        private static readonly int[] _opacityTable;
        private static readonly int[] _luminanceTable;

        private class CacheTableArray<T> : ICacheTable<T>
        {
            private T[] _cache;

            public T this[int index]
            {
                get { return _cache[index]; }
            }

            public CacheTableArray (T[] cache)
            {
                _cache = cache;
            }
        }

        private class DataLimits
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

            public DataLimits (int low, int high, int bitmask)
            {
                _low = low;
                _high = high;
                _bitmask = bitmask;
            }

            public bool Test (int data)
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
        private bool _blocksFluid = true;
        private bool _registered = false;

        private BlockState _state = BlockState.SOLID;

        private DataLimits _dataLimits;

        private static readonly CacheTableArray<BlockInfo> _blockTableCache;
        private static readonly CacheTableArray<int> _opacityTableCache;
        private static readonly CacheTableArray<int> _luminanceTableCache;

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
        }

        /// <summary>
        /// Get's the name of the block type.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the block's opacity value.  An opacity of 0 is fully transparent to light.
        /// </summary>
        public int Opacity 
        {
            get { return _opacity; }
        }
        
        /// <summary>
        /// Gets the block's luminance value.
        /// </summary>
        /// <remarks>Blocks with luminance act as light sources and transmit light to other blocks.</remarks>
        public int Luminance 
        {
            get { return _luminance; }
        }

        /// <summary>
        /// Checks whether the block transmits light to neighboring blocks.
        /// </summary>
        /// <remarks>A block may stop the transmission of light, but still be illuminated.</remarks>
        public bool TransmitsLight
        {
            get { return _transmitLight; }
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
        public bool BlocksFluid
        {
            get { return _blocksFluid; }
        }

        /// <summary>
        /// Gets the block's physical state type.
        /// </summary>
        public BlockState State
        {
            get { return _state; }
        }

        /// <summary>
        /// Checks whether this block type has been registered as a known type.
        /// </summary>
        public bool Registered
        {
            get { return _registered; }
        }

        public int Tick
        {
            get { return _tick; }
        }

        internal BlockInfo (int id)
        {
            _id = id;
            _name = "Unknown Block";
            _blockTable[_id] = this;
        }

        /// <summary>
        /// Constructs a new <see cref="BlockInfo"/> record for a given block id and name.
        /// </summary>
        /// <param name="id">The id of the block.</param>
        /// <param name="name">The name of the block.</param>
        /// <remarks>All user-constructed <see cref="BlockInfo"/> objects are registered automatically.</remarks>
        public BlockInfo (int id, string name)
        {
            _id = id;
            _name = name;
            _blockTable[_id] = this;
            _registered = true;
        }

        /// <summary>
        /// Sets a new opacity value for this block type.
        /// </summary>
        /// <param name="opacity">A new opacity value.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        /// <seealso cref="AlphaBlockCollection.AutoLight"/>
        public BlockInfo SetOpacity (int opacity)
        {
            _opacity = MIN_OPACITY + opacity;
            _opacityTable[_id] = _opacity;

            if (opacity == MAX_OPACITY) {
                _transmitLight = false;
            }
            else {
                _transmitLight = true;
            }

            return this;
        }

        /// <summary>
        /// Sets a new luminance value for this block type.
        /// </summary>
        /// <param name="luminance">A new luminance value.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        /// <seealso cref="AlphaBlockCollection.AutoLight"/>
        public BlockInfo SetLuminance (int luminance)
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
        public BlockInfo SetLightTransmission (bool transmit)
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
        public BlockInfo SetDataLimits (int low, int high, int bitmask)
        {
            _dataLimits = new DataLimits(low, high, bitmask);
            return this;
        }

        /// <summary>
        /// Sets the physical state of the block type.
        /// </summary>
        /// <param name="state">A physical state.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        public BlockInfo SetState (BlockState state)
        {
            _state = state;

            if (_state == BlockState.SOLID) {
                _blocksFluid = true;
            }
            else {
                _blocksFluid = false;
            }

            return this;
        }

        /// <summary>
        /// Sets whether or not this block type blocks fluids.
        /// </summary>
        /// <param name="blocks">True if this block type blocks fluids, false otherwise.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        /// <seealso cref="AlphaBlockCollection.AutoFluid"/>
        public BlockInfo SetBlocksFluid (bool blocks)
        {
            _blocksFluid = blocks;
            return this;
        }

        /// <summary>
        /// Sets the default tick rate/delay used for updating this block.
        /// </summary>
        /// <remarks>Set <paramref name="tick"/> to <c>0</c> to indicate that this block is not processed by tick updates.</remarks>
        /// <param name="tick">The tick rate in frames between scheduled updates on this block.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        /// <seealso cref="AlphaBlockCollection.AutoTileTick"/>
        public BlockInfo SetTick (int tick)
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
        public bool TestData (int data)
        {
            if (_dataLimits == null) {
                return true;
            }
            return _dataLimits.Test(data);
        }

        public static BlockInfo Air;
        public static BlockInfo Stone;
        public static BlockInfo Grass;
        public static BlockInfo Dirt;
        public static BlockInfo Cobblestone;
        public static BlockInfo WoodPlank;
        public static BlockInfo Sapling;
        public static BlockInfo Bedrock;
        public static BlockInfo Water;
        public static BlockInfo StationaryWater;
        public static BlockInfo Lava;
        public static BlockInfo StationaryLava;
        public static BlockInfo Sand;
        public static BlockInfo Gravel;
        public static BlockInfo GoldOre;
        public static BlockInfo IronOre;
        public static BlockInfo CoalOre;
        public static BlockInfo Wood;
        public static BlockInfo Leaves;
        public static BlockInfo Sponge;
        public static BlockInfo Glass;
        public static BlockInfo LapisOre;
        public static BlockInfo LapisBlock;
        public static BlockInfoEx Dispenser;
        public static BlockInfo Sandstone;
        public static BlockInfoEx NoteBlock;
        public static BlockInfo Bed;
        public static BlockInfo PoweredRail;
        public static BlockInfo DetectorRail;
        public static BlockInfo StickyPiston;
        public static BlockInfo Cobweb;
        public static BlockInfo TallGrass;
        public static BlockInfo DeadShrub;
        public static BlockInfo Piston;
        public static BlockInfo PistonHead;
        public static BlockInfo Wool;
        public static BlockInfoEx PistonMoving;
        public static BlockInfo YellowFlower;
        public static BlockInfo RedRose;
        public static BlockInfo BrownMushroom;
        public static BlockInfo RedMushroom;
        public static BlockInfo GoldBlock;
        public static BlockInfo IronBlock;
        public static BlockInfo DoubleStoneSlab;
        public static BlockInfo StoneSlab;
        public static BlockInfo BrickBlock;
        public static BlockInfo TNT;
        public static BlockInfo Bookshelf;
        public static BlockInfo MossStone;
        public static BlockInfo Obsidian;
        public static BlockInfo Torch;
        public static BlockInfo Fire;
        public static BlockInfoEx MonsterSpawner;
        public static BlockInfo WoodStairs;
        public static BlockInfoEx Chest;
        public static BlockInfo RedstoneWire;
        public static BlockInfo DiamondOre;
        public static BlockInfo DiamondBlock;
        public static BlockInfo CraftTable;
        public static BlockInfo Crops;
        public static BlockInfo Farmland;
        public static BlockInfoEx Furnace;
        public static BlockInfoEx BurningFurnace;
        public static BlockInfoEx SignPost;
        public static BlockInfo WoodDoor;
        public static BlockInfo Ladder;
        public static BlockInfo Rails;
        public static BlockInfo CobbleStairs;
        public static BlockInfoEx WallSign;
        public static BlockInfo Lever;
        public static BlockInfo StonePlate;
        public static BlockInfo IronDoor;
        public static BlockInfo WoodPlate;
        public static BlockInfo RedstoneOre;
        public static BlockInfo GlowRedstoneOre;
        public static BlockInfo RedstoneTorch;
        public static BlockInfo RedstoneTorchOn;
        public static BlockInfo StoneButton;
        public static BlockInfo Snow;
        public static BlockInfo Ice;
        public static BlockInfo SnowBlock;
        public static BlockInfo Cactus;
        public static BlockInfo ClayBlock;
        public static BlockInfo SugarCane;
        public static BlockInfo Jukebox;
        public static BlockInfo Fence;
        public static BlockInfo Pumpkin;
        public static BlockInfo Netherrack;
        public static BlockInfo SoulSand;
        public static BlockInfo Glowstone;
        public static BlockInfo Portal;
        public static BlockInfo JackOLantern;
        public static BlockInfo CakeBlock;
        public static BlockInfo RedstoneRepeater;
        public static BlockInfo RedstoneRepeaterOn;
        public static BlockInfoEx LockedChest;
        public static BlockInfo Trapdoor;
        public static BlockInfo SilverfishStone;
        public static BlockInfo StoneBrick;
        public static BlockInfo HugeRedMushroom;
        public static BlockInfo HugeBrownMushroom;
        public static BlockInfo IronBars;
        public static BlockInfo GlassPane;
        public static BlockInfo Melon;
        public static BlockInfo PumpkinStem;
        public static BlockInfo MelonStem;
        public static BlockInfo Vines;
        public static BlockInfo FenceGate;
        public static BlockInfo BrickStairs;
        public static BlockInfo StoneBrickStairs;
        public static BlockInfo Mycelium;
        public static BlockInfo LillyPad;
        public static BlockInfo NetherBrick;
        public static BlockInfo NetherBrickFence;
        public static BlockInfo NetherBrickStairs;
        public static BlockInfo NetherWart;
        public static BlockInfoEx EnchantmentTable;
        public static BlockInfoEx BrewingStand;
        public static BlockInfo Cauldron;
        public static BlockInfoEx EndPortal;
        public static BlockInfo EndPortalFrame;
        public static BlockInfo EndStone;
        public static BlockInfo DragonEgg;
        public static BlockInfo RedstoneLampOff;
        public static BlockInfo RedstoneLampOn;
        public static BlockInfo DoubleWoodSlab;
        public static BlockInfo WoodSlab;
        public static BlockInfo CocoaPlant;
        public static BlockInfo SandstoneStairs;
        public static BlockInfo EmeraldOre;
        public static BlockInfoEx EnderChest;
        public static BlockInfo TripwireHook;
        public static BlockInfo Tripwire;
        public static BlockInfo EmeraldBlock;
        public static BlockInfo SpruceWoodStairs;
        public static BlockInfo BirchWoodStairs;
        public static BlockInfo JungleWoodStairs;
        public static BlockInfoEx CommandBlock;
        public static BlockInfoEx BeaconBlock;
        public static BlockInfo CobblestoneWall;
        public static BlockInfo FlowerPot;
        public static BlockInfo Carrots;
        public static BlockInfo Potatoes;
        public static BlockInfo WoodButton;
        public static BlockInfo Heads;
        public static BlockInfo Anvil;
        public static BlockInfoEx TrappedChest;
        public static BlockInfo WeightedPressurePlateLight;
        public static BlockInfo WeightedPressurePlateHeavy;
        public static BlockInfo RedstoneComparatorInactive;
        public static BlockInfo RedstoneComparatorActive;
        public static BlockInfo DaylightSensor;
        public static BlockInfo RedstoneBlock;
        public static BlockInfo NetherQuartzOre;
        public static BlockInfoEx Hopper;
        public static BlockInfo QuartzBlock;
        public static BlockInfo QuartzStairs;
        public static BlockInfo ActivatorRail;
        public static BlockInfoEx Dropper;
        public static BlockInfo StainedClay;
        public static BlockInfo HayBlock;
        public static BlockInfo Carpet;
        public static BlockInfo HardenedClay;
        public static BlockInfo CoalBlock;

        static BlockInfo ()
        {
            _blockTable = new BlockInfo[MAX_BLOCKS];
            _opacityTable = new int[MAX_BLOCKS];
            _luminanceTable = new int[MAX_BLOCKS];

            _blockTableCache = new CacheTableArray<BlockInfo>(_blockTable);
            _opacityTableCache = new CacheTableArray<int>(_opacityTable);
            _luminanceTableCache = new CacheTableArray<int>(_luminanceTable);

            Air = new BlockInfo(0, "Air").SetOpacity(0).SetState(BlockState.NONSOLID);
            Stone = new BlockInfo(1, "Stone");
            Grass = new BlockInfo(2, "Grass").SetTick(10);
            Dirt = new BlockInfo(3, "Dirt");
            Cobblestone = new BlockInfo(4, "Cobblestone");
            WoodPlank = new BlockInfo(5, "Wooden Plank");
            Sapling = new BlockInfo(6, "Sapling").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            Bedrock = new BlockInfo(7, "Bedrock");
            Water = new BlockInfo(8, "Water").SetOpacity(3).SetState(BlockState.FLUID).SetTick(5);
            StationaryWater = new BlockInfo(9, "Stationary Water").SetOpacity(3).SetState(BlockState.FLUID);
            Lava = new BlockInfo(10, "Lava").SetOpacity(0).SetLuminance(MAX_LUMINANCE).SetState(BlockState.FLUID).SetTick(30);
            StationaryLava = new BlockInfo(11, "Stationary Lava").SetOpacity(0).SetLuminance(MAX_LUMINANCE).SetState(BlockState.FLUID).SetTick(10);
            Sand = new BlockInfo(12, "Sand").SetTick(3);
            Gravel = new BlockInfo(13, "Gravel").SetTick(3);
            GoldOre = new BlockInfo(14, "Gold Ore");
            IronOre = new BlockInfo(15, "Iron Ore");
            CoalOre = new BlockInfo(16, "Coal Ore");
            Wood = new BlockInfo(17, "Wood");
            Leaves = new BlockInfo(18, "Leaves").SetOpacity(1).SetTick(10);
            Sponge = new BlockInfo(19, "Sponge");
            Glass = new BlockInfo(20, "Glass").SetOpacity(0);
            LapisOre = new BlockInfo(21, "Lapis Lazuli Ore");
            LapisBlock = new BlockInfo(22, "Lapis Lazuli Block");
            Dispenser = (BlockInfoEx)new BlockInfoEx(23, "Dispenser").SetTick(4);
            Sandstone = new BlockInfo(24, "Sandstone");
            NoteBlock = new BlockInfoEx(25, "Note Block");
            Bed = new BlockInfo(26, "Bed").SetOpacity(0);
            PoweredRail = new BlockInfo(27, "Powered Rail").SetOpacity(0).SetState(BlockState.NONSOLID);
            DetectorRail = new BlockInfo(28, "Detector Rail").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(20);
            StickyPiston = new BlockInfo(29, "Sticky Piston").SetOpacity(0);
            Cobweb = new BlockInfo(30, "Cobweb").SetOpacity(0).SetState(BlockState.NONSOLID);
            TallGrass = new BlockInfo(31, "Tall Grass").SetOpacity(0).SetState(BlockState.NONSOLID);
            DeadShrub = new BlockInfo(32, "Dead Shrub").SetOpacity(0).SetState(BlockState.NONSOLID);
            Piston = new BlockInfo(33, "Piston").SetOpacity(0);
            PistonHead = new BlockInfo(34, "Piston Head").SetOpacity(0);
            Wool = new BlockInfo(35, "Wool");
            PistonMoving = (BlockInfoEx)new BlockInfoEx(36, "Piston Moving").SetOpacity(0);
            YellowFlower = new BlockInfo(37, "Yellow Flower").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            RedRose = new BlockInfo(38, "Red Rose").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            BrownMushroom = new BlockInfo(39, "Brown Mushroom").SetOpacity(0).SetLuminance(1).SetState(BlockState.NONSOLID).SetTick(10);
            RedMushroom = new BlockInfo(40, "Red Mushroom").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            GoldBlock = new BlockInfo(41, "Gold Block");
            IronBlock = new BlockInfo(42, "Iron Block");
            DoubleStoneSlab = new BlockInfo(43, "Double Slab");
            StoneSlab = new BlockInfo(44, "Slab").SetOpacity(0);
            BrickBlock = new BlockInfo(45, "Brick Block");
            TNT = new BlockInfo(46, "TNT");
            Bookshelf = new BlockInfo(47, "Bookshelf");
            MossStone = new BlockInfo(48, "Moss Stone");
            Obsidian = new BlockInfo(49, "Obsidian");
            Torch = new BlockInfo(50, "Torch").SetOpacity(0).SetLuminance(MAX_LUMINANCE - 1).SetState(BlockState.NONSOLID).SetTick(10);
            Fire = new BlockInfo(51, "Fire").SetOpacity(0).SetLuminance(MAX_LUMINANCE).SetState(BlockState.NONSOLID).SetTick(40);
            MonsterSpawner = (BlockInfoEx)new BlockInfoEx(52, "Monster Spawner").SetOpacity(0);
            WoodStairs = new BlockInfo(53, "Wooden Stairs").SetOpacity(0);
            Chest = (BlockInfoEx)new BlockInfoEx(54, "Chest").SetOpacity(0);
            RedstoneWire = new BlockInfo(55, "Redstone Wire").SetOpacity(0).SetState(BlockState.NONSOLID);
            DiamondOre = new BlockInfo(56, "Diamond Ore");
            DiamondBlock = new BlockInfo(57, "Diamond Block");
            CraftTable = new BlockInfo(58, "Crafting Table");
            Crops = new BlockInfo(59, "Crops").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            Farmland = new BlockInfo(60, "Farmland").SetOpacity(0).SetTick(10);
            Furnace = new BlockInfoEx(61, "Furnace");
            BurningFurnace = (BlockInfoEx)new BlockInfoEx(62, "Burning Furnace").SetLuminance(MAX_LUMINANCE - 1);
            SignPost = (BlockInfoEx)new BlockInfoEx(63, "Sign Post").SetOpacity(0).SetState(BlockState.NONSOLID);
            WoodDoor = new BlockInfo(64, "Wooden Door").SetOpacity(0);
            Ladder = new BlockInfo(65, "Ladder").SetOpacity(0);
            Rails = new BlockInfo(66, "Rails").SetOpacity(0).SetState(BlockState.NONSOLID);
            CobbleStairs = new BlockInfo(67, "Cobblestone Stairs").SetOpacity(0);
            WallSign = (BlockInfoEx)new BlockInfoEx(68, "Wall Sign").SetOpacity(0).SetState(BlockState.NONSOLID);
            Lever = new BlockInfo(69, "Lever").SetOpacity(0).SetState(BlockState.NONSOLID);
            StonePlate = new BlockInfo(70, "Stone Pressure Plate").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(20);
            IronDoor = new BlockInfo(71, "Iron Door").SetOpacity(0);
            WoodPlate = new BlockInfo(72, "Wooden Pressure Plate").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(20);
            RedstoneOre = new BlockInfo(73, "Redstone Ore").SetTick(30);
            GlowRedstoneOre = new BlockInfo(74, "Glowing Redstone Ore").SetLuminance(9).SetTick(30);
            RedstoneTorch = new BlockInfo(75, "Redstone Torch (Off)").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(2);
            RedstoneTorchOn = new BlockInfo(76, "Redstone Torch (On)").SetOpacity(0).SetLuminance(7).SetState(BlockState.NONSOLID).SetTick(2);
            StoneButton = new BlockInfo(77, "Stone Button").SetOpacity(0).SetState(BlockState.NONSOLID);
            Snow = new BlockInfo(78, "Snow").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            Ice = new BlockInfo(79, "Ice").SetOpacity(3).SetTick(10);
            SnowBlock = new BlockInfo(80, "Snow Block").SetTick(10);
            Cactus = new BlockInfo(81, "Cactus").SetOpacity(0).SetTick(10);
            ClayBlock = new BlockInfo(82, "Clay Block");
            SugarCane = new BlockInfo(83, "Sugar Cane").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            Jukebox = new BlockInfo(84, "Jukebox");
            Fence = new BlockInfo(85, "Fence").SetOpacity(0);
            Pumpkin = new BlockInfo(86, "Pumpkin");
            Netherrack = new BlockInfo(87, "Netherrack");
            SoulSand = new BlockInfo(88, "Soul Sand");
            Glowstone = new BlockInfo(89, "Glowstone Block").SetLuminance(MAX_LUMINANCE);
            Portal = new BlockInfo(90, "Portal").SetOpacity(0).SetLuminance(11).SetState(BlockState.NONSOLID);
            JackOLantern = new BlockInfo(91, "Jack-O-Lantern").SetLuminance(MAX_LUMINANCE);
            CakeBlock = new BlockInfo(92, "Cake Block").SetOpacity(0);
            RedstoneRepeater = new BlockInfo(93, "Redstone Repeater (Off)").SetOpacity(0).SetTick(10);
            RedstoneRepeaterOn = new BlockInfo(94, "Redstone Repeater (On)").SetOpacity(0).SetLuminance(7).SetTick(10);
            LockedChest = (BlockInfoEx)new BlockInfoEx(95, "Locked Chest").SetLuminance(MAX_LUMINANCE).SetTick(10);
            Trapdoor = new BlockInfo(96, "Trapdoor").SetOpacity(0);
            SilverfishStone = new BlockInfo(97, "Stone with Silverfish");
            StoneBrick = new BlockInfo(98, "Stone Brick");
            HugeRedMushroom = new BlockInfo(99, "Huge Red Mushroom");
            HugeBrownMushroom = new BlockInfo(100, "Huge Brown Mushroom");
            IronBars = new BlockInfo(101, "Iron Bars").SetOpacity(0);
            GlassPane = new BlockInfo(102, "Glass Pane").SetOpacity(0);
            Melon = new BlockInfo(103, "Melon");
            PumpkinStem = new BlockInfo(104, "Pumpkin Stem").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            MelonStem = new BlockInfo(105, "Melon Stem").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            Vines = new BlockInfo(106, "Vines").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            FenceGate = new BlockInfo(107, "Fence Gate").SetOpacity(0);
            BrickStairs = new BlockInfo(108, "Brick Stairs").SetOpacity(0);
            StoneBrickStairs = new BlockInfo(109, "Stone Brick Stairs").SetOpacity(0);
            Mycelium = new BlockInfo(110, "Mycelium").SetTick(10);
            LillyPad = new BlockInfo(111, "Lilly Pad").SetOpacity(0).SetState(BlockState.NONSOLID);
            NetherBrick = new BlockInfo(112, "Nether Brick");
            NetherBrickFence = new BlockInfo(113, "Nether Brick Fence").SetOpacity(0);
            NetherBrickStairs = new BlockInfo(114, "Nether Brick Stairs").SetOpacity(0);
            NetherWart = new BlockInfo(115, "Nether Wart").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            EnchantmentTable = (BlockInfoEx)new BlockInfoEx(116, "Enchantment Table").SetOpacity(0);
            BrewingStand = (BlockInfoEx)new BlockInfoEx(117, "Brewing Stand").SetOpacity(0);
            Cauldron = new BlockInfo(118, "Cauldron").SetOpacity(0);
            EndPortal = (BlockInfoEx)new BlockInfoEx(119, "End Portal").SetOpacity(0).SetLuminance(MAX_LUMINANCE).SetState(BlockState.NONSOLID);
            EndPortalFrame = new BlockInfo(120, "End Portal Frame").SetLuminance(MAX_LUMINANCE);
            EndStone = new BlockInfo(121, "End Stone");
            DragonEgg = new BlockInfo(122, "Dragon Egg").SetOpacity(0).SetLuminance(1).SetTick(3);
            RedstoneLampOff = new BlockInfo(123, "Redstone Lamp (Off)").SetTick(2);
            RedstoneLampOn = new BlockInfo(124, "Redstone Lamp (On)").SetLuminance(15).SetTick(2);
            DoubleWoodSlab = new BlockInfo(125, "Double Wood Slab");
            WoodSlab = new BlockInfo(126, "Wood Slab");
            CocoaPlant = new BlockInfo(127, "Cocoa Plant").SetLuminance(2).SetOpacity(0);
            SandstoneStairs = new BlockInfo(128, "Sandstone Stairs").SetOpacity(0);
            EmeraldOre = new BlockInfo(129, "Emerald Ore");
            EnderChest = (BlockInfoEx)new BlockInfoEx(130, "Ender Chest").SetLuminance(7).SetOpacity(0);
            TripwireHook = new BlockInfo(131, "Tripwire Hook").SetOpacity(0).SetState(BlockState.NONSOLID);
            Tripwire = new BlockInfo(132, "Tripwire").SetOpacity(0).SetState(BlockState.NONSOLID);
            EmeraldBlock = new BlockInfo(133, "Emerald Block");
            SpruceWoodStairs = new BlockInfo(134, "Sprice Wood Stairs").SetOpacity(0);
            BirchWoodStairs = new BlockInfo(135, "Birch Wood Stairs").SetOpacity(0);
            JungleWoodStairs = new BlockInfo(136, "Jungle Wood Stairs").SetOpacity(0);
            CommandBlock = (BlockInfoEx)new BlockInfoEx(137, "Command Block");
            BeaconBlock = (BlockInfoEx)new BlockInfoEx(138, "Beacon Block").SetOpacity(0).SetLuminance(MAX_LUMINANCE);
            CobblestoneWall = new BlockInfo(139, "Cobblestone Wall").SetOpacity(0);
            FlowerPot = new BlockInfo(140, "Flower Pot").SetOpacity(0);
            Carrots = new BlockInfo(141, "Carrots").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            Potatoes = new BlockInfo(142, "Potatoes").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            WoodButton = new BlockInfo(143, "Wooden Button").SetOpacity(0).SetState(BlockState.NONSOLID);
            Heads = new BlockInfo(144, "Heads").SetOpacity(0);
            Anvil = new BlockInfo(145, "Anvil").SetOpacity(0);
            TrappedChest = (BlockInfoEx)new BlockInfoEx(146, "Trapped Chest").SetOpacity(0).SetTick(10);
            WeightedPressurePlateLight = new BlockInfo(147, "Weighted Pressure Plate (Light)").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(20);
            WeightedPressurePlateHeavy = new BlockInfo(148, "Weighted Pressure Plate (Heavy)").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(20);
            RedstoneComparatorInactive = new BlockInfo(149, "Redstone Comparator (Inactive)").SetOpacity(0).SetTick(10);
            RedstoneComparatorActive = new BlockInfo(150, "Redstone Comparator (Active)").SetOpacity(0).SetLuminance(9).SetTick(10);
            DaylightSensor = new BlockInfo(151, "Daylight Sensor").SetOpacity(0).SetTick(10);
            RedstoneBlock = new BlockInfo(152, "Block of Redstone").SetTick(10);
            NetherQuartzOre = new BlockInfo(153, "Neither Quartz Ore");
            Hopper = (BlockInfoEx)new BlockInfoEx(154, "Hopper").SetOpacity(0).SetTick(10);
            QuartzBlock = new BlockInfo(155, "Block of Quartz");
            QuartzStairs = new BlockInfo(156, "Quartz Stairs").SetOpacity(0);
            ActivatorRail = new BlockInfo(157, "Activator Rail").SetOpacity(0).SetState(BlockState.NONSOLID).SetTick(10);
            Dropper = (BlockInfoEx)new BlockInfoEx(158, "Dropper").SetTick(10);
            StainedClay = new BlockInfo(159, "Stained Clay");
            HayBlock = new BlockInfo(170, "Hay Block");
            Carpet = new BlockInfo(171, "Carpet").SetOpacity(0);
            HardenedClay = new BlockInfo(172, "Hardened Clay");
            CoalBlock = new BlockInfo(173, "Block of Coal");

            for (int i = 0; i < MAX_BLOCKS; i++) {
                if (_blockTable[i] == null) {
                    _blockTable[i] = new BlockInfo(i);
                }
            }

            // Override default light transmission rules

            Lava.SetLightTransmission(false);
            StationaryLava.SetLightTransmission(false);
            StoneSlab.SetLightTransmission(false);
            WoodStairs.SetLightTransmission(false);
            Farmland.SetLightTransmission(false);
            CobbleStairs.SetLightTransmission(false);
            BrickStairs.SetLightTransmission(false);
            StoneBrickStairs.SetLightTransmission(false);
            NetherBrickStairs.SetLightTransmission(false);
            WoodSlab.SetLightTransmission(false);
            SandstoneStairs.SetLightTransmission(false);
            SpruceWoodStairs.SetLightTransmission(false);
            BirchWoodStairs.SetLightTransmission(false);
            JungleWoodStairs.SetLightTransmission(false);
            QuartzStairs.SetLightTransmission(false);
            Carpet.SetLightTransmission(false);

            // Override default fluid blocking rules

            SignPost.SetBlocksFluid(true);
            WallSign.SetBlocksFluid(true);
            Cactus.SetBlocksFluid(false);

            // Set Tile Entity Data

            Dispenser.SetTileEntity("Trap");
            NoteBlock.SetTileEntity("Music");
            PistonMoving.SetTileEntity("Piston");
            MonsterSpawner.SetTileEntity("MobSpawner");
            Chest.SetTileEntity("Chest");
            Furnace.SetTileEntity("Furnace");
            BurningFurnace.SetTileEntity("Furnace");
            SignPost.SetTileEntity("Sign");
            WallSign.SetTileEntity("Sign");
            EnchantmentTable.SetTileEntity("EnchantTable");
            BrewingStand.SetTileEntity("Cauldron");
            EndPortal.SetTileEntity("Airportal");
            EnderChest.SetTileEntity("EnderChest");
            CommandBlock.SetTileEntity("Control");
            BeaconBlock.SetTileEntity("Beacon");
            TrappedChest.SetTileEntity("Chest");
            Hopper.SetTileEntity("Hopper");
            Dropper.SetTileEntity("Dropper");

            // Set Data Limits

            Wood.SetDataLimits(0, 2, 0);
            Leaves.SetDataLimits(0, 2, 0);
            Jukebox.SetDataLimits(0, 2, 0);
            Sapling.SetDataLimits(0, 15, 0);
            Cactus.SetDataLimits(0, 15, 0);
            SugarCane.SetDataLimits(0, 15, 0);
            Water.SetDataLimits(0, 7, 0x8);
            Lava.SetDataLimits(0, 7, 0x8);
            TallGrass.SetDataLimits(0, 2, 0);
            Crops.SetDataLimits(0, 7, 0);
            PoweredRail.SetDataLimits(0, 5, 0x8);
            DetectorRail.SetDataLimits(0, 5, 0x8);
            StickyPiston.SetDataLimits(1, 5, 0x8);
            Piston.SetDataLimits(1, 5, 0x8);
            PistonHead.SetDataLimits(1, 5, 0x8);
            Wool.SetDataLimits(0, 15, 0);
            Torch.SetDataLimits(1, 5, 0);
            RedstoneTorch.SetDataLimits(0, 5, 0);
            RedstoneTorchOn.SetDataLimits(0, 5, 0);
            Rails.SetDataLimits(0, 9, 0);
            Ladder.SetDataLimits(2, 5, 0);
            WoodStairs.SetDataLimits(0, 3, 0x4);
            CobbleStairs.SetDataLimits(0, 3, 0x4);
            Lever.SetDataLimits(0, 6, 0x8);
            WoodDoor.SetDataLimits(0, 3, 0xC);
            IronDoor.SetDataLimits(0, 3, 0xC);
            StoneButton.SetDataLimits(1, 4, 0x8);
            Snow.SetDataLimits(0, 7, 0);
            SignPost.SetDataLimits(0, 15, 0);
            WallSign.SetDataLimits(2, 5, 0);
            Furnace.SetDataLimits(2, 5, 0);
            BurningFurnace.SetDataLimits(2, 5, 0);
            Dispenser.SetDataLimits(2, 5, 0);
            Pumpkin.SetDataLimits(0, 3, 0);
            JackOLantern.SetDataLimits(0, 3, 0);
            StonePlate.SetDataLimits(0, 0, 0x1);
            WoodPlate.SetDataLimits(0, 0, 0x1);
            StoneSlab.SetDataLimits(0, 5, 0);
            DoubleStoneSlab.SetDataLimits(0, 5, 0x8);
            Cactus.SetDataLimits(0, 5, 0);
            Bed.SetDataLimits(0, 3, 0x8);
            RedstoneRepeater.SetDataLimits(0, 0, 0xF);
            RedstoneRepeaterOn.SetDataLimits(0, 0, 0xF);
            Trapdoor.SetDataLimits(0, 3, 0x4);
            StoneBrick.SetDataLimits(0, 2, 0);
            HugeRedMushroom.SetDataLimits(0, 10, 0);
            HugeBrownMushroom.SetDataLimits(0, 10, 0);
            Vines.SetDataLimits(0, 0, 0xF);
            FenceGate.SetDataLimits(0, 3, 0x4);
            SilverfishStone.SetDataLimits(0, 2, 0);
            BrewingStand.SetDataLimits(0, 0, 0x7);
            Cauldron.SetDataLimits(0, 3, 0);
            EndPortalFrame.SetDataLimits(0, 0, 0x7);
            WoodSlab.SetDataLimits(0, 5, 0);
            DoubleWoodSlab.SetDataLimits(0, 5, 0x8);
            TripwireHook.SetDataLimits(0, 3, 0xC);
            Tripwire.SetDataLimits(0, 0, 0x5);
            Anvil.SetDataLimits(0, 0, 0xD);
            QuartzBlock.SetDataLimits(0, 4, 0);
            QuartzStairs.SetDataLimits(0, 3, 0x4);
            Carpet.SetDataLimits(0, 15, 0);
            Dropper.SetDataLimits(0, 5, 0);
            Hopper.SetDataLimits(0, 5, 0);

            //> @rabitH5
            // ------------------------------
            // Custom setup
            // ------------------------------

            _isOpaqueTable = new bool[MAX_BLOCKS];

            FinishBlockSetup();
        }

        // -------------------------------------------------------------------------------------------------
        // Bukkit implementation
        // -------------------------------------------------------------------------------------------------

        private static Dictionary<string, Material> BY_NAME = new Dictionary<string, Material>();
        private int _maxStack;
        private short _durability;
        private bool _isEdible;
        private bool _isFlammable;
        private bool _isBurnable;
        private bool _hasGravity;
        private bool _isWalkable;
        private bool _isPassable;
        private bool _isTeleportable;
        private bool _breakNaturally;

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

        // -------------------------------------------------------------------------------------------------
        // Setup
        // -------------------------------------------------------------------------------------------------

        public static string nullBlockName = "Unknown Block";
        private static readonly bool[] _isOpaqueTable;

        /// <summary>
        /// Must be called whenever all the blocks have been defined.
        /// </summary>
        private static void FinishBlockSetup()
        {

            FillEmptyBlockData();
            UpdateOpacityLookup();
            UpdateNameLookup();
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

        // -------------------------------------------------------------------------------------------------
        //< @rabitH5
    }

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

        internal BlockInfoEx (int id) : base(id) { }

        /// <summary>
        /// Constructs a new <see cref="BlockInfoEx"/> with a given block id and name.
        /// </summary>
        /// <param name="id">The id of the block type.</param>
        /// <param name="name">The name of the block type.</param>
        public BlockInfoEx (int id, string name) : base(id, name) { }

        /// <summary>
        /// Sets the name of the <see cref="TileEntity"/> type associated with this block type.
        /// </summary>
        /// <param name="name">The name of a registered <see cref="TileEntity"/> type.</param>
        /// <returns>The object instance used to invoke this method.</returns>
        /// <seealso cref="TileEntityFactory"/>
        public BlockInfo SetTileEntity (string name) {
            _tileEntityName = name;
            return this;
        }
    }
}
