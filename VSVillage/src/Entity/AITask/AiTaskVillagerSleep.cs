using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VsVillage
{
	// Token: 0x0200001C RID: 28
	public class AiTaskVillagerSleep : AiTaskBase
	{
		// Token: 0x060000C3 RID: 195 RVA: 0x00007AE4 File Offset: 0x00005CE4
		public AiTaskVillagerSleep(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
			: base(entity, taskConfig, aiConfig)
		{
			this.moveSpeed = taskConfig["movespeed"].AsFloat(0.06f);
			this.offset = (float)entity.World.Rand.Next(taskConfig["minoffset"].AsInt(-50), taskConfig["maxoffset"].AsInt(50)) / 100f;
			this.pathfinder = new VillagerAStarNew(entity.World.GetCachingBlockAccessor(false, false));
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00007B7C File Offset: 0x00005D7C
		public override bool ShouldExecute()
		{
			float hourOfDay = this.entity.World.Calendar.HourOfDay;
			bool flag = !this.ManualTimeCheck(hourOfDay);
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = this.isExecuting;
				if (flag3)
				{
					flag2 = true;
				}
				else
				{
					long elapsedMilliseconds = this.entity.World.ElapsedMilliseconds;
					bool flag4 = elapsedMilliseconds - this.lastSearchTime > 5000L;
					bool flag5 = !flag4;
					if (flag5)
					{
						flag2 = false;
					}
					else
					{
						this.lastSearchTime = elapsedMilliseconds;
						this.entity.World.Logger.Debug("Sleep: Entity " + this.entity.EntityId.ToString() + " searching for bed...");
						EntityBehaviorVillager behavior = this.entity.GetBehavior<EntityBehaviorVillager>();
						Village village = ((behavior != null) ? behavior.Village : null);
						bool flag6 = village == null;
						if (flag6)
						{
							this.entity.World.Logger.Warning("Sleep: No village found for entity " + this.entity.EntityId.ToString());
							flag2 = false;
						}
						else
						{
							BlockPos blockPos = village.FindFreeBed(this.entity.EntityId);
							bool flag7 = blockPos == null;
							if (flag7)
							{
								this.entity.World.Logger.Warning("Sleep: No free bed found for entity " + this.entity.EntityId.ToString());
								flag2 = false;
							}
							else
							{
								this.targetBedPos = blockPos.Copy();
								this.entity.World.Logger.Notification("Sleep: Entity " + this.entity.EntityId.ToString() + " found bed at " + this.targetBedPos.ToString());
								Vec3d bedStandingPos = this.GetBedStandingPos(this.targetBedPos.ToVec3d());
								bool flag8 = bedStandingPos == null;
								if (flag8)
								{
									this.entity.World.Logger.Warning("Sleep: Could not find standing position near bed");
									this.targetBedPos = null;
									flag2 = false;
								}
								else
								{
									this.targetPos = bedStandingPos;
									flag2 = true;
								}
							}
						}
					}
				}
			}
			return flag2;
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00007DA0 File Offset: 0x00005FA0
		public override void StartExecute()
		{
			base.StartExecute();
			this.isExecuting = true;
			this.entity.World.Logger.Notification("Sleep: StartExecute for entity " + this.entity.EntityId.ToString());
			bool flag = this.targetPos == null;
			if (flag)
			{
				this.entity.World.Logger.Warning("Sleep: StartExecute but targetPos is null!");
				this.stuck = true;
			}
			else
			{
				this.pathfinder.blockAccessor.Begin();
				this.pathfinder.SetEntityCollisionBox(this.entity);
				BlockPos startPos = this.pathfinder.GetStartPos(this.entity.ServerPos.XYZ);
				BlockPos asBlockPos = this.targetPos.AsBlockPos;
				this.currentPath = this.pathfinder.FindPath(startPos, asBlockPos, 1000);
				this.pathfinder.blockAccessor.Commit();
				bool flag2 = this.currentPath != null && this.currentPath.Count > 0;
				if (flag2)
				{
					this.entity.World.Logger.Notification("Sleep: Found path with " + this.currentPath.Count.ToString() + " nodes to bed");
					this.currentPathIndex = 0;
					this.stuck = false;
					this.reachedBed = false;
					this.lastPosition = this.entity.ServerPos.XYZ.Clone();
					this.stuckCheckTime = this.entity.World.ElapsedMilliseconds;
					this.timesStuck = 0;
				}
				else
				{
					this.entity.World.Logger.Warning("Sleep: No path found to bed at " + this.targetPos.ToString());
					this.stuck = true;
				}
			}
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00007F78 File Offset: 0x00006178
		public override bool ContinueExecute(float dt)
		{
			bool flag = this.targetPos == null || this.stuck || this.currentPath == null;
			bool flag2;
			if (flag)
			{
				this.entity.World.Logger.Debug(string.Concat(new string[]
				{
					"Sleep: ContinueExecute ending - targetPos:",
					(this.targetPos == null).ToString(),
					" stuck:",
					this.stuck.ToString(),
					" path:",
					(this.currentPath == null).ToString()
				}));
				flag2 = false;
			}
			else
			{
				float hourOfDay = this.entity.World.Calendar.HourOfDay;
				bool flag3 = !this.ManualTimeCheck(hourOfDay);
				if (flag3)
				{
					this.entity.World.Logger.Notification("Sleep: Time to wake up! Entity " + this.entity.EntityId.ToString());
					flag2 = false;
				}
				else
				{
					bool flag4 = this.reachedBed;
					if (flag4)
					{
						flag2 = this.entity.AnimManager.IsAnimationActive(new string[] { "Lie" });
					}
					else
					{
						this.CheckIfStuck();
						bool flag5 = this.currentPathIndex >= this.currentPath.Count;
						if (flag5)
						{
							this.entity.World.Logger.Notification("Sleep: Entity " + this.entity.EntityId.ToString() + " reached end of path, starting sleep");
							this.StartSleeping();
							flag2 = true;
						}
						else
						{
							this.HandlePathTraversal();
							bool flag6 = this.IsAtBed();
							if (flag6)
							{
								this.entity.World.Logger.Notification("Sleep: Entity " + this.entity.EntityId.ToString() + " at bed, starting sleep");
								this.StartSleeping();
								flag2 = true;
							}
							else
							{
								flag2 = !this.stuck;
							}
						}
					}
				}
			}
			return flag2;
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x0000817C File Offset: 0x0000637C
		public override void FinishExecute(bool cancelled)
		{
			base.FinishExecute(cancelled);
			this.isExecuting = false;
			this.entity.World.Logger.Debug("Sleep: FinishExecute for entity " + this.entity.EntityId.ToString() + ", cancelled: " + cancelled.ToString());
			this.entity.Controls.WalkVector.Set(0.0, 0.0, 0.0);
			this.entity.Controls.StopAllMovement();
			this.entity.ServerPos.Motion.Set(0.0, 0.0, 0.0);
			this.CloseAllOpenDoors();
			bool flag = this.animMeta != null;
			if (flag)
			{
				this.entity.AnimManager.StopAnimation(this.animMeta.Code);
			}
			this.entity.AnimManager.StopAnimation("Lie");
			this.targetPos = null;
			this.targetBedPos = null;
			this.currentPath = null;
			this.currentPathIndex = 0;
			this.reachedBed = false;
			this.lastPosition = null;
			this.timesStuck = 0;
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x000082C4 File Offset: 0x000064C4
		private void ToggleDoor(bool opened, BlockPos target)
		{
			Block block = this.entity.World.BlockAccessor.GetBlock(target);
			bool flag = block != null && block.Code != null;
			if (flag)
			{
				bool flag2 = block.Code.Path.Contains("door") || block.Code.Path.Contains("gate");
				if (flag2)
				{
					BlockSelection blockSelection = new BlockSelection
					{
						Block = block,
						Position = target,
						HitPosition = new Vec3d(0.5, 0.5, 0.5),
						Face = BlockFacing.NORTH
					};
					TreeAttribute treeAttribute = new TreeAttribute();
					treeAttribute.SetBool("opened", opened);
					try
					{
						block.Activate(this.entity.World, new Caller
						{
							Entity = this.entity,
							Type = EnumCallerType.Entity,
							Pos = this.entity.Pos.XYZ
						}, blockSelection, treeAttribute);
					}
					catch (Exception ex)
					{
						this.entity.World.Logger.Error("Sleep: Failed to toggle door: " + ex.Message);
					}
				}
			}
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x0000841C File Offset: 0x0000661C
		private void HandlePathTraversal()
		{
			bool flag = this.currentPath == null || this.currentPathIndex >= this.currentPath.Count;
			if (!flag)
			{
				VillagerPathNode villagerPathNode = this.currentPath[this.currentPathIndex];
				Vec3d vec3d = villagerPathNode.BlockPos.ToVec3d().Add(0.5, 0.0, 0.5);
				Vec3d xyz = this.entity.ServerPos.XYZ;
				double num = xyz.X - vec3d.X;
				double num2 = xyz.Z - vec3d.Z;
				double num3 = Math.Sqrt(num * num + num2 * num2);
				bool flag2 = num3 < 0.5;
				if (flag2)
				{
					this.currentPathIndex++;
					bool flag3 = this.currentPathIndex < this.currentPath.Count;
					if (flag3)
					{
						VillagerPathNode villagerPathNode2 = this.currentPath[this.currentPathIndex];
						bool isDoor = villagerPathNode2.IsDoor;
						if (isDoor)
						{
							this.ToggleDoor(true, villagerPathNode2.BlockPos);
						}
					}
					bool isDoor2 = villagerPathNode.IsDoor;
					if (isDoor2)
					{
						BlockPos doorPos = villagerPathNode.BlockPos.Copy();
						this.entity.World.RegisterCallback(delegate(float dtt)
						{
							this.ToggleDoor(false, doorPos);
						}, 2000);
					}
				}
				bool flag4 = this.currentPathIndex < this.currentPath.Count;
				if (flag4)
				{
					VillagerPathNode villagerPathNode3 = this.currentPath[this.currentPathIndex];
					Vec3d vec3d2 = villagerPathNode3.BlockPos.ToVec3d().Add(0.5, 0.0, 0.5);
					Vec3d vec3d3 = vec3d2.Clone().Sub(xyz);
					vec3d3.Y = 0.0;
					vec3d3 = vec3d3.Normalize();
					float num4 = (float)Math.Atan2(vec3d3.X, vec3d3.Z);
					this.entity.ServerPos.Yaw = num4;
					double num5 = (double)this.moveSpeed;
					this.entity.Controls.WalkVector.Set(vec3d3.X * num5, 0.0, vec3d3.Z * num5);
					bool flag5 = this.animMeta != null && !this.entity.AnimManager.IsAnimationActive(new string[] { this.animMeta.Code });
					if (flag5)
					{
						this.entity.AnimManager.StartAnimation(this.animMeta);
					}
				}
			}
		}

		// Token: 0x060000CA RID: 202 RVA: 0x000086D4 File Offset: 0x000068D4
		private void CloseAllOpenDoors()
		{
			bool flag = this.currentPath != null;
			if (flag)
			{
				for (int i = 0; i < this.currentPath.Count; i++)
				{
					VillagerPathNode villagerPathNode = this.currentPath[i];
					bool isDoor = villagerPathNode.IsDoor;
					if (isDoor)
					{
						Block block = this.entity.World.BlockAccessor.GetBlock(villagerPathNode.BlockPos);
						bool flag2 = block != null && block.Code != null && (block.Code.Path.Contains("opened") || block.Code.Path.Contains("open"));
						if (flag2)
						{
							this.ToggleDoor(false, villagerPathNode.BlockPos);
						}
					}
				}
			}
		}

		// Token: 0x060000CB RID: 203 RVA: 0x000087AC File Offset: 0x000069AC
		private bool IsAtBed()
		{
			return this.targetPos != null && this.entity.ServerPos.SquareDistanceTo(this.targetPos) < 2.25;
		}

		// Token: 0x060000CC RID: 204 RVA: 0x000087F0 File Offset: 0x000069F0
		private void StartSleeping()
		{
			this.entity.Controls.WalkVector.Set(0.0, 0.0, 0.0);
			this.entity.Controls.StopAllMovement();
			this.entity.ServerPos.Motion.Set(0.0, 0.0, 0.0);
			bool flag = this.animMeta != null;
			if (flag)
			{
				this.entity.AnimManager.StopAnimation(this.animMeta.Code);
			}
			this.entity.AnimManager.StopAnimation("walk");
			this.entity.AnimManager.StopAnimation("Walk");
			this.entity.AnimManager.StopAnimation("balanced-walk");
			this.entity.AnimManager.StopAnimation("interact");
			bool flag2 = this.targetBedPos != null;
			if (flag2)
			{
				BlockEntityVillagerBed blockEntity = this.entity.World.BlockAccessor.GetBlockEntity<BlockEntityVillagerBed>(this.targetBedPos);
				bool flag3 = blockEntity != null;
				if (flag3)
				{
					Vec3d bedSleepPosition = this.GetBedSleepPosition(blockEntity);
					this.entity.ServerPos.SetPos(bedSleepPosition);
					this.entity.ServerPos.Yaw = blockEntity.Yaw;
				}
				else
				{
					this.FaceBed();
				}
			}
			AnimationMetaData animationMetaData = new AnimationMetaData
			{
				Code = "Lie",
				Animation = "Lie",
				AnimationSpeed = 1f
			}.Init();
			this.entity.AnimManager.StartAnimation(animationMetaData);
			this.reachedBed = true;
			this.entity.World.Logger.Notification("Sleep: Entity " + this.entity.EntityId.ToString() + " now sleeping at " + ((this.targetBedPos != null) ? this.targetBedPos.ToString() : "unknown"));
		}

		// Token: 0x060000CD RID: 205 RVA: 0x00008A08 File Offset: 0x00006C08
		private void FaceBed()
		{
			bool flag = this.targetBedPos != null;
			if (flag)
			{
				Vec3d xyz = this.entity.ServerPos.XYZ;
				Vec3d vec3d = this.targetBedPos.ToVec3d().Add(0.5, 0.5, 0.5);
				double num = vec3d.X - xyz.X;
				double num2 = vec3d.Z - xyz.Z;
				float num3 = (float)Math.Atan2(num, num2);
				this.entity.ServerPos.Yaw = num3;
				this.entity.Pos.Yaw = num3;
			}
		}

		// Token: 0x060000CE RID: 206 RVA: 0x00008AB8 File Offset: 0x00006CB8
		private Vec3d GetBedStandingPos(Vec3d bedCenter)
		{
			bool flag = bedCenter == null;
			Vec3d vec3d;
			if (flag)
			{
				vec3d = null;
			}
			else
			{
				IBlockAccessor blockAccessor = this.entity.World.BlockAccessor;
				BlockPos asBlockPos = bedCenter.AsBlockPos;
				Block block = blockAccessor.GetBlock(asBlockPos);
				BlockFacing blockFacing = BlockFacing.NORTH;
				bool flag2 = block != null && block.Variant != null;
				if (flag2)
				{
					string text;
					bool flag3 = block.Variant.TryGetValue("side", out text);
					if (flag3)
					{
						blockFacing = BlockFacing.FromCode(text) ?? BlockFacing.NORTH;
					}
					else
					{
						bool flag4 = block.Variant.TryGetValue("facing", out text);
						if (flag4)
						{
							blockFacing = BlockFacing.FromCode(text) ?? BlockFacing.NORTH;
						}
						else
						{
							bool flag5 = block.Variant.TryGetValue("orientation", out text);
							if (flag5)
							{
								blockFacing = BlockFacing.FromCode(text) ?? BlockFacing.NORTH;
							}
						}
					}
				}
				BlockFacing opposite = blockFacing.Opposite;
				Vec3d vec3d2 = bedCenter.AddCopy((double)opposite.Normalf.X * 1.2, 0.0, (double)opposite.Normalf.Z * 1.2);
				BlockPos asBlockPos2 = vec3d2.AsBlockPos;
				bool flag6 = blockAccessor.GetBlock(asBlockPos2.Up(1)).Id == 0;
				bool flag7 = blockAccessor.GetBlock(asBlockPos2).Id != 0;
				bool flag8 = flag6 && flag7;
				if (flag8)
				{
					vec3d = vec3d2;
				}
				else
				{
					vec3d = bedCenter;
				}
			}
			return vec3d;
		}

		// Token: 0x060000CF RID: 207 RVA: 0x00008C34 File Offset: 0x00006E34
		private void CheckIfStuck()
		{
			long elapsedMilliseconds = this.entity.World.ElapsedMilliseconds;
			bool flag = elapsedMilliseconds - this.stuckCheckTime < 3000L;
			if (!flag)
			{
				Vec3d xyz = this.entity.ServerPos.XYZ;
				bool flag2 = this.lastPosition != null;
				if (flag2)
				{
					double num = (double)xyz.DistanceTo(this.lastPosition);
					bool flag3 = num < 0.5;
					if (flag3)
					{
						this.timesStuck++;
						this.entity.World.Logger.Warning(string.Concat(new string[]
						{
							"Sleep: Entity ",
							this.entity.EntityId.ToString(),
							" stuck! (count: ",
							this.timesStuck.ToString(),
							")"
						}));
						bool flag4 = this.timesStuck <= 3;
						if (flag4)
						{
							long num2 = elapsedMilliseconds - this.lastRepathTime;
							bool flag5 = num2 > 5000L;
							if (flag5)
							{
								this.AttemptRepath();
								this.lastRepathTime = elapsedMilliseconds;
							}
						}
						else
						{
							bool flag6 = this.timesStuck >= 4;
							if (flag6)
							{
								this.entity.World.Logger.Notification("Sleep: Entity " + this.entity.EntityId.ToString() + " teleporting to safe location");
								this.TeleportToSafeLocation();
								this.timesStuck = 0;
							}
						}
					}
					else
					{
						this.timesStuck = 0;
					}
				}
				this.lastPosition = xyz.Clone();
				this.stuckCheckTime = elapsedMilliseconds;
			}
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x00008DD8 File Offset: 0x00006FD8
		private void AttemptRepath()
		{
			bool flag = this.targetPos == null;
			if (!flag)
			{
				this.entity.World.Logger.Debug("Sleep: Entity " + this.entity.EntityId.ToString() + " attempting repath");
				this.pathfinder.blockAccessor.Begin();
				this.pathfinder.SetEntityCollisionBox(this.entity);
				BlockPos startPos = this.pathfinder.GetStartPos(this.entity.ServerPos.XYZ);
				BlockPos asBlockPos = this.targetPos.AsBlockPos;
				List<VillagerPathNode> list = this.pathfinder.FindPath(startPos, asBlockPos, 1000);
				this.pathfinder.blockAccessor.Commit();
				bool flag2 = list != null && list.Count > 0;
				if (flag2)
				{
					this.entity.World.Logger.Debug("Sleep: Found new path with " + list.Count.ToString() + " nodes");
					this.currentPath = list;
					this.currentPathIndex = 0;
				}
				else
				{
					this.entity.World.Logger.Warning("Sleep: Could not find alternative path");
				}
			}
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x00008F18 File Offset: 0x00007118
		private bool ManualTimeCheck(float currentHour)
		{
			float num = 21.5f + this.offset;
			float num2 = 6f + this.offset;
			bool flag = num > num2;
			bool flag2;
			if (flag)
			{
				flag2 = currentHour >= num || currentHour <= num2;
			}
			else
			{
				flag2 = currentHour >= num && currentHour <= num2;
			}
			return flag2;
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00008F70 File Offset: 0x00007170
		private void TeleportToSafeLocation()
		{
			Vec3d vec3d = null;
			string text = "";
			bool flag = this.targetPos != null;
			if (flag)
			{
				bool flag2 = this.IsPositionSafe(this.targetPos);
				if (flag2)
				{
					vec3d = this.targetPos;
					text = "bed";
				}
			}
			bool flag3 = vec3d == null;
			if (flag3)
			{
				EntityBehaviorVillager behavior = this.entity.GetBehavior<EntityBehaviorVillager>();
				Village village = ((behavior != null) ? behavior.Village : null);
				bool flag4 = village != null;
				if (flag4)
				{
					BlockPos blockPos = village.FindRandomGatherplace();
					bool flag5 = blockPos != null;
					if (flag5)
					{
						Vec3d vec3d2 = blockPos.ToVec3d().Add(0.5, 1.0, 0.5);
						bool flag6 = this.IsPositionSafe(vec3d2);
						if (flag6)
						{
							vec3d = vec3d2;
							text = "gatherplace";
						}
					}
				}
			}
			bool flag7 = vec3d == null;
			if (flag7)
			{
				EntityBehaviorVillager behavior2 = this.entity.GetBehavior<EntityBehaviorVillager>();
				Village village2 = ((behavior2 != null) ? behavior2.Village : null);
				bool flag8 = village2 != null && behavior2 != null;
				if (flag8)
				{
					BlockPos blockPos2 = village2.FindFreeWorkstation(this.entity.EntityId, behavior2.Profession);
					bool flag9 = blockPos2 != null;
					if (flag9)
					{
						Vec3d vec3d3 = blockPos2.ToVec3d().Add(0.5, 1.0, 0.5);
						bool flag10 = this.IsPositionSafe(vec3d3);
						if (flag10)
						{
							vec3d = vec3d3;
							text = "workstation";
						}
					}
				}
			}
			bool flag11 = vec3d != null;
			if (flag11)
			{
				this.entity.TeleportTo(vec3d);
				this.entity.World.Logger.Notification(string.Concat(new string[]
				{
					"Sleep: Teleported entity ",
					this.entity.EntityId.ToString(),
					" to ",
					text,
					" at ",
					vec3d.ToString()
				}));
				bool flag12 = text == "bed";
				if (flag12)
				{
					this.StartSleeping();
				}
				else
				{
					this.AttemptRepath();
				}
			}
			else
			{
				this.entity.World.Logger.Warning("Sleep: Entity " + this.entity.EntityId.ToString() + " could not find safe teleport location, giving up");
				this.stuck = true;
			}
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x000091DC File Offset: 0x000073DC
		private bool IsPositionSafe(Vec3d pos)
		{
			bool flag = pos == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				IBlockAccessor blockAccessor = this.entity.World.BlockAccessor;
				BlockPos asBlockPos = pos.AsBlockPos;
				Block block = blockAccessor.GetBlock(asBlockPos);
				Block block2 = blockAccessor.GetBlock(asBlockPos.UpCopy(1));
				bool flag3 = block.CollisionBoxes == null || block.CollisionBoxes.Length == 0;
				bool flag4 = block2.CollisionBoxes == null || block2.CollisionBoxes.Length == 0;
				Block block3 = blockAccessor.GetBlock(asBlockPos.DownCopy(1));
				bool flag5 = block3.CollisionBoxes != null && block3.CollisionBoxes.Length != 0;
				flag2 = flag3 && flag4 && flag5;
			}
			return flag2;
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00009294 File Offset: 0x00007494
		private Vec3d GetBedSleepPosition(BlockEntityVillagerBed bed)
		{
			string text = bed.Block.Variant["side"];
			bool flag = text == "north";
			Cardinal cardinal;
			if (flag)
			{
				cardinal = Cardinal.North;
			}
			else
			{
				bool flag2 = text == "east";
				if (flag2)
				{
					cardinal = Cardinal.East;
				}
				else
				{
					bool flag3 = text == "south";
					if (flag3)
					{
						cardinal = Cardinal.South;
					}
					else
					{
						cardinal = Cardinal.West;
					}
				}
			}
			return bed.Pos.ToVec3d().Add(0.5, 0.0, 0.5).Add(cardinal.Normalf.Clone().Mul(0.7f));
		}

		// Token: 0x04000066 RID: 102
		private float moveSpeed = 0.06f;

		// Token: 0x04000067 RID: 103
		private float offset;

		// Token: 0x04000068 RID: 104
		private VillagerAStarNew pathfinder;

		// Token: 0x04000069 RID: 105
		private List<VillagerPathNode> currentPath;

		// Token: 0x0400006A RID: 106
		private int currentPathIndex;

		// Token: 0x0400006B RID: 107
		private bool stuck;

		// Token: 0x0400006C RID: 108
		private Vec3d targetPos;

		// Token: 0x0400006D RID: 109
		private BlockPos targetBedPos;

		// Token: 0x0400006E RID: 110
		private long lastSearchTime;

		// Token: 0x0400006F RID: 111
		private bool reachedBed;

		// Token: 0x04000070 RID: 112
		private Vec3d lastPosition;

		// Token: 0x04000071 RID: 113
		private long stuckCheckTime;

		// Token: 0x04000072 RID: 114
		private int timesStuck;

		// Token: 0x04000073 RID: 115
		private long lastRepathTime;

		// Token: 0x04000074 RID: 116
		private bool isExecuting;
	}
}
