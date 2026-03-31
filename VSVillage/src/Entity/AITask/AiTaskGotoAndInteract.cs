using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VsVillage
{
	// Token: 0x0200000F RID: 15
	public abstract class AiTaskGotoAndInteract : AiTaskBase
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00002649 File Offset: 0x00000849
		// (set) Token: 0x06000064 RID: 100 RVA: 0x00002651 File Offset: 0x00000851
		protected float maxDistance { get; set; }

		// Token: 0x06000065 RID: 101 RVA: 0x00004B2C File Offset: 0x00002D2C
		public AiTaskGotoAndInteract(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
			: base(entity, taskConfig, aiConfig)
		{
			this.maxDistance = taskConfig["maxdistance"].AsFloat(5f);
			this.moveSpeed = taskConfig["movespeed"].AsFloat(0.08f);
			this.interactAnim = new AnimationMetaData
			{
				Code = "interact",
				Animation = taskConfig["interact"].AsString("interact")
			}.Init();
			this.pathfinder = new VillagerAStarNew(entity.World.GetCachingBlockAccessor(false, false));
			this.lastPosition = null;
			this.stuckCheckTime = 0L;
			this.timesStuck = 0;
			this.lastRepathTime = 0L;
			this.currentlyOpeningDoor = null;
			this.doorOpenedTime = 0L;
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00004BF8 File Offset: 0x00002DF8
		public override bool ShouldExecute()
		{
			long elapsedMilliseconds = this.entity.World.ElapsedMilliseconds;
			bool flag = 5000L + this.lastSearch < elapsedMilliseconds && this.cooldownUntilMs + this.lastExecution < elapsedMilliseconds;
			bool flag2 = flag;
			bool flag3 = flag2;
			if (flag3)
			{
				this.lastSearch = elapsedMilliseconds;
				this.targetPos = this.GetTargetPos();
			}
			return this.targetPos != null && this.cooldownUntilMs + this.lastExecution < elapsedMilliseconds;
		}

		// Token: 0x06000067 RID: 103
		protected abstract Vec3d GetTargetPos();

		// Token: 0x06000068 RID: 104 RVA: 0x00004C80 File Offset: 0x00002E80
		public override void StartExecute()
		{
			this.pathfinder.blockAccessor.Begin();
			this.pathfinder.SetEntityCollisionBox(this.entity);
			BlockPos startPos = this.pathfinder.GetStartPos(this.entity.ServerPos.XYZ);
			BlockPos asBlockPos = this.targetPos.AsBlockPos;
			this.currentPath = this.pathfinder.FindPath(startPos, asBlockPos, 1000);
			this.pathfinder.blockAccessor.Commit();
			bool flag = this.currentPath != null && this.currentPath.Count > 0;
			bool flag2 = flag;
			if (flag2)
			{
				this.entity.World.Logger.Debug("Found path with " + this.currentPath.Count.ToString() + " nodes");
				this.currentPathIndex = 0;
				this.stuck = false;
				this.targetReached = false;
				this.lastPosition = this.entity.ServerPos.XYZ.Clone();
				this.stuckCheckTime = this.entity.World.ElapsedMilliseconds;
				this.timesStuck = 0;
			}
			else
			{
				this.entity.World.Logger.Debug("No path found to target");
				this.stuck = true;
			}
			base.StartExecute();
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00004DDC File Offset: 0x00002FDC
		public override bool ContinueExecute(float dt)
		{
			this.CheckIfStuck();
			bool flag = this.targetReached;
			bool flag2 = flag;
			bool flag3;
			if (flag2)
			{
				flag3 = this.entity.AnimManager.IsAnimationActive(new string[] { this.interactAnim.Code });
			}
			else
			{
				bool flag4 = !this.targetReached && this.targetPos != null && this.currentPath != null;
				bool flag5 = flag4;
				if (flag5)
				{
					this.HandlePathTraversal();
				}
				bool flag6 = this.InteractionPossible();
				bool flag7 = flag6;
				if (flag7)
				{
					this.entity.Controls.WalkVector.Set(0.0, 0.0, 0.0);
					this.entity.Controls.StopAllMovement();
					this.entity.AnimManager.StopAnimation(this.animMeta.Code);
					this.entity.AnimManager.StartAnimation(this.interactAnim);
					this.targetReached = true;
					flag3 = true;
				}
				else
				{
					flag3 = !this.stuck && this.currentPath != null;
				}
			}
			return flag3;
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00004F14 File Offset: 0x00003114
		protected virtual bool InteractionPossible()
		{
			return this.targetPos != null && this.entity.ServerPos.SquareDistanceTo(this.targetPos) < 2.25;
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00004F58 File Offset: 0x00003158
		public override void FinishExecute(bool cancelled)
		{
			base.FinishExecute(cancelled);
			this.entity.Controls.WalkVector.Set(0.0, 0.0, 0.0);
			this.entity.Controls.StopAllMovement();
			this.entity.AnimManager.StopAnimation(this.animMeta.Code);
			this.CloseAllOpenDoors();
			bool flag = this.targetReached;
			bool flag2 = flag;
			if (flag2)
			{
				this.ApplyInteractionEffect();
				this.lastExecution = this.entity.World.ElapsedMilliseconds;
			}
			this.entity.AnimManager.StopAnimation("interact");
			this.targetPos = null;
			this.targetReached = false;
			this.currentPath = null;
			this.lastPosition = null;
			this.timesStuck = 0;
			this.currentlyOpeningDoor = null;
		}

		// Token: 0x0600006C RID: 108
		protected abstract void ApplyInteractionEffect();

		// Token: 0x0600006D RID: 109 RVA: 0x00005040 File Offset: 0x00003240
		protected void ToggleDoor(bool opened, BlockPos target)
		{
			Block block = this.entity.World.BlockAccessor.GetBlock(target);
			bool flag = block != null && block.Code != null;
			bool flag2 = flag;
			if (flag2)
			{
				bool flag3 = block.Code.Path.Contains("door") || block.Code.Path.Contains("gate");
				bool flag4 = flag3;
				if (flag4)
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
						this.entity.World.Logger.Debug("Toggled door at " + target.ToString() + " to " + (opened ? "open" : "closed"));
					}
					catch (Exception ex)
					{
						this.entity.World.Logger.Error("Failed to toggle door at " + target.ToString() + ": " + ex.Message);
					}
				}
			}
		}

		// Token: 0x0600006E RID: 110 RVA: 0x000051E4 File Offset: 0x000033E4
		private void HandlePathTraversal()
		{
			bool flag = this.currentlyOpeningDoor != null;
			bool flag2 = flag;
			if (flag2)
			{
				long num = this.entity.World.ElapsedMilliseconds - this.doorOpenedTime;
				bool flag3 = num < 500L;
				bool flag4 = flag3;
				if (flag4)
				{
					this.entity.Controls.WalkVector.Set(0.0, 0.0, 0.0);
					return;
				}
				this.currentlyOpeningDoor = null;
			}
			bool flag5 = this.currentPath == null || this.currentPathIndex >= this.currentPath.Count;
			bool flag6 = flag5;
			if (flag6)
			{
				this.stuck = true;
			}
			else
			{
				VillagerPathNode villagerPathNode = this.currentPath[this.currentPathIndex];
				Vec3d vec3d = villagerPathNode.BlockPos.ToVec3d().Add(0.5, 0.0, 0.5);
				Vec3d xyz = this.entity.ServerPos.XYZ;
				double num2 = xyz.X - vec3d.X;
				double num3 = xyz.Z - vec3d.Z;
				double num4 = Math.Sqrt(num2 * num2 + num3 * num3);
				bool flag7 = num4 < 0.5;
				bool flag8 = flag7;
				if (flag8)
				{
					this.currentPathIndex++;
					bool flag9 = this.currentPathIndex < this.currentPath.Count;
					bool flag10 = flag9;
					if (flag10)
					{
						VillagerPathNode villagerPathNode2 = this.currentPath[this.currentPathIndex];
						bool isDoor = villagerPathNode2.IsDoor;
						bool flag11 = isDoor;
						if (flag11)
						{
							this.entity.World.Logger.Debug("Opening door at " + villagerPathNode2.BlockPos.ToString());
							this.ToggleDoor(true, villagerPathNode2.BlockPos);
							this.currentlyOpeningDoor = villagerPathNode2.BlockPos.Copy();
							this.doorOpenedTime = this.entity.World.ElapsedMilliseconds;
						}
					}
					bool isDoor2 = villagerPathNode.IsDoor;
					bool flag12 = isDoor2;
					if (flag12)
					{
						this.entity.World.Logger.Debug("Closing door behind at " + villagerPathNode.BlockPos.ToString());
						BlockPos doorPos = villagerPathNode.BlockPos.Copy();
						this.entity.World.RegisterCallback(delegate(float dtt)
						{
							this.ToggleDoor(false, doorPos);
						}, 2000);
					}
				}
				bool flag13 = this.currentPathIndex < this.currentPath.Count;
				bool flag14 = flag13;
				if (flag14)
				{
					VillagerPathNode villagerPathNode3 = this.currentPath[this.currentPathIndex];
					Vec3d vec3d2 = villagerPathNode3.BlockPos.ToVec3d().Add(0.5, 0.0, 0.5);
					Vec3d vec3d3 = vec3d2.Clone().Sub(xyz);
					vec3d3.Y = 0.0;
					vec3d3 = vec3d3.Normalize();
					float num5 = (float)Math.Atan2(vec3d3.X, vec3d3.Z);
					this.entity.ServerPos.Yaw = num5;
					double num6 = (double)this.moveSpeed;
					this.entity.Controls.WalkVector.Set(vec3d3.X * num6, 0.0, vec3d3.Z * num6);
					bool flag15 = !this.entity.AnimManager.IsAnimationActive(new string[] { this.animMeta.Code });
					bool flag16 = flag15;
					if (flag16)
					{
						this.entity.AnimManager.StartAnimation(this.animMeta);
					}
				}
			}
		}

		// Token: 0x0600006F RID: 111 RVA: 0x000055C8 File Offset: 0x000037C8
		protected void CloseAllOpenDoors()
		{
			bool flag = this.currentPath != null;
			bool flag2 = flag;
			if (flag2)
			{
				for (int i = 0; i < this.currentPath.Count; i++)
				{
					VillagerPathNode villagerPathNode = this.currentPath[i];
					bool isDoor = villagerPathNode.IsDoor;
					bool flag3 = isDoor;
					if (flag3)
					{
						Block block = this.entity.World.BlockAccessor.GetBlock(villagerPathNode.BlockPos);
						bool flag4 = block != null && block.Code != null && (block.Code.Path.Contains("opened") || block.Code.Path.Contains("open"));
						bool flag5 = flag4;
						if (flag5)
						{
							this.ToggleDoor(false, villagerPathNode.BlockPos);
						}
					}
				}
			}
		}

		// Token: 0x06000070 RID: 112 RVA: 0x000056B0 File Offset: 0x000038B0
		private void CheckIfStuck()
		{
			long elapsedMilliseconds = this.entity.World.ElapsedMilliseconds;
			bool flag = elapsedMilliseconds - this.stuckCheckTime < 3000L;
			bool flag2 = !flag;
			if (flag2)
			{
				Vec3d xyz = this.entity.ServerPos.XYZ;
				bool flag3 = this.lastPosition != null;
				bool flag4 = flag3;
				if (flag4)
				{
					double num = (double)xyz.DistanceTo(this.lastPosition);
					bool flag5 = num < 0.5;
					bool flag6 = flag5;
					if (flag6)
					{
						this.timesStuck++;
						this.entity.World.Logger.Warning(string.Concat(new string[]
						{
							"Villager appears stuck! Distance moved: ",
							num.ToString("F2"),
							" (stuck count: ",
							this.timesStuck.ToString(),
							")"
						}));
						bool flag7 = this.timesStuck <= 5;
						bool flag8 = flag7;
						if (flag8)
						{
							long num2 = elapsedMilliseconds - this.lastRepathTime;
							bool flag9 = num2 > 5000L;
							bool flag10 = flag9;
							if (flag10)
							{
								this.entity.World.Logger.Notification("Recovery attempt " + this.timesStuck.ToString() + ": Re-pathing");
								this.AttemptRepath();
								this.lastRepathTime = elapsedMilliseconds;
							}
						}
						else
						{
							bool flag11 = this.timesStuck >= 6;
							bool flag12 = flag11;
							if (flag12)
							{
								this.entity.World.Logger.Notification("Recovery attempt FINAL: Teleporting (last resort)");
								this.TeleportToRecoveryPosition();
								this.timesStuck = 0;
							}
						}
					}
					else
					{
						bool flag13 = this.timesStuck > 0;
						bool flag14 = flag13;
						if (flag14)
						{
							this.entity.World.Logger.Debug("Villager is moving again! Distance: " + num.ToString("F2"));
						}
						this.timesStuck = 0;
					}
				}
				this.lastPosition = xyz.Clone();
				this.stuckCheckTime = elapsedMilliseconds;
			}
		}

		// Token: 0x06000071 RID: 113 RVA: 0x000058C4 File Offset: 0x00003AC4
		private void AttemptRepath()
		{
			bool flag = this.targetPos == null;
			bool flag2 = !flag;
			if (flag2)
			{
				this.entity.World.Logger.Debug("Attempting to find new path to target");
				this.pathfinder.blockAccessor.Begin();
				this.pathfinder.SetEntityCollisionBox(this.entity);
				BlockPos startPos = this.pathfinder.GetStartPos(this.entity.ServerPos.XYZ);
				BlockPos asBlockPos = this.targetPos.AsBlockPos;
				List<VillagerPathNode> list = this.pathfinder.FindPath(startPos, asBlockPos, 1000);
				this.pathfinder.blockAccessor.Commit();
				bool flag3 = list != null && list.Count > 0;
				bool flag4 = flag3;
				if (flag4)
				{
					this.entity.World.Logger.Notification("Found new path with " + list.Count.ToString() + " nodes");
					this.currentPath = list;
					this.currentPathIndex = 0;
					this.stuck = false;
				}
				else
				{
					this.entity.World.Logger.Warning("Could not find alternative path");
				}
			}
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00005A00 File Offset: 0x00003C00
		private void TeleportToRecoveryPosition()
		{
			Vec3d vec3d = null;
			bool flag = this.currentPath != null && this.currentPathIndex < this.currentPath.Count;
			bool flag2 = flag;
			if (flag2)
			{
				int num = Math.Min(2, this.currentPath.Count - this.currentPathIndex - 1);
				bool flag3 = num > 0;
				bool flag4 = flag3;
				if (flag4)
				{
					VillagerPathNode villagerPathNode = this.currentPath[this.currentPathIndex + num];
					vec3d = villagerPathNode.BlockPos.ToVec3d().Add(0.5, 0.1, 0.5);
					this.currentPathIndex += num;
					this.entity.World.Logger.Notification("Teleporting " + num.ToString() + " nodes ahead to " + vec3d.ToString());
				}
			}
			bool flag5 = vec3d == null && this.targetPos != null;
			bool flag6 = flag5;
			if (flag6)
			{
				Vec3d xyz = this.entity.ServerPos.XYZ;
				Vec3d vec3d2 = this.targetPos.Clone().Sub(xyz).Normalize();
				vec3d = xyz.Add(vec3d2.X * 2.0, 0.5, vec3d2.Z * 2.0);
				this.entity.World.Logger.Notification("Teleporting toward target to " + vec3d.ToString());
			}
			bool flag7 = vec3d != null;
			bool flag8 = flag7;
			if (flag8)
			{
				IBlockAccessor blockAccessor = this.entity.World.BlockAccessor;
				BlockPos asBlockPos = vec3d.AsBlockPos;
				Block block = blockAccessor.GetBlock(asBlockPos);
				Block block2 = blockAccessor.GetBlock(asBlockPos.UpCopy(1));
				bool flag9 = block.CollisionBoxes == null || block.CollisionBoxes.Length == 0;
				bool flag10 = block2.CollisionBoxes == null || block2.CollisionBoxes.Length == 0;
				bool flag11 = flag9 && flag10;
				bool flag12 = flag11;
				if (flag12)
				{
					this.entity.TeleportTo(vec3d);
					this.entity.World.Logger.Notification("Teleported villager to " + vec3d.ToString());
					this.entity.ServerPos.Motion.Y = 0.1;
				}
				else
				{
					this.entity.World.Logger.Warning("Teleport destination " + vec3d.ToString() + " is not safe, skipping");
					this.stuck = true;
				}
			}
		}

		// Token: 0x04000025 RID: 37
		protected float moveSpeed;

		// Token: 0x04000026 RID: 38
		protected long lastSearch;

		// Token: 0x04000027 RID: 39
		protected long lastExecution;

		// Token: 0x04000028 RID: 40
		protected bool stuck;

		// Token: 0x04000029 RID: 41
		protected Vec3d targetPos;

		// Token: 0x0400002A RID: 42
		protected AnimationMetaData interactAnim;

		// Token: 0x0400002B RID: 43
		protected bool targetReached;

		// Token: 0x0400002C RID: 44
		protected List<VillagerPathNode> currentPath;

		// Token: 0x0400002D RID: 45
		protected int currentPathIndex;

		// Token: 0x0400002E RID: 46
		protected VillagerAStarNew pathfinder;

		// Token: 0x0400002F RID: 47
		protected Vec3d lastPosition;

		// Token: 0x04000030 RID: 48
		protected long stuckCheckTime;

		// Token: 0x04000031 RID: 49
		protected int timesStuck;

		// Token: 0x04000032 RID: 50
		protected long lastRepathTime;

		// Token: 0x04000033 RID: 51
		protected BlockPos currentlyOpeningDoor;

		// Token: 0x04000034 RID: 52
		protected long doorOpenedTime;
	}
}
