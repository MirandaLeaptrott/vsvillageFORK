using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace VsVillage;

public class VillageManager : ModSystem
{
    public ConcurrentDictionary<string, Village> Villages = new ConcurrentDictionary<string, Village>();

    private ICoreAPI Api;

    private const int villagerHiringCost = 5;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        Api = api;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        api.Event.GameWorldSave += delegate
        {
            OnSave(api);
        };
        api.Network.RegisterChannel("villagemanagementnetwork").RegisterMessageType<Village>().RegisterMessageType<VillageManagementMessage>()
            .SetMessageHandler(delegate (IServerPlayer fromPlayer, VillageManagementMessage message)
            {
                OnManagementMessage(fromPlayer, message, api);
            });
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        api.Network.RegisterChannel("villagemanagementnetwork").RegisterMessageType<Village>().SetMessageHandler(delegate (Village village)
        {
            OnVillageMessage(village, api);
        })
            .RegisterMessageType<VillageManagementMessage>();
    }

    public Village GetVillage(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }
        if (!Villages.TryGetValue(id, out var value) && Api is ICoreServerAPI coreServerAPI)
        {
            try
            {
                byte[] data = coreServerAPI.WorldManager.SaveGame.GetData(id);
                value = ((data == null || data.Length < 10) ? null : SerializerUtil.Deserialize<Village>(data));
                value?.Init(coreServerAPI);
                if (value != null)
                {
                    Villages.TryAdd(id, value);
                }
            }
            catch (Exception)
            {
                Api.Logger.Error($"Village with id={id} could not be loaded and will be newly created. Maybe it was removed/ outdated/ corrupted. I guess we will never know for sure because I am too lazy to log this information.");
                List<int> list = Regex.Match(id, "village-(\\d+), (\\d+), (\\d+)").Groups.Values.ToList().GetRange(1, 3).ConvertAll((Group number) => int.Parse(number.Value));
                value = new Village
                {
                    Pos = new BlockPos(list[0], list[1], list[2]),
                    Radius = 50,
                    Name = "Lauras little Village"
                };
                value?.Init(coreServerAPI);
                Villages.TryAdd(id, value);
            }
        }
        return value;
    }

    public Village GetVillage(BlockPos pos)
    {
        foreach (Village value in Villages.Values)
        {
            BlockPos pos2 = value.Pos;
            int radius = value.Radius;
            if (pos2.X - radius <= pos.X && pos2.X + radius >= pos.X && pos2.Z - radius <= pos.Z && pos2.Z + radius >= pos.Z)
            {
                return value;
            }
        }
        return null;
    }

    private void OnSave(ICoreServerAPI sapi)
    {
        foreach (Village value in Villages.Values)
        {
            sapi.WorldManager.SaveGame.StoreData(value.Id, SerializerUtil.Serialize(value));
        }
    }

    public void RemoveVillage(string id)
    {
        if (Villages.ContainsKey(id))
        {
            Village village = Villages.Get(id);
            Villages.Remove(id);
            (Api as ICoreServerAPI).WorldManager.SaveGame.StoreData(id, null);
            village.Workstations.Values.Foreach(delegate (VillagerWorkstation workstation)
            {
                Api.World.BlockAccessor.GetBlockEntity<BlockEntityVillagerWorkstation>(workstation.Pos)?.RemoveVillage();
            });
            village.Gatherplaces.Foreach(delegate (BlockPos gatherplace)
            {
                Api.World.BlockAccessor.GetBlockEntity<BlockEntityVillagerBrazier>(gatherplace)?.RemoveVillage();
            });
            village.Beds.Values.Foreach(delegate (VillagerBed bed)
            {
                Api.World.BlockAccessor.GetBlockEntity<BlockEntityVillagerBed>(bed.Pos)?.RemoveVillage();
            });
            village.Villagers.ForEach(delegate (EntityBehaviorVillager villager)
            {
                villager?.RemoveVillage();
            });
        }
    }

    private void OnVillageMessage(Village village, ICoreClientAPI capi)
    {
        village.Init(capi);
        new ManagementGui(capi, village.Pos, village).TryOpen();
    }

    private void OnManagementMessage(IServerPlayer fromPlayer, VillageManagementMessage message, ICoreServerAPI api)
    {
        switch (message.Operation)
        {
            case EnumVillageManagementOperation.create:
                {
                    Village village5 = new Village
                    {
                        Radius = ((message.Radius > 0) ? message.Radius : 20),
                        Pos = message.Pos,
                        Name = (string.IsNullOrEmpty(message.Name) ? "Lauras little Village" : message.Name)
                    };
                    village5.Init(api);
                    BlockEntityVillagerWorkstation blockEntity = api.World.BlockAccessor.GetBlockEntity<BlockEntityVillagerWorkstation>(message.Pos);
                    blockEntity.VillageId = village5.Id;
                    blockEntity.VillageName = village5.Name;
                    blockEntity.MarkDirty();
                    village5.Workstations.Add(message.Pos, new VillagerWorkstation
                    {
                        OwnerId = -1L,
                        Pos = message.Pos,
                        Profession = blockEntity.Profession
                    });
                    Villages.TryAdd(village5.Id, village5);
                    break;
                }
            case EnumVillageManagementOperation.destroy:
                RemoveVillage(message.Id);
                break;
            case EnumVillageManagementOperation.removeVillager:
                {
                    Village village4 = GetVillage(message.Id);
                    Entity dismissedEntity = Api.World.GetEntityById(message.VillagerToRemove);
                    BlockPos villageCenter = village4?.Pos;
                    dismissedEntity?.GetBehavior<EntityBehaviorVillager>()?.RemoveVillage();
                    village4?.RemoveVillager(message.VillagerToRemove);
                    if (dismissedEntity != null)
                    {
                        Vec3d departTarget = null;
                        if (villageCenter != null)
                        {
                            double angle = Api.World.Rand.NextDouble() * Math.PI * 2.0;
                            departTarget = villageCenter.ToVec3d().Add(
                                Math.Cos(angle) * 42.0,
                                1.0,
                                Math.Sin(angle) * 42.0
                            );
                        }
                        Vec3d finalTarget = departTarget;
                        long eid = message.VillagerToRemove;
                        Api.World.RegisterCallback(delegate
                        {
                            Entity e = Api.World.GetEntityById(eid);
                            if (e != null && e.Alive && finalTarget != null)
                            {
                                e.TeleportTo(finalTarget);
                            }
                        }, 2000);
                        Api.World.RegisterCallback(delegate
                        {
                            Entity e = Api.World.GetEntityById(eid);
                            if (e != null && e.Alive)
                            {
                                (Api as ICoreServerAPI)?.World.DespawnEntity(e, new EntityDespawnData { Reason = EnumDespawnReason.Removed });
                            }
                        }, 10000);
                    }
                    break;
                }
            case EnumVillageManagementOperation.removeStructure:
                {
                    Village village3 = GetVillage(message.Id);
                    if (village3.Workstations.Remove(message.Pos))
                    {
                        api.World.BlockAccessor.GetBlockEntity<BlockEntityVillagerWorkstation>(message.Pos)?.RemoveVillage();
                    }
                    if (village3.Gatherplaces.Remove(message.Pos))
                    {
                        api.World.BlockAccessor.GetBlockEntity<BlockEntityVillagerBrazier>(message.Pos)?.RemoveVillage();
                    }
                    if (village3.Beds.Remove(message.Pos))
                    {
                        api.World.BlockAccessor.GetBlockEntity<BlockEntityVillagerBed>(message.Pos)?.RemoveVillage();
                    }
                    break;
                }
            case EnumVillageManagementOperation.changeStats:
                {
                    Village village2 = GetVillage(message.Id);
                    village2.Radius = message.Radius;
                    village2.Name = message.Name;
                    break;
                }
            case EnumVillageManagementOperation.hireVillager:
                {
                    Village village = GetVillage(message.Id);
                    TryHireVillager(message.VillagerProfession, message.VillagerType, village, fromPlayer);
                    break;
                }
        }
    }

    private bool TryHireVillager(EnumVillagerProfession profession, string type, Village village, IServerPlayer fromPlayer)
    {
        if (village == null)
        {
            Api.Logger.Error("TryHireVillager called with null village");
            return false;
        }
        if (village.Beds == null || village.Workstations == null || village.VillagerSaveData == null)
        {
            Api.Logger.Error($"Village {village.Id} has null collections - Beds:{village.Beds == null}, Workstations:{village.Workstations == null}, VillagerSaveData:{village.VillagerSaveData == null}");
            fromPlayer?.SendIngameError("village-not-initialized", null);
            return false;
        }
        if (fromPlayer?.InventoryManager == null)
        {
            Api.Logger.Error("TryHireVillager called with null player inventory");
            return false;
        }

        if (village.Beds.Values.Where((VillagerBed bed) => bed.OwnerId == -1).Count() == 0)
        {
            fromPlayer.SendIngameError("not-enough-beds", null);
            return false;
        }

        if (village.Workstations.Values.Where((VillagerWorkstation workstation) => workstation.Profession == profession && workstation.OwnerId == -1).Count() == 0)
        {
            fromPlayer.SendIngameError("not-enough-workstations", null);
            return false;
        }

        if (profession != EnumVillagerProfession.farmer && profession != EnumVillagerProfession.shepherd)
        {
            List<EntityBehaviorVillager> list = village.Villagers.Where((EntityBehaviorVillager v) => v != null).ToList();
            int num = list.Where((EntityBehaviorVillager villager) => villager.Profession == EnumVillagerProfession.farmer || villager.Profession == EnumVillagerProfession.shepherd).Count();
            if (2 * num - list.Count <= 0)
            {
                fromPlayer.SendIngameError("not-enough-food", null);
                return false;
            }
        }

        int gears = 0;
        foreach (IInventory inventory in fromPlayer.InventoryManager.Inventories.Values)
        {
            if (inventory.ClassName == "creative") continue;
            foreach (ItemSlot slot in inventory)
            {
                if (slot?.Itemstack?.Collectible?.Code?.Path == "gear-rusty")
                {
                    gears += slot.Itemstack.StackSize;
                }
            }
        }

        if (gears < villagerHiringCost)
        {
            fromPlayer.SendIngameError("not-enough-gears", null);
            return false;
        }

        if (!(fromPlayer.Entity.Api.World is IServerWorldAccessor serverWorldAccessor))
        {
            Api.Logger.Error("Could not get server world accessor");
            return false;
        }

        string text2 = string.Format("vsvillage:villager-{0}-{1}", (serverWorldAccessor.Rand.Next(0, 2) == 0) ? "male" : "female", type);
        EntityProperties entityType = serverWorldAccessor.GetEntityType(new AssetLocation(text2));
        if (entityType == null)
        {
            fromPlayer.SendIngameError("no-valid-villager", null, text2);
            return false;
        }

        Entity entity = serverWorldAccessor.ClassRegistry.CreateEntity(entityType);
        if (entity == null)
        {
            Api.Logger.Error("Failed to create entity of type " + text2);
            return false;
        }

        VillagerBed villagerBed = village.Beds.Values.Where((VillagerBed bed) => bed.OwnerId == -1).First();
        entity.ServerPos.X = villagerBed.Pos.X;
        entity.ServerPos.Y = villagerBed.Pos.Y;
        entity.ServerPos.Z = villagerBed.Pos.Z;
        serverWorldAccessor.SpawnEntity(entity);
        villagerBed.OwnerId = entity.EntityId;

        EntityBehaviorVillager behavior = entity.GetBehavior<EntityBehaviorVillager>();
        if (behavior != null)
        {
            behavior.Bed = villagerBed.Pos;
            // Claim the workstation immediately on hire — mirrors what AiTaskVillagerGotoWork.GetTargetPos
            // does lazily on first work tick, but doing it here prevents rapid-hire exploits where
            // multiple villagers are hired before any of them tick their work task.
            behavior.Workstation = village.FindFreeWorkstation(entity.EntityId, profession);
        }
        else
        {
            Api.Logger.Warning("Spawned villager entity " + text2 + " is missing EntityBehaviorVillager behavior");
            // Still claim the workstation even without the behavior, to prevent double-hire.
            village.FindFreeWorkstation(entity.EntityId, profession);
        }

        fromPlayer.Entity.World.PlaySoundFor(new AssetLocation("sounds/effect/cashregister"), fromPlayer, randomizePitch: false, 32f, 0.25f);

        gears = 0;
        foreach (IInventory inventory2 in fromPlayer.InventoryManager.Inventories.Values)
        {
            if (inventory2.ClassName == "creative") continue;
            foreach (ItemSlot slot2 in inventory2)
            {
                if (slot2?.Itemstack?.Collectible?.Code?.Path == "gear-rusty")
                {
                    ItemStack stack = slot2.TakeOut(Math.Min(slot2.Itemstack.StackSize, villagerHiringCost - gears));
                    slot2.MarkDirty();
                    gears += stack.StackSize;
                }
                if (gears >= villagerHiringCost) break;
            }
            if (gears >= villagerHiringCost) break;
        }

        return true;
    }
}