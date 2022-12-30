using System.Threading;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Models
{
    public class EntitiesCollection
    {
        #region Public Properties

        public EntityTypeCollection<Player> Players { get; set; }

        #endregion Public Properties

        #region Private Properties

        private EntityTypeCollection<Creature> Creatures { get; set; }
        private EntityTypeCollection<GameObject> GameObjects { get; set; }
        private EntityTypeCollection<Item> Items { get; set; }
        private ValueCollection<MapType> Map { get; set; }
        private EntityTypeCollection<Npc> Npc { get; set; }
        private EntityTypeCollection<Entity> UnCategorized { get; set; }
        private EntityTypeCollection<Entity> UnCategorizedUnit { get; set; }
        private Thread UpdateUnitThread { get; set; }
        private WorldClient WorldClient { get; set; }

        #endregion Private Properties

        #region Internal Constructors

        internal EntitiesCollection(WorldClient client)
        {
            WorldClient = client;
            Players = new EntityTypeCollection<Player>();
            Npc = new EntityTypeCollection<Npc>();
            Creatures = new EntityTypeCollection<Creature>();
            Items = new EntityTypeCollection<Item>();
            GameObjects = new EntityTypeCollection<GameObject>();
            Map = new ValueCollection<MapType>();
            UnCategorized = new EntityTypeCollection<Entity>();
            UnCategorizedUnit = new EntityTypeCollection<Entity>();
            UpdateUnitThread = new Thread(UpdateUnit);
            UpdateUnitThread.Start();
        }

        #endregion Internal Constructors

        #region Internal Methods

        internal void Categorize(Entity entity, TypeID type)
        {
            entity.Type = type;
            switch (type)
            {
                case TypeID.TYPEID_ITEM:
                    Items.Add(new Item(entity));
                    Map.Set(entity.Guid, MapType.ITEM);
                    break;

                case TypeID.TYPEID_UNIT:
                    UnCategorizedUnit.Add(entity);
                    Map.Set(entity.Guid, MapType.UNIT);
                    break;

                case TypeID.TYPEID_PLAYER:
                    Players.Add(new Player(entity));
                    Map.Set(entity.Guid, MapType.PLAYER);
                    WorldClient.Send(new NameQueryRequest(entity.Guid));
                    break;

                case TypeID.TYPEID_GAMEOBJECT:
                    GameObjects.Add(new GameObject(entity));
                    Map.Set(entity.Guid, MapType.GAME_OBJECT);
                    break;

                default:
                    return;
            }

            UnCategorized.Remove(entity.Guid);
        }

        internal void Close()
        {
            UpdateUnitThread.Interrupt();
        }

        internal void DestroyEntity(ulong guid)
        {
            MapType type = Map.Contains(guid) ? Map.Get(guid) : MapType.UNKNOWN;

            switch (type)
            {
                case MapType.UNKNOWN:
                    UnCategorized.Remove(guid);
                    break;

                case MapType.PLAYER:
                    Players.Remove(guid);
                    break;

                case MapType.UNIT:
                    UnCategorizedUnit.Remove(guid);
                    break;

                case MapType.NPC:
                    Npc.Remove(guid);
                    break;

                case MapType.CREATURE:
                    Creatures.Remove(guid);
                    break;

                case MapType.ITEM:
                    Items.Remove(guid);
                    break;

                case MapType.GAME_OBJECT:
                    GameObjects.Remove(guid);
                    break;
            }
        }

        internal Player GetPlayer()
        {
            ulong? guid = WorldClient?.GetCharacter()?.GUID;
            if (guid == null) return null;
            return Players.Get(guid.Value);
        }

        internal Entity GetUnit(ulong guid)
        {
            MapType type = Map.Contains(guid) ? Map.Get(guid) : MapType.UNKNOWN;
            Entity entity = null;
            switch (type)
            {
                case MapType.UNKNOWN:
                    entity = UnCategorized.Get(guid);
                    break;

                case MapType.PLAYER:
                    entity = Players.Get(guid);
                    break;

                case MapType.UNIT:
                    entity = UnCategorizedUnit.Get(guid);
                    break;

                case MapType.NPC:
                    entity = Npc.Get(guid);
                    break;

                case MapType.CREATURE:
                    entity = Creatures.Get(guid);
                    break;

                case MapType.ITEM:
                    entity = Items.Get(guid);
                    break;

                case MapType.GAME_OBJECT:
                    entity = GameObjects.Get(guid);
                    break;
            }

            return entity ?? AddEntity(guid);
        }

        #endregion Internal Methods

        #region Private Methods

        private Entity AddEntity(ulong guid)
        {
            Entity entity = new Entity(guid);
            UnCategorized.Add(entity);
            Map.Add(guid, MapType.UNKNOWN);
            return entity;
        }

        private void UpdateUnit()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(100);

                    Entity entity = UnCategorizedUnit.FirstOrDefault();

                    if (entity == null) continue;
                    if (!entity.Fields.ContainsKey(UpdateFields.OBJECT_FIELD_ENTRY)) continue;

                    uint entryId = entity.Fields[UpdateFields.OBJECT_FIELD_ENTRY];
                    UnitInfo info = WorldClient.Query.GetUnitInfo(entryId, entity.Guid).Result;

                    if (info == null) continue;

                    if (entity.Fields.ContainsKey(UpdateFields.UNIT_FIELD_FLAGS) && entity.Fields[UpdateFields.UNIT_FIELD_FLAGS] > 0)
                    {
                        Npc.Add(new Npc(entity, info) { Name = info.Name });
                        Map.Add(entity.Guid, MapType.NPC);
                    }
                    else
                    {
                        Creatures.Add(new Creature(entity, info) { Name = info.Name });
                        Map.Add(entity.Guid, MapType.CREATURE);
                    }
                    UnCategorizedUnit.Remove(entity.Guid);
                }
            }
            catch (ThreadInterruptedException)
            {
                // Ending process
            }
        }

        #endregion Private Methods
    }
}