using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VsVillage
{
	// Token: 0x02000015 RID: 21
	public class AiTaskVillagerGotoGatherspot : AiTaskGotoAndInteract
	{
		// Token: 0x06000087 RID: 135 RVA: 0x0000638C File Offset: 0x0000458C
		public AiTaskVillagerGotoGatherspot(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
			: base(entity, taskConfig, aiConfig)
		{
			this.offset = (float)entity.World.Rand.Next(taskConfig["minoffset"].AsInt(-50), taskConfig["maxoffset"].AsInt(50)) / 100f;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x000063E8 File Offset: 0x000045E8
		protected override void ApplyInteractionEffect()
		{
			bool flag = this.brazier != null;
			bool flag2 = flag;
			if (flag2)
			{
				this.brazier.Ignite();
				this.entity.World.Logger.Notification("Villager ignited brazier");
			}
			this.brazier = null;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00006438 File Offset: 0x00004638
		protected override Vec3d GetTargetPos()
		{
			ICoreAPI api = this.entity.Api;
			EntityBehaviorVillager behavior = this.entity.GetBehavior<EntityBehaviorVillager>();
			Village village = ((behavior != null) ? behavior.Village : null);
			BlockPos blockPos = ((village != null) ? village.FindRandomGatherplace() : null);
			this.brazier = ((blockPos != null) ? api.World.BlockAccessor.GetBlockEntity<BlockEntityVillagerBrazier>(blockPos) : null);
			Vec3d vec3d = ((this.brazier != null) ? this.brazier.Position : null);
			bool flag = vec3d == null;
			bool flag2 = flag;
			Vec3d vec3d2;
			if (flag2)
			{
				this.entity.World.Logger.Debug("No gatherplace found for villager");
				vec3d2 = null;
			}
			else
			{
				this.entity.World.Logger.Notification("Villager going to gatherplace at " + vec3d.ToString());
				vec3d2 = this.getRandomPosNearby(vec3d);
			}
			return vec3d2;
		}

		// Token: 0x0600008A RID: 138 RVA: 0x0000651C File Offset: 0x0000471C
		public override bool ShouldExecute()
		{
			bool flag = !IntervalUtil.matchesCurrentTime(this.duringDayTimeFrames, this.entity.World, this.offset);
			bool flag2 = flag;
			bool flag3;
			if (flag2)
			{
				flag3 = false;
			}
			else
			{
				long elapsedMilliseconds = this.entity.World.ElapsedMilliseconds;
				bool flag4 = elapsedMilliseconds - this.lastExecutionTime < 10000L;
				bool flag5 = flag4;
				flag3 = !flag5 && base.ShouldExecute();
			}
			return flag3;
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00006590 File Offset: 0x00004790
		private Vec3d getRandomPosNearby(Vec3d middle)
		{
			bool flag = middle == null;
			bool flag2 = flag;
			Vec3d vec3d;
			if (flag2)
			{
				vec3d = null;
			}
			else
			{
				IBlockAccessor blockAccessor = this.entity.World.BlockAccessor;
				for (int i = 0; i < 5; i++)
				{
					int num = this.entity.World.Rand.Next(-3, 4);
					int num2 = this.entity.World.Rand.Next(-3, 4);
					Vec3d vec3d2 = middle.AddCopy((float)num2, 0f, (float)num);
					bool flag3 = blockAccessor.GetBlock(vec3d2.AsBlockPos.Up(1)).Id == 0;
					bool flag4 = flag3;
					if (flag4)
					{
						this.entity.World.Logger.Debug("Found random position near gatherplace: " + vec3d2.ToString());
						return vec3d2;
					}
				}
				this.entity.World.Logger.Debug("Could not find open position, using center");
				vec3d = middle;
			}
			return vec3d;
		}

		// Token: 0x0600008C RID: 140 RVA: 0x0000669C File Offset: 0x0000489C
		public override bool ContinueExecute(float dt)
		{
			return base.ContinueExecute(dt);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00002738 File Offset: 0x00000938
		public override void FinishExecute(bool cancelled)
		{
			this.entity.World.Logger.Notification("GotoGatherspot: Finishing execution");
			this.entity.Controls.StopAllMovement();
			base.FinishExecute(cancelled);
		}

		// Token: 0x0600008E RID: 142 RVA: 0x0000276F File Offset: 0x0000096F
		public override void StartExecute()
		{
			this.lastExecutionTime = this.entity.World.ElapsedMilliseconds;
			this.entity.World.Logger.Notification("GotoGatherspot: Starting execution");
			base.StartExecute();
		}

		// Token: 0x04000041 RID: 65
		private float offset;

		// Token: 0x04000042 RID: 66
		private BlockEntityVillagerBrazier brazier;

		// Token: 0x04000043 RID: 67
		private long lastExecutionTime;
	}
}
