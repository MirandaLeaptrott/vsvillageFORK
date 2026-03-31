using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VsVillage
{
	// Token: 0x02000016 RID: 22
	public class AiTaskVillagerGotoWork : AiTaskGotoAndInteract
	{
		// Token: 0x0600008F RID: 143 RVA: 0x000066B8 File Offset: 0x000048B8
		public AiTaskVillagerGotoWork(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
			: base(entity, taskConfig, aiConfig)
		{
			this.offset = (float)entity.World.Rand.Next(taskConfig["minoffset"].AsInt(-50), taskConfig["maxoffset"].AsInt(50)) / 100f;
		}

		// Token: 0x06000090 RID: 144 RVA: 0x000027AA File Offset: 0x000009AA
		protected override void ApplyInteractionEffect()
		{
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00006714 File Offset: 0x00004914
		protected override Vec3d GetTargetPos()
		{
			EntityBehaviorVillager behavior = this.entity.GetBehavior<EntityBehaviorVillager>();
			Village village = ((behavior != null) ? behavior.Village : null);
			bool flag = village == null;
			Vec3d vec3d;
			if (flag)
			{
				vec3d = null;
			}
			else
			{
				BlockPos blockPos = behavior.Workstation;
				bool flag2 = blockPos == null;
				if (flag2)
				{
					blockPos = village.FindFreeWorkstation(this.entity.EntityId, behavior.Profession);
					behavior.Workstation = blockPos;
				}
				else
				{
					VillagerWorkstation villagerWorkstation;
					village.Workstations.TryGetValue(blockPos, out villagerWorkstation);
					bool flag3 = villagerWorkstation == null || villagerWorkstation.OwnerId != this.entity.EntityId;
					if (flag3)
					{
						blockPos = null;
						behavior.Workstation = null;
					}
				}
				bool flag4 = blockPos != null;
				if (flag4)
				{
					this.workstationPos = blockPos.Copy();
				}
				vec3d = this.getWorkstationStandingPos((blockPos != null) ? blockPos.ToVec3d() : null);
			}
			return vec3d;
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00006800 File Offset: 0x00004A00
		public override bool ShouldExecute()
		{
			return base.ShouldExecute() && IntervalUtil.matchesCurrentTime(this.duringDayTimeFrames, this.entity.World, this.offset);
		}

		// Token: 0x06000093 RID: 147 RVA: 0x0000683C File Offset: 0x00004A3C
		private Vec3d getRandomPosNearby(Vec3d middle)
		{
			bool flag = middle == null;
			Vec3d vec3d;
			if (flag)
			{
				vec3d = null;
			}
			else
			{
				IBlockAccessor blockAccessor = this.entity.World.BlockAccessor;
				for (int i = 0; i < 5; i++)
				{
					int num = this.entity.World.Rand.Next(-1, 2);
					int num2 = this.entity.World.Rand.Next(-1, 2);
					Vec3d vec3d2 = middle.AddCopy((float)num2, 0f, (float)num);
					bool flag2 = blockAccessor.GetBlock(vec3d2.AsBlockPos.Up(1)).Id == 0;
					if (flag2)
					{
						return vec3d2;
					}
				}
				vec3d = middle;
			}
			return vec3d;
		}

		// Token: 0x06000094 RID: 148 RVA: 0x000068F4 File Offset: 0x00004AF4
		public override bool ContinueExecute(float dt)
		{
			bool flag = this.workstationPos != null && this.targetReached;
			if (flag)
			{
				Vec3d xyz = this.entity.ServerPos.XYZ;
				Vec3d targetPos = this.targetPos;
				bool flag2 = targetPos != null;
				if (flag2)
				{
					double num = (double)xyz.DistanceTo(targetPos);
					bool flag3 = num < 2.0;
					if (flag3)
					{
						this.FaceWorkstation();
					}
				}
			}
			return base.ContinueExecute(dt);
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00006978 File Offset: 0x00004B78
		private Vec3d getWorkstationStandingPos(Vec3d workstationCenter)
		{
			bool flag = workstationCenter == null;
			Vec3d vec3d;
			if (flag)
			{
				vec3d = null;
			}
			else
			{
				IBlockAccessor blockAccessor = this.entity.World.BlockAccessor;
				BlockPos asBlockPos = workstationCenter.AsBlockPos;
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
				Vec3d vec3d2 = workstationCenter.AddCopy((double)opposite.Normalf.X * 1.2, 0.0, (double)opposite.Normalf.Z * 1.2);
				BlockPos asBlockPos2 = vec3d2.AsBlockPos;
				bool flag6 = blockAccessor.GetBlock(asBlockPos2.Up(1)).Id == 0 && blockAccessor.GetBlock(asBlockPos2).Id != 0;
				if (flag6)
				{
					vec3d = vec3d2;
				}
				else
				{
					vec3d = this.getRandomPosNearby(workstationCenter);
				}
			}
			return vec3d;
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00006AF4 File Offset: 0x00004CF4
		private void FaceWorkstation()
		{
			bool flag = this.workstationPos != null;
			if (flag)
			{
				Vec3d xyz = this.entity.ServerPos.XYZ;
				Vec3d vec3d = this.workstationPos.ToVec3d().Add(0.5, 0.5, 0.5);
				double num = vec3d.X - xyz.X;
				double num2 = vec3d.Z - xyz.Z;
				float num3 = (float)Math.Atan2(num, num2);
				this.entity.ServerPos.Yaw = num3;
				this.entity.Pos.Yaw = num3;
			}
		}

		// Token: 0x04000044 RID: 68
		private float offset;

		// Token: 0x04000045 RID: 69
		private BlockPos workstationPos;
	}
}
