using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VsVillage
{
	// Token: 0x02000067 RID: 103
	public class AiTaskVillagerWander : AiTaskBase
	{
		// Token: 0x06000271 RID: 625 RVA: 0x000134E0 File Offset: 0x000116E0
		public AiTaskVillagerWander(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
			: base(entity, taskConfig, aiConfig)
		{
			bool flag = taskConfig["moveSpeed"] != null;
			if (flag)
			{
				this.moveSpeed = taskConfig["moveSpeed"].AsFloat(0.03f);
			}
			bool flag2 = taskConfig["wanderRange"] != null;
			if (flag2)
			{
				this.wanderRange = taskConfig["wanderRange"].AsFloat(20f);
			}
			bool flag3 = taskConfig["wanderCooldownSeconds"] != null;
			if (flag3)
			{
				this.wanderCooldownMs = (long)taskConfig["wanderCooldownSeconds"].AsInt(10) * 1000L;
			}
			bool flag4 = taskConfig["entitySuffix"] != null;
			if (flag4)
			{
				this.requiredEntitySuffix = taskConfig["entitySuffix"].AsString(null);
			}
			this.pathfinder = new VillagerAStarNew(entity.World.GetCachingBlockAccessor(false, false));
			this.lastPosition = null;
			this.stuckCheckTime = 0L;
			this.timesStuck = 0;
			this.lastRepathTime = 0L;
		}

		// Token: 0x06000272 RID: 626 RVA: 0x00013610 File Offset: 0x00011810
		public override bool ShouldExecute()
		{
			bool flag = !string.IsNullOrEmpty(this.requiredEntitySuffix);
			if (flag)
			{
				bool flag2 = this.entity == null || this.entity.Code == null || this.entity.Code.Path == null || !this.entity.Code.Path.EndsWith(this.requiredEntitySuffix);
				if (flag2)
				{
					return false;
				}
			}
			bool flag3 = this.duringDayTimeFrames != null && this.duringDayTimeFrames.Length != 0;
			if (flag3)
			{
				bool flag4 = !IntervalUtil.matchesCurrentTime(this.duringDayTimeFrames, this.entity.World, 0f);
				if (flag4)
				{
					return false;
				}
			}
			long elapsedMilliseconds = this.entity.World.ElapsedMilliseconds;
			return elapsedMilliseconds - this.lastWanderTime >= this.wanderCooldownMs && this.entity.World.Rand.NextDouble() <= 0.3;
		}

		// Token: 0x06000273 RID: 627 RVA: 0x00013720 File Offset: 0x00011920
		public override void StartExecute()
		{
			base.StartExecute();
			this.lastWanderTime = this.entity.World.ElapsedMilliseconds;
			BlockPos asBlockPos = this.entity.ServerPos.AsBlockPos;
			int num = 10;
			for (int i = 0; i < num; i++)
			{
				double num2 = this.entity.World.Rand.NextDouble() * 3.141592653589793 * 2.0;
				double num3 = this.entity.World.Rand.NextDouble() * (double)this.wanderRange;
				double num4 = Math.Cos(num2) * num3;
				double num5 = Math.Sin(num2) * num3;
				BlockPos blockPos = asBlockPos.AddCopy((int)num4, 0, (int)num5);
				for (int j = 5; j >= -5; j--)
				{
					BlockPos blockPos2 = blockPos.AddCopy(0, j, 0);
					Block block = this.entity.World.BlockAccessor.GetBlock(blockPos2);
					Block block2 = this.entity.World.BlockAccessor.GetBlock(blockPos2.UpCopy(1));
					bool flag = block.SideSolid[BlockFacing.UP.Index] && (block2.CollisionBoxes == null || block2.CollisionBoxes.Length == 0);
					bool flag2 = flag;
					if (flag2)
					{
						blockPos = blockPos2.UpCopy(1);
						break;
					}
				}
				this.pathfinder.blockAccessor.Begin();
				this.pathfinder.SetEntityCollisionBox(this.entity);
				this.currentPath = this.pathfinder.FindPath(asBlockPos, blockPos, 500);
				this.pathfinder.blockAccessor.Commit();
				bool flag3 = this.currentPath != null && this.currentPath.Count > 5;
				if (flag3)
				{
					this.targetPos = blockPos.ToVec3d().Add(0.5, 0.0, 0.5);
					this.currentPathIndex = 0;
					this.stuck = false;
					this.lastPosition = this.entity.ServerPos.XYZ.Clone();
					this.stuckCheckTime = this.entity.World.ElapsedMilliseconds;
					this.timesStuck = 0;
					this.entity.World.Logger.Debug("Villager found wander path with " + this.currentPath.Count.ToString() + " nodes to " + blockPos.ToString());
					break;
				}
			}
			bool flag4 = this.currentPath == null || this.currentPath.Count <= 5;
			if (flag4)
			{
				this.entity.World.Logger.Debug("Villager could not find valid wander destination after " + num.ToString() + " attempts");
				this.stuck = true;
			}
		}

		// Token: 0x06000274 RID: 628 RVA: 0x00013A14 File Offset: 0x00011C14
		public override bool ContinueExecute(float dt)
		{
			this.CheckIfStuck();
			bool flag = this.targetPos == null || this.stuck || this.currentPath == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = this.duringDayTimeFrames != null && this.duringDayTimeFrames.Length != 0;
				if (flag3)
				{
					bool flag4 = !IntervalUtil.matchesCurrentTime(this.duringDayTimeFrames, this.entity.World, 0f);
					if (flag4)
					{
						this.entity.World.Logger.Debug("Wander: Time window ended, stopping");
						return false;
					}
				}
				bool flag5 = this.currentPathIndex >= this.currentPath.Count;
				if (flag5)
				{
					this.entity.World.Logger.Debug("Villager reached wander destination");
					flag2 = false;
				}
				else
				{
					this.HandlePathTraversal();
					double num = this.entity.ServerPos.SquareDistanceTo(this.targetPos);
					flag2 = num > 2.0;
				}
			}
			return flag2;
		}

		// Token: 0x06000275 RID: 629 RVA: 0x00013B20 File Offset: 0x00011D20
		public override void FinishExecute(bool cancelled)
		{
			base.FinishExecute(cancelled);
			this.entity.Controls.WalkVector.Set(0.0, 0.0, 0.0);
			this.entity.Controls.StopAllMovement();
			bool flag = this.animMeta != null;
			if (flag)
			{
				this.entity.AnimManager.StopAnimation(this.animMeta.Code);
			}
			this.entity.ServerPos.Motion.X = 0.0;
			this.entity.ServerPos.Motion.Z = 0.0;
			this.CloseAllOpenDoors();
			this.targetPos = null;
			this.currentPath = null;
			this.currentPathIndex = 0;
			this.lastPosition = null;
			this.timesStuck = 0;
		}

		// Token: 0x06000276 RID: 630 RVA: 0x00013C0C File Offset: 0x00011E0C
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
						this.entity.World.Logger.Error("Villager failed to toggle door: " + ex.Message);
					}
				}
			}
		}

		// Token: 0x06000277 RID: 631 RVA: 0x00013D64 File Offset: 0x00011F64
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

		// Token: 0x06000278 RID: 632 RVA: 0x00013E3C File Offset: 0x0001203C
		private void HandlePathTraversal()
		{
			bool flag = this.currentPath == null || this.currentPathIndex >= this.currentPath.Count;
			if (flag)
			{
				this.stuck = true;
			}
			else
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
							this.entity.World.Logger.Debug("Villager opening door at " + villagerPathNode2.BlockPos.ToString());
							this.ToggleDoor(true, villagerPathNode2.BlockPos);
						}
					}
					bool isDoor2 = villagerPathNode.IsDoor;
					if (isDoor2)
					{
						this.entity.World.Logger.Debug("Villager closing door at " + villagerPathNode.BlockPos.ToString());
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

		// Token: 0x06000279 RID: 633 RVA: 0x00014154 File Offset: 0x00012354
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
							"Wander: Villager stuck! Distance: ",
							num.ToString("F2"),
							" (count: ",
							this.timesStuck.ToString(),
							")"
						}));
						bool flag4 = this.timesStuck <= 5;
						if (flag4)
						{
							long num2 = elapsedMilliseconds - this.lastRepathTime;
							bool flag5 = num2 > 5000L;
							if (flag5)
							{
								this.entity.World.Logger.Notification("Wander recovery " + this.timesStuck.ToString() + ": Re-pathing");
								this.AttemptRepath();
								this.lastRepathTime = elapsedMilliseconds;
							}
						}
						else
						{
							bool flag6 = this.timesStuck >= 6;
							if (flag6)
							{
								this.entity.World.Logger.Notification("Wander recovery FINAL: Teleporting");
								this.TeleportToRecoveryPosition();
								this.timesStuck = 0;
							}
						}
					}
					else
					{
						bool flag7 = this.timesStuck > 0;
						if (flag7)
						{
							this.entity.World.Logger.Debug("Wander: Moving again! Distance: " + num.ToString("F2"));
						}
						this.timesStuck = 0;
					}
				}
				this.lastPosition = xyz.Clone();
				this.stuckCheckTime = elapsedMilliseconds;
			}
		}

		// Token: 0x0600027A RID: 634 RVA: 0x00014348 File Offset: 0x00012548
		private void AttemptRepath()
		{
			bool flag = this.targetPos == null;
			if (!flag)
			{
				this.entity.World.Logger.Debug("Wander: Finding new path");
				this.pathfinder.blockAccessor.Begin();
				this.pathfinder.SetEntityCollisionBox(this.entity);
				BlockPos startPos = this.pathfinder.GetStartPos(this.entity.ServerPos.XYZ);
				BlockPos asBlockPos = this.targetPos.AsBlockPos;
				List<VillagerPathNode> list = this.pathfinder.FindPath(startPos, asBlockPos, 500);
				this.pathfinder.blockAccessor.Commit();
				bool flag2 = list != null && list.Count > 0;
				if (flag2)
				{
					this.entity.World.Logger.Notification("Wander: New path with " + list.Count.ToString() + " nodes");
					this.currentPath = list;
					this.currentPathIndex = 0;
					this.stuck = false;
				}
				else
				{
					this.entity.World.Logger.Warning("Wander: No alternative path found");
				}
			}
		}

		// Token: 0x0600027B RID: 635 RVA: 0x00014478 File Offset: 0x00012678
		private void TeleportToRecoveryPosition()
		{
			Vec3d vec3d = null;
			bool flag = this.currentPath != null && this.currentPathIndex < this.currentPath.Count;
			if (flag)
			{
				int num = Math.Min(2, this.currentPath.Count - this.currentPathIndex - 1);
				bool flag2 = num > 0;
				if (flag2)
				{
					VillagerPathNode villagerPathNode = this.currentPath[this.currentPathIndex + num];
					vec3d = villagerPathNode.BlockPos.ToVec3d().Add(0.5, 0.1, 0.5);
					this.currentPathIndex += num;
					this.entity.World.Logger.Notification("Wander: Teleporting " + num.ToString() + " nodes ahead");
				}
			}
			bool flag3 = vec3d == null && this.targetPos != null;
			if (flag3)
			{
				Vec3d xyz = this.entity.ServerPos.XYZ;
				Vec3d vec3d2 = this.targetPos.Clone().Sub(xyz).Normalize();
				vec3d = xyz.Add(vec3d2.X * 2.0, 0.5, vec3d2.Z * 2.0);
				this.entity.World.Logger.Notification("Wander: Teleporting toward target");
			}
			bool flag4 = vec3d != null;
			if (flag4)
			{
				IBlockAccessor blockAccessor = this.entity.World.BlockAccessor;
				BlockPos asBlockPos = vec3d.AsBlockPos;
				Block block = blockAccessor.GetBlock(asBlockPos);
				Block block2 = blockAccessor.GetBlock(asBlockPos.UpCopy(1));
				bool flag5 = block.CollisionBoxes == null || block.CollisionBoxes.Length == 0;
				bool flag6 = block2.CollisionBoxes == null || block2.CollisionBoxes.Length == 0;
				bool flag7 = flag5 && flag6;
				if (flag7)
				{
					this.entity.TeleportTo(vec3d);
					this.entity.World.Logger.Notification("Wander: Teleported to " + vec3d.ToString());
					this.entity.ServerPos.Motion.Y = 0.1;
				}
				else
				{
					this.entity.World.Logger.Warning("Wander: Teleport target unsafe");
					this.stuck = true;
				}
			}
		}

		// Token: 0x04000183 RID: 387
		private Vec3d targetPos;

		// Token: 0x04000184 RID: 388
		private float moveSpeed = 0.03f;

		// Token: 0x04000185 RID: 389
		private float wanderRange = 20f;

		// Token: 0x04000186 RID: 390
		private long lastWanderTime;

		// Token: 0x04000187 RID: 391
		private long wanderCooldownMs = 10000L;

		// Token: 0x04000188 RID: 392
		private string requiredEntitySuffix;

		// Token: 0x04000189 RID: 393
		private VillagerAStarNew pathfinder;

		// Token: 0x0400018A RID: 394
		private List<VillagerPathNode> currentPath;

		// Token: 0x0400018B RID: 395
		private int currentPathIndex;

		// Token: 0x0400018C RID: 396
		private bool stuck;

		// Token: 0x0400018D RID: 397
		private Vec3d lastPosition;

		// Token: 0x0400018E RID: 398
		private long stuckCheckTime;

		// Token: 0x0400018F RID: 399
		private int timesStuck;

		// Token: 0x04000190 RID: 400
		private long lastRepathTime;
	}
}
