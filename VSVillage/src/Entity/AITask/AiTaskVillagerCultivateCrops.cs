using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VsVillage
{
	// Token: 0x02000060 RID: 96
	public class AiTaskVillagerCultivateCrops : AiTaskGotoAndInteract
	{
		// Token: 0x06000241 RID: 577 RVA: 0x00011F00 File Offset: 0x00010100
		public AiTaskVillagerCultivateCrops(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
			: base(entity, taskConfig, aiConfig)
		{
			this.recentlyCultivatedFarmland = new Dictionary<BlockPos, long>();
			bool flag = taskConfig["farmlandCooldownSeconds"] != null;
			bool flag2 = flag;
			bool flag3 = flag2;
			if (flag3)
			{
				this.farmlandCooldownMs = (long)(taskConfig["farmlandCooldownSeconds"].AsInt(60) * 1000);
			}
			else
			{
				this.farmlandCooldownMs = 60000L;
			}
		}

		// Token: 0x06000242 RID: 578 RVA: 0x00011F6C File Offset: 0x0001016C
		protected override Vec3d GetTargetPos()
		{
			bool flag = !this.IsFarmer();
			bool flag2 = flag;
			bool flag3 = flag2;
			Vec3d vec3d;
			if (flag3)
			{
				vec3d = null;
			}
			else
			{
				this.nearestFarmland = this.entity.Api.ModLoader.GetModSystem<POIRegistry>(true).GetNearestPoi(this.entity.ServerPos.XYZ, base.maxDistance, new PoiMatcher(this.isValidFarmland)) as BlockEntityFarmland;
				bool flag4 = this.nearestFarmland == null;
				bool flag5 = flag4;
				bool flag6 = flag5;
				if (flag6)
				{
					vec3d = null;
				}
				else
				{
					BlockPos pos = this.nearestFarmland.Pos;
					this.entity.World.Logger.Debug("Cultivate: Found farmland at " + pos.ToString() + ", targeting directly");
					vec3d = pos.ToVec3d().Add(0.5, 1.0, 0.5);
				}
			}
			return vec3d;
		}

		// Token: 0x06000243 RID: 579 RVA: 0x00012068 File Offset: 0x00010268
		protected override void ApplyInteractionEffect()
		{
			bool flag = !this.IsFarmer();
			bool flag2 = !flag;
			bool flag3 = flag2;
			if (flag3)
			{
				bool flag4 = this.nearestFarmland == null;
				bool flag5 = !flag4;
				bool flag6 = flag5;
				if (flag6)
				{
					bool flag7 = this.nearestFarmland.HasUnripeCrop();
					bool flag8 = flag7;
					bool flag9 = flag8;
					if (flag9)
					{
						this.entity.AnimManager.StartAnimation(new AnimationMetaData
						{
							Animation = "hoe-till",
							Code = "hoe-till",
							AnimationSpeed = 1f,
							BlendMode = EnumAnimationBlendMode.Average
						}.Init());
						this.entity.World.RegisterCallback(delegate(float dt)
						{
							this.PerformCultivation();
						}, 1500);
					}
				}
			}
		}

		// Token: 0x06000244 RID: 580 RVA: 0x0001212C File Offset: 0x0001032C
		private bool isValidFarmland(IPointOfInterest poi)
		{
			BlockEntityFarmland blockEntityFarmland = poi as BlockEntityFarmland;
			return blockEntityFarmland != null && blockEntityFarmland.HasUnripeCrop() && !this.IsFarmlandOnCooldown(blockEntityFarmland.Pos) && this.entity.World.Rand.NextDouble() < 0.2;
		}

		// Token: 0x06000245 RID: 581 RVA: 0x00003831 File Offset: 0x00001A31
		public override void FinishExecute(bool cancelled)
		{
			this.entity.AnimManager.StopAnimation("hoe-till");
			base.FinishExecute(cancelled);
		}

		// Token: 0x06000246 RID: 582 RVA: 0x00012184 File Offset: 0x00010384
		private bool IsFarmer()
		{
			return this.entity != null && this.entity.Code != null && this.entity.Code.Path != null && this.entity.Code.Path.EndsWith("-farmer");
		}

		// Token: 0x06000247 RID: 583 RVA: 0x000121E0 File Offset: 0x000103E0
		private bool IsFarmlandOnCooldown(BlockPos pos)
		{
			long elapsedMilliseconds = this.entity.World.ElapsedMilliseconds;
			List<BlockPos> list = new List<BlockPos>();
			foreach (KeyValuePair<BlockPos, long> keyValuePair in this.recentlyCultivatedFarmland)
			{
				bool flag = elapsedMilliseconds - keyValuePair.Value > this.farmlandCooldownMs;
				bool flag2 = flag;
				bool flag3 = flag2;
				if (flag3)
				{
					list.Add(keyValuePair.Key);
				}
			}
			for (int i = 0; i < list.Count; i++)
			{
				this.recentlyCultivatedFarmland.Remove(list[i]);
			}
			long num;
			return this.recentlyCultivatedFarmland.TryGetValue(pos, out num) && elapsedMilliseconds - num < this.farmlandCooldownMs;
		}

		// Token: 0x06000248 RID: 584 RVA: 0x00003852 File Offset: 0x00001A52
		private void MarkFarmlandCultivated(BlockPos pos)
		{
			this.recentlyCultivatedFarmland[pos.Copy()] = this.entity.World.ElapsedMilliseconds;
		}

		// Token: 0x06000249 RID: 585 RVA: 0x000122D0 File Offset: 0x000104D0
		private void PerformCultivation()
		{
			bool flag = this.nearestFarmland == null || !this.nearestFarmland.HasUnripeCrop();
			bool flag2 = !flag;
			bool flag3 = flag2;
			if (flag3)
			{
				double totalHours = this.entity.World.Calendar.TotalHours;
				double hoursForNextStage = this.nearestFarmland.GetHoursForNextStage();
				double num = totalHours + hoursForNextStage + 1.0;
				this.nearestFarmland.TryGrowCrop(num);
				this.MarkFarmlandCultivated(this.nearestFarmland.Pos);
				SimpleParticleProperties simpleParticleProperties = new SimpleParticleProperties(10f, 15f, ColorUtil.ToRgba(255, 255, 233, 83), this.nearestFarmland.Position.AddCopy(-0.4, 0.8, -0.4), this.nearestFarmland.Position.AddCopy(-0.6, 0.8, -0.6), new Vec3f(-0.25f, 0f, -0.25f), new Vec3f(0.25f, 0f, 0.25f), 2f, 1f, 0.2f, 1f, EnumParticleModel.Cube);
				simpleParticleProperties.MinPos = this.nearestFarmland.Position.AddCopy(0.5, 1.0, 0.5);
				this.entity.World.SpawnParticles(simpleParticleProperties, null);
				this.entity.AnimManager.StopAnimation("hoe-till");
				this.entity.World.Logger.Debug("Cultivate: Performed cultivation on farmland at " + this.nearestFarmland.Pos.ToString());
			}
		}

		// Token: 0x0400016C RID: 364
		private BlockEntityFarmland nearestFarmland;

		// Token: 0x0400016D RID: 365
		private Dictionary<BlockPos, long> recentlyCultivatedFarmland;

		// Token: 0x0400016E RID: 366
		private long farmlandCooldownMs;
	}
}
