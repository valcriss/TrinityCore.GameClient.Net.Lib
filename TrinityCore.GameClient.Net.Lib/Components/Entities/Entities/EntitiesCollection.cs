using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Commands;
using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Entities
{
    internal class EntitiesCollection
    {
        internal Dictionary<ulong, Creature> Creatures { get; set; }
        internal Dictionary<ulong, GameObject> GameObjects { get; set; }
        internal Dictionary<ulong, Item> Items { get; set; }
        internal Dictionary<ulong, Npc> Npc { get; set; }
        internal Dictionary<ulong, Player> Players { get; set; }
        private Dictionary<ulong, MapType> Map { get; set; }
        private Dictionary<ulong, Entity> UnCategorized { get; set; }
        private Dictionary<ulong, Entity> UnCategorizedUnit { get; set; }
        private Thread UpdateUnitThread { get; set; }
        private WorldClient WorldClient { get; set; }

        internal EntitiesCollection(WorldClient client)
        {
            WorldClient = client;
            Players = new Dictionary<ulong, Player>();
            Npc = new Dictionary<ulong, Npc>();
            Creatures = new Dictionary<ulong, Creature>();
            Items = new Dictionary<ulong, Item>();
            GameObjects = new Dictionary<ulong, GameObject>();
            Map = new Dictionary<ulong, MapType>();
            UnCategorized = new Dictionary<ulong, Entity>();
            UnCategorizedUnit = new Dictionary<ulong, Entity>();
            UpdateUnitThread = new Thread(UpdateUnit);
            UpdateUnitThread.Start();
        }

        internal void Categorize(Entity entity, TypeID type)
        {
            entity.Type = type;
            switch (type)
            {
                case TypeID.TYPEID_ITEM:
                    lock (Items)
                    {
                        if (!Items.ContainsKey(entity.Guid))
                        {
                            Items.Add(entity.Guid, new Item(entity));
                            Map[entity.Guid] = MapType.ITEM;
                        }
                    }
                    break;

                case TypeID.TYPEID_UNIT:
                    lock (UnCategorizedUnit)
                    {
                        if (!UnCategorizedUnit.ContainsKey(entity.Guid))
                        {
                            UnCategorizedUnit.Add(entity.Guid, entity);
                            Map[entity.Guid] = MapType.UNIT;
                        }
                    }
                    break;

                case TypeID.TYPEID_PLAYER:
                    lock (Players)
                    {
                        if (!Players.ContainsKey(entity.Guid))
                        {
                            Players.Add(entity.Guid, new Player(entity));
                            Map[entity.Guid] = MapType.PLAYER;
                            WorldClient.Send(new NameQueryRequest(WorldClient, entity.Guid));
                        }
                    }
                    break;

                case TypeID.TYPEID_GAMEOBJECT:
                    lock (GameObjects)
                    {
                        if (!GameObjects.ContainsKey(entity.Guid))
                        {
                            GameObjects.Add(entity.Guid, new GameObject(entity));
                            Map[entity.Guid] = MapType.GAME_OBJECT;
                        }
                    }
                    break;

                default:
                    Logger.Log("Unhandled TypeId : " + type + " Keeping on UnCategorized", LogLevel.WARNING);
                    return;
            }

            lock (UnCategorized)
            {
                if (UnCategorized.ContainsKey(entity.Guid))
                {
                    UnCategorized.Remove(entity.Guid);
                }
            }
            
            DisplayStatus();
        } 

        internal void Close()
        {
            UpdateUnitThread.Abort();
        }

        internal void DestroyEntity(ulong guid)
        {
            MapType type = MapType.UNKNOWN;
            lock (Map)
            {
                if (Map.ContainsKey(guid))
                {
                    type = Map[guid];
                }
            }

            switch (type)
            {
                case MapType.UNKNOWN:
                    lock (UnCategorized)
                    {
                        if (UnCategorized.ContainsKey(guid))
                        {
                            UnCategorized.Remove(guid);
                        }
                    }
                    break;

                case MapType.PLAYER:
                    lock (Players)
                    {
                        if (Players.ContainsKey(guid))
                        {
                            Players.Remove(guid);
                        }
                    }
                    break;

                case MapType.UNIT:
                    lock (UnCategorizedUnit)
                    {
                        if (UnCategorizedUnit.ContainsKey(guid))
                        {
                            UnCategorizedUnit.Remove(guid);
                        }
                    }
                    break;

                case MapType.NPC:
                    lock (Npc)
                    {
                        if (Npc.ContainsKey(guid))
                        {
                            Npc.Remove(guid);
                        }
                    }
                    break;

                case MapType.CREATURE:
                    lock (Creatures)
                    {
                        if (Creatures.ContainsKey(guid))
                        {
                            Creatures.Remove(guid);
                        }
                    }
                    break;

                case MapType.ITEM:
                    lock (Items)
                    {
                        if (Items.ContainsKey(guid))
                        {
                            Items.Remove(guid);
                        }
                    }
                    break;

                case MapType.GAME_OBJECT:
                    lock (GameObjects)
                    {
                        if (GameObjects.ContainsKey(guid))
                        {
                            GameObjects.Remove(guid);
                        }
                    }
                    break;
            }

            DisplayStatus();
        }

        internal Player GetPlayer()
        {
            ulong? guid = WorldClient?.Character?.GUID;
            if (guid == null) return null;
            if (!Players.ContainsKey(guid.Value)) return null;
            return Players[guid.Value];
        }

        internal Entity GetUnit(ulong guid)
        {
            MapType type = MapType.UNKNOWN;
            lock (Map)
            {
                if (Map.ContainsKey(guid))
                {
                    type = Map[guid];
                }
            }

            switch (type)
            {
                case MapType.UNKNOWN:
                    lock (UnCategorized)
                    {
                        if (UnCategorized.ContainsKey(guid))
                        {
                            return UnCategorized[guid];
                        }
                    }
                    break;

                case MapType.PLAYER:
                    lock (Players)
                    {
                        if (Players.ContainsKey(guid))
                        {
                            return Players[guid];
                        }
                    }
                    break;

                case MapType.UNIT:
                    lock (UnCategorizedUnit)
                    {
                        if (UnCategorizedUnit.ContainsKey(guid))
                        {
                            return UnCategorizedUnit[guid];
                        }
                    }
                    break;

                case MapType.NPC:
                    lock (Npc)
                    {
                        if (Npc.ContainsKey(guid))
                        {
                            return Npc[guid];
                        }
                    }
                    break;

                case MapType.CREATURE:
                    lock (Creatures)
                    {
                        if (Creatures.ContainsKey(guid))
                        {
                            return Creatures[guid];
                        }
                    }
                    break;

                case MapType.ITEM:
                    lock (Items)
                    {
                        if (Items.ContainsKey(guid))
                        {
                            return Items[guid];
                        }
                    }
                    break;

                case MapType.GAME_OBJECT:
                    lock (GameObjects)
                    {
                        if (GameObjects.ContainsKey(guid))
                        {
                            return GameObjects[guid];
                        }
                    }
                    break;
            }

            return AddEntity(guid);
        }

        private Entity AddEntity(ulong guid)
        {
            Entity entity = new Entity(guid);
            lock (UnCategorized)
            {
                if (UnCategorized.ContainsKey(guid))
                {
                    UnCategorized.Add(guid, entity);
                }
            }

            lock (Map)
            {
                if (!Map.ContainsKey(guid))
                {
                    Map.Add(guid, MapType.UNKNOWN);
                }
            }
            return entity;
        }

        private void DisplayStatus(bool force = false)
        {
            Logger.Log("----------------------------------------------------------------", force ? LogLevel.INFO : LogLevel.DETAIL);
            Logger.Log("UnCategorized entities          : " + UnCategorized.Count, force ? LogLevel.INFO : LogLevel.DETAIL);
            Logger.Log("Players entities                : " + Players.Count, force ? LogLevel.INFO : LogLevel.DETAIL);
            Logger.Log("Npc entities                    : " + Npc.Count, force ? LogLevel.INFO : LogLevel.DETAIL);
            Logger.Log("Creature entities               : " + Creatures.Count, force ? LogLevel.INFO : LogLevel.DETAIL);
            Logger.Log("UnCategorized Unit entities     : " + UnCategorizedUnit.Count, force ? LogLevel.INFO : LogLevel.DETAIL);
            Logger.Log("Items entities                  : " + Items.Count, force ? LogLevel.INFO : LogLevel.DETAIL);
            Logger.Log("GameObjects entities            : " + GameObjects.Count, force ? LogLevel.INFO : LogLevel.DETAIL);
            Logger.Log("----------------------------------------------------------------", force ? LogLevel.INFO : LogLevel.DETAIL);
        }

        private void UpdateUnit()
        {
            try
            {
                while (true)
                {
                    Entity entity;
                    lock (UnCategorizedUnit)
                    {
                        entity = UnCategorizedUnit.Values.FirstOrDefault();
                    }

                    if (entity != null)
                    {
                        if (entity.Fields.ContainsKey(UpdateFields.OBJECT_FIELD_ENTRY))
                        {
                            uint entryId = entity.Fields[UpdateFields.OBJECT_FIELD_ENTRY];
                            UnitInfo info = WorldClient.Query.GetUnitInfo(entryId, entity.Guid).Result;
                            if (info != null)
                            {
                                if (entity.Fields.ContainsKey(UpdateFields.UNIT_FIELD_FLAGS) && entity.Fields[UpdateFields.UNIT_FIELD_FLAGS] > 0)
                                {
                                    lock (Npc)
                                    {
                                        if (!Npc.ContainsKey(entity.Guid))
                                        {
                                            Npc npc = new Npc(entity, info);
                                            npc.Name = npc.Infos.Name;
                                            Npc.Add(entity.Guid, npc);
                                        }
                                    }

                                    lock (Map)
                                    {
                                        Map[entity.Guid] = MapType.NPC;
                                    }
                                }
                                else
                                {
                                    lock (Creatures)
                                    {
                                        if (!Creatures.ContainsKey(entity.Guid))
                                        {
                                            Creature creature = new Creature(entity, info);
                                            creature.Name = creature.Infos.Name;
                                            Creatures.Add(entity.Guid, creature);
                                        }
                                    }
                                    lock (Map)
                                    {
                                        Map[entity.Guid] = MapType.CREATURE;
                                    }
                                }

                                lock (UnCategorizedUnit)
                                {
                                    if (UnCategorizedUnit.ContainsKey(entity.Guid))
                                    {
                                        UnCategorizedUnit.Remove(entity.Guid);
                                    }
                                }

                                DisplayStatus();
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (ThreadAbortException)
            {
            }
        }
    }
}
