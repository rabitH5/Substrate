using System;
using System.Collections.Generic;
using Klokkit;
using Substrate.Core;
using Substrate.Nbt;

namespace Substrate
{
    /// <summary>
    /// Represents an item (or item stack) within an item slot.
    /// </summary>
    public class ItemNbt : INbtObject<ItemNbt>, ICopyable<ItemNbt>
    {
        private static readonly SchemaNodeCompound _schema = new SchemaNodeCompound("")
        {
            new SchemaNodeScaler("type", TagType.TAG_BYTE, SchemaOptions.CREATE_ON_MISSING),
            new SchemaNodeScaler("id", TagType.TAG_SHORT),
            new SchemaNodeScaler("Damage", TagType.TAG_SHORT),
            new SchemaNodeScaler("Count", TagType.TAG_BYTE),
        };

        private TagNodeCompound _source;
        private PropType _itemType;
        private short _id;
        private byte _count;
        private short _damage;

        /// <summary>
        /// Constructs an empty <see cref="ItemNbt"/> instance.
        /// </summary>
        public ItemNbt ()
        {
            _source = new TagNodeCompound();
        }

        /// <summary>
        /// Constructs an <see cref="ItemNbt"/> instance representing the given item id.
        /// </summary>
        /// <param name="id">An item id.</param>
        public ItemNbt (PropType itemtype, int id)
            : this()
        {
            _itemType = itemtype;
            _id = (short)id;
        }

        #region Properties

        public PropType ItemType
        {
            get { return _itemType; }
            set { _itemType = value; }
        }

        /// <summary>
        /// Gets or sets the current type (id) of the item.
        /// </summary>
        public int ID
        {
            get { return _id; }
            set { _id = (short)value; }
        }

        /// <summary>
        /// Gets or sets the damage value of the item.
        /// </summary>
        /// <remarks>The damage value may represent a generic data value for some items.</remarks>
        public int Damage
        {
            get { return _damage; }
            set { _damage = (short)value; }
        }

        /// <summary>
        /// Gets or sets the number of this item stacked together in an item slot.
        /// </summary>
        public int Count
        {
            get { return _count; }
            set { _count = (byte)value; }
        }

        /// <summary>
        /// Gets the source <see cref="TagNodeCompound"/> used to create this <see cref="ItemNbt"/> if it exists.
        /// </summary>
        public TagNodeCompound Source
        {
            get { return _source; }
        }

        /// <summary>
        /// Gets a <see cref="SchemaNode"/> representing the schema of an item.
        /// </summary>
        public static SchemaNodeCompound Schema
        {
            get { return _schema; }
        }

        #endregion

        #region ICopyable<Item> Members

        /// <inheritdoc/>
        public ItemNbt Copy ()
        {
            ItemNbt item = new ItemNbt();
            item.ItemType = _itemType;
            item._id = _id;
            item._count = _count;
            item._damage = _damage;

            if (_source != null) {
                item._source = _source.Copy() as TagNodeCompound;
            }

            return item;
        }

        #endregion

        #region INBTObject<Item> Members

        /// <inheritdoc/>
        public ItemNbt LoadTree (TagNode tree)
        {
            TagNodeCompound ctree = tree as TagNodeCompound;
            if (ctree == null) {
                return null;
            }

           byte type =   ctree["type"].ToTagByte();
            _itemType = (PropType) type;
            _id = ctree["id"].ToTagShort();
            _count = ctree["Count"].ToTagByte();
            _damage = ctree["Damage"].ToTagShort();

            _source = ctree.Copy() as TagNodeCompound;

            return this;
        }

        /// <inheritdoc/>
        public ItemNbt LoadTreeSafe (TagNode tree)
        {
            if (!ValidateTree(tree)) {
                return null;
            }

            return LoadTree(tree);
        }

        /// <inheritdoc/>
        public TagNode BuildTree ()
        {
            TagNodeCompound tree = new TagNodeCompound();
            tree["type"] = new TagNodeByte((byte) _itemType);
            tree["id"] = new TagNodeShort(_id);
            tree["Count"] = new TagNodeByte(_count);
            tree["Damage"] = new TagNodeShort(_damage);

            if (_source != null) {
                tree.MergeFrom(_source);
            }

            return tree;
        }

        /// <inheritdoc/>
        public bool ValidateTree (TagNode tree)
        {
            return new NbtVerifier(tree, _schema).Verify();
        }

        #endregion

        //> @rabitH5
        // ---------------------------------------------------------

        public override string ToString()
        {
            return "Item ( Type: "+(PropType)_itemType + " Id:" + _id + " Damage:" + _damage + " Count:" + _count + ")";
        }

        protected bool Equals(ItemNbt other)
        {
            return _itemType.Equals(other._itemType) && _id == other._id && _count == other._count && _damage == other._damage;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemNbt) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = _id.GetHashCode();
                hashCode = (hashCode*397) ^ _count.GetHashCode();
                hashCode = (hashCode*397) ^ _damage.GetHashCode();
                hashCode = (hashCode*397) ^ _itemType.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ItemNbt left, ItemNbt right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ItemNbt left, ItemNbt right)
        {
            return !Equals(left, right);
        }

        // ---------------------------------------------------------
        //< @rabitH5
    }
}
