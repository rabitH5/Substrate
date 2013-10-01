using System;
using System.Collections.Generic;
using System.Text;
using Substrate.Core;
using Substrate.Nbt;
using UnityEngine;
using JumpGate;

namespace Substrate
{
    /// <summary>
    /// The base Entity type for Minecraft Entities, providing access to data common to all Minecraft Entities.
    /// </summary>
    public class Entity : INbtObject<Entity>, ICopyable<Entity>
    {
        private static readonly SchemaNodeCompound _schema = new SchemaNodeCompound("")
        {
            new SchemaNodeList("Pos", TagType.TAG_FLOAT, 3),
            new SchemaNodeList("Motion", TagType.TAG_FLOAT, 3),
            new SchemaNodeList("Rotation", TagType.TAG_FLOAT, 2),
            new SchemaNodeScaler("FallDistance", TagType.TAG_FLOAT),
            new SchemaNodeScaler("Fire", TagType.TAG_SHORT),
            new SchemaNodeScaler("Air", TagType.TAG_SHORT),
            new SchemaNodeScaler("OnGround", TagType.TAG_BYTE),
        };

        private TagNodeCompound _source;

        private Vector3 _pos;
        private Vector3 _motion;
        private Orientation _rotation;

        private float _fallDistance;
        private short _fire;
        private short _air;
        private byte _onGround;

        /// <summary>
        /// Gets or sets the global position of the entity in fractional block coordinates.
        /// </summary>
        public Vector3 Position
        {
            get { return _pos; }
            set { _pos = value; }
        }

        /// <summary>
        /// Gets or sets the velocity of the entity.
        /// </summary>
        public Vector3 Motion
        {
            get { return _motion; }
            set { _motion = value; }
        }

        /// <summary>
        /// Gets or sets the orientation of the entity.
        /// </summary>
        public Orientation Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        /// <summary>
        /// Gets or sets the distance that the entity has fallen, if it is falling.
        /// </summary>
        public double FallDistance
        {
            get { return _fallDistance; }
            set { _fallDistance = (float)value; }
        }

        /// <summary>
        /// Gets or sets the fire counter of the entity.
        /// </summary>
        public int Fire
        {
            get { return _fire; }
            set { _fire = (short)value; }
        }

        /// <summary>
        /// Gets or sets the remaining air availale to the entity.
        /// </summary>
        public int Air
        {
            get { return _air; }
            set { _air = (short)value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is currently touch the ground.
        /// </summary>
        public bool IsOnGround
        {
            get { return _onGround == 1; }
            set { _onGround = (byte)(value ? 1 : 0); }
        }

        /// <summary>
        /// Gets the source <see cref="TagNodeCompound"/> used to create this <see cref="Entity"/> if it exists.
        /// </summary>
        public TagNodeCompound Source
        {
            get { return _source; }
        }

        /// <summary>
        /// Constructs a new generic <see cref="Entity"/> with default values.
        /// </summary>
        public Entity ()
        {
            _pos = new Vector3();
            _motion = new Vector3();
            _rotation = new Orientation();

            _source = new TagNodeCompound();
        }

        /// <summary>
        /// Constructs a new generic <see cref="Entity"/> by copying fields from another <see cref="Entity"/> object.
        /// </summary>
        /// <param name="e">An <see cref="Entity"/> to copy fields from.</param>
        protected Entity (Entity e)
        {
            _pos = new Vector3();
            _pos.x = e._pos.x;
            _pos.y = e._pos.y;
            _pos.z = e._pos.z;

            _motion = new Vector3();
            _motion.x = e._motion.x;
            _motion.y = e._motion.y;
            _motion.z = e._motion.z;

            _rotation = new Orientation();
            _rotation.Pitch = e._rotation.Pitch;
            _rotation.Yaw = e._rotation.Yaw;

            _fallDistance = e._fallDistance;
            _fire = e._fire;
            _air = e._air;
            _onGround = e._onGround;

            if (e._source != null) {
                _source = e._source.Copy() as TagNodeCompound;
            }
        }

        /// <summary>
        /// Moves the <see cref="Entity"/> by given block offsets.
        /// </summary>
        /// <param name="diffX">The X-offset to move by, in blocks.</param>
        /// <param name="diffY">The Y-offset to move by, in blocks.</param>
        /// <param name="diffZ">The Z-offset to move by, in blocks.</param>
        public virtual void MoveBy (int diffX, int diffY, int diffZ)
        {
            _pos.x += diffX;
            _pos.y += diffY;
            _pos.z += diffZ;
        }


        #region INBTObject<Entity> Members

        /// <summary>
        /// Gets a <see cref="SchemaNode"/> representing the basic schema of an Entity.
        /// </summary>
        public static SchemaNodeCompound Schema
        {
            get { return _schema; }
        }

        /// <summary>
        /// Attempt to load an Entity subtree into the <see cref="Entity"/> without validation.
        /// </summary>
        /// <param name="tree">The root node of an Entity subtree.</param>
        /// <returns>The <see cref="Entity"/> returns itself on success, or null if the tree was unparsable.</returns>
        public Entity LoadTree (TagNode tree)
        {
            TagNodeCompound ctree = tree as TagNodeCompound;
            if (ctree == null) {
                return null;
            }

            TagNodeList pos = ctree["Pos"].ToTagList();
            _pos = new Vector3();
            _pos.x = pos[0].ToTagFloat();
            _pos.y = pos[1].ToTagFloat();
            _pos.z = pos[2].ToTagFloat();

            Logger.LogInfo("THIS IS POS >>>>> " + _pos.ToString());

            TagNodeList motion = ctree["Motion"].ToTagList();
            _motion = new Vector3();
            _motion.x = motion[0].ToTagFloat();
            _motion.y = motion[1].ToTagFloat();
            _motion.z = motion[2].ToTagFloat();

            TagNodeList rotation = ctree["Rotation"].ToTagList();
            _rotation = new Orientation();
            _rotation.Yaw = rotation[0].ToTagFloat();
            _rotation.Pitch = rotation[1].ToTagFloat();

            _fire = ctree["Fire"].ToTagShort();
            _air = ctree["Air"].ToTagShort();
            _onGround = ctree["OnGround"].ToTagByte();

            _source = ctree.Copy() as TagNodeCompound;

            return this;
        }

        /// <summary>
        /// Attempt to load an Entity subtree into the <see cref="Entity"/> with validation.
        /// </summary>
        /// <param name="tree">The root node of an Entity subtree.</param>
        /// <returns>The <see cref="Entity"/> returns itself on success, or null if the tree failed validation.</returns>
        public Entity LoadTreeSafe (TagNode tree)
        {
            if (!ValidateTree(tree)) {
                return null;
            }

            return LoadTree(tree);
        }

        /// <summary>
        /// Builds an Entity subtree from the current data.
        /// </summary>
        /// <returns>The root node of an Entity subtree representing the current data.</returns>
        public TagNode BuildTree ()
        {

            TagNodeCompound tree = new TagNodeCompound();

            TagNodeList pos = new TagNodeList(TagType.TAG_FLOAT);
            pos.Add(new TagNodeFloat(_pos.x));
            pos.Add(new TagNodeFloat(_pos.y));
            pos.Add(new TagNodeFloat(_pos.z));
            tree["Pos"] = pos;

            TagNodeList motion = new TagNodeList(TagType.TAG_FLOAT);
            motion.Add(new TagNodeFloat(_motion.x));
            motion.Add(new TagNodeFloat(_motion.y));
            motion.Add(new TagNodeFloat(_motion.z));
            tree["Motion"] = motion;

            TagNodeList rotation = new TagNodeList(TagType.TAG_FLOAT);
            rotation.Add(new TagNodeFloat((float)_rotation.Yaw));
            rotation.Add(new TagNodeFloat((float)_rotation.Pitch));
            tree["Rotation"] = rotation;

            tree["FallDistance"] = new TagNodeFloat(_fallDistance);
            tree["Fire"] = new TagNodeShort(_fire);
            tree["Air"] = new TagNodeShort(_air);
            tree["OnGround"] = new TagNodeByte(_onGround);

            if (_source != null) {
                tree.MergeFrom(_source);
            }

            return tree;
        }

        /// <summary>
        /// Validate an Entity subtree against a basic schema.
        /// </summary>
        /// <param name="tree">The root node of an Entity subtree.</param>
        /// <returns>Status indicating whether the tree was valid against the internal schema.</returns>
        public bool ValidateTree (TagNode tree)
        {
            return new NbtVerifier(tree, _schema).Verify();
        }

        #endregion


        #region ICopyable<Entity> Members

        /// <summary>
        /// Creates a deep-copy of the <see cref="Entity"/>.
        /// </summary>
        /// <returns>A deep-copy of the <see cref="Entity"/>.</returns>
        public Entity Copy ()
        {
            return new Entity(this);
        }

        #endregion
    }

    /// <summary>
    /// A base entity type for all entities except <see cref="Player"/> entities.
    /// </summary>
    /// <remarks>Generally, this class should be subtyped into new concrete Entity types, as this generic type is unable to
    /// capture any of the custom data fields.  It is however still possible to create instances of <see cref="Entity"/> objects, 
    /// which may allow for graceful handling of unknown Entity types.</remarks>
    public class TypedEntity : Entity, INbtObject<TypedEntity>, ICopyable<TypedEntity>
    {
        private static readonly SchemaNodeCompound _schema = Entity.Schema.MergeInto(new SchemaNodeCompound("")
        {
            new SchemaNodeScaler("id", TagType.TAG_STRING),
        });

        private string _id;

        /// <summary>
        /// Gets the id (type) of the entity.
        /// </summary>
        public string ID
        {
            get { return _id; }
        }

        /// <summary>
        /// Creates a new generic <see cref="TypedEntity"/> with the given id.
        /// </summary>
        /// <param name="id">The id (name) of the Entity.</param>
        public TypedEntity (string id)
            : base()
        {
            _id = id;
        }

        /// <summary>
        /// Constructs a new <see cref="TypedEntity"/> by copying an existing one.
        /// </summary>
        /// <param name="e">The <see cref="TypedEntity"/> to copy.</param>
        protected TypedEntity (TypedEntity e)
            : base(e)
        {
            _id = e._id;
        }


        #region INBTObject<Entity> Members

        /// <summary>
        /// Gets a <see cref="SchemaNode"/> representing the basic schema of an Entity.
        /// </summary>
        public static new SchemaNodeCompound Schema
        {
            get { return _schema; }
        }

        /// <summary>
        /// Attempt to load an Entity subtree into the <see cref="TypedEntity"/> without validation.
        /// </summary>
        /// <param name="tree">The root node of an Entity subtree.</param>
        /// <returns>The <see cref="TypedEntity"/> returns itself on success, or null if the tree was unparsable.</returns>
        public virtual new TypedEntity LoadTree (TagNode tree)
        {
            TagNodeCompound ctree = tree as TagNodeCompound;
            if (ctree == null || base.LoadTree(tree) == null) {
                return null;
            }

            _id = ctree["id"].ToTagString();

            return this;
        }

        /// <summary>
        /// Attempt to load an Entity subtree into the <see cref="TypedEntity"/> with validation.
        /// </summary>
        /// <param name="tree">The root node of an Entity subtree.</param>
        /// <returns>The <see cref="TypedEntity"/> returns itself on success, or null if the tree failed validation.</returns>
        public virtual new TypedEntity LoadTreeSafe (TagNode tree)
        {
            if (!ValidateTree(tree)) {
                return null;
            }

            return LoadTree(tree);
        }

        /// <summary>
        /// Builds an Entity subtree from the current data.
        /// </summary>
        /// <returns>The root node of an Entity subtree representing the current data.</returns>
        public virtual new TagNode BuildTree ()
        {
            TagNodeCompound tree = base.BuildTree() as TagNodeCompound;
            tree["id"] = new TagNodeString(_id);

            return tree;
        }

        /// <summary>
        /// Validate an Entity subtree against a basic schema.
        /// </summary>
        /// <param name="tree">The root node of an Entity subtree.</param>
        /// <returns>Status indicating whether the tree was valid against the internal schema.</returns>
        public virtual new bool ValidateTree (TagNode tree)
        {
            return new NbtVerifier(tree, _schema).Verify();
        }

        #endregion


        #region ICopyable<Entity> Members

        /// <summary>
        /// Creates a deep-copy of the <see cref="TypedEntity"/>.
        /// </summary>
        /// <returns>A deep-copy of the <see cref="TypedEntity"/>.</returns>
        public virtual new TypedEntity Copy ()
        {
            return new TypedEntity(this);
        }

        #endregion
    }

}