using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VsVillage
{
	// Token: 0x02000061 RID: 97
	public class AiTaskVillagerFillTrough : AiTaskGotoAndInteract
	{
		// Token: 0x0600024B RID: 587 RVA: 0x000124A0 File Offset: 0x000106A0
		public AiTaskVillagerFillTrough(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
			: base(entity, taskConfig, aiConfig)
		{
			this.recentlyFilledTroughs = new Dictionary<BlockPos, long>();
			bool flag = taskConfig["troughCooldownSeconds"] != null;
			bool flag2 = flag;
			if (flag2)
			{
				this.troughCooldownMs = (long)(taskConfig["troughCooldownSeconds"].AsInt(60) * 1000);
			}
		}

		// Token: 0x0600024C RID: 588 RVA: 0x00012504 File Offset: 0x00010704
		protected override Vec3d GetTargetPos()
		{
			bool flag = !this.IsShepherd();
			bool flag2 = flag;
			Vec3d vec3d;
			if (flag2)
			{
				vec3d = null;
			}
			else
			{
				this.nearestTrough = this.entity.Api.ModLoader.GetModSystem<POIRegistry>(true).GetNearestPoi(this.entity.ServerPos.XYZ, base.maxDistance, new PoiMatcher(this.isValidTrough)) as BlockEntityTrough;
				bool flag3 = this.nearestTrough == null;
				bool flag4 = flag3;
				if (flag4)
				{
					vec3d = null;
				}
				else
				{
					this.entity.World.Logger.Notification("Shepherd found trough at: " + this.nearestTrough.Position.ToString());
					vec3d = this.nearestTrough.Position;
				}
			}
			return vec3d;
		}

		// Token: 0x0600024D RID: 589 RVA: 0x000125D0 File Offset: 0x000107D0
		protected override void ApplyInteractionEffect()
		{
			bool flag = !this.IsShepherd();
			bool flag2 = !flag;
			if (flag2)
			{
				bool flag3 = this.nearestTrough == null;
				bool flag4 = !flag3;
				if (flag4)
				{
					this.entity.World.Logger.Notification("Shepherd found trough at: " + this.nearestTrough.Position.ToString());
					Item item = (this.nearestTrough.Inventory[0].Empty ? this.entity.World.GetItem(new AssetLocation("grain-flax")) : this.nearestTrough.Inventory[0].Itemstack.Item);
					this.entity.World.Logger.Notification("Item to fill: " + ((item != null) ? item.Code.ToString() : "NULL"));
					bool flag5 = item == null;
					bool flag6 = !flag5;
					if (flag6)
					{
						ItemSlot itemSlot = new DummySlot(new ItemStack(item, 16));
						ContentConfig contentConfig = ItemSlotTrough.getContentConfig(this.entity.Api.World, this.nearestTrough.contentConfigs, itemSlot);
						this.entity.World.Logger.Notification("ContentConfig: " + ((contentConfig != null) ? "Valid" : "NULL"));
						bool flag7 = contentConfig == null;
						bool flag8 = !flag7;
						if (flag8)
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
								this.PerformFilling(itemSlot, contentConfig);
							}, 1500);
						}
					}
				}
			}
		}

		// Token: 0x0600024E RID: 590 RVA: 0x00003831 File Offset: 0x00001A31
		public override void FinishExecute(bool cancelled)
		{
			this.entity.AnimManager.StopAnimation("hoe-till");
			base.FinishExecute(cancelled);
		}

		// Token: 0x0600024F RID: 591 RVA: 0x000127E4 File Offset: 0x000109E4
		private bool IsShepherd()
		{
			return this.entity != null && this.entity.Code != null && this.entity.Code.Path != null && this.entity.Code.Path.EndsWith("-shepherd");
		}

		// Token: 0x06000250 RID: 592 RVA: 0x00012840 File Offset: 0x00010A40
		private bool IsTroughOnCooldown(BlockPos pos)
		{
			long elapsedMilliseconds = this.entity.World.ElapsedMilliseconds;
			List<BlockPos> list = new List<BlockPos>();
			foreach (KeyValuePair<BlockPos, long> keyValuePair in this.recentlyFilledTroughs)
			{
				bool flag = elapsedMilliseconds - keyValuePair.Value > this.troughCooldownMs;
				bool flag2 = flag;
				if (flag2)
				{
					list.Add(keyValuePair.Key);
				}
			}
			for (int i = 0; i < list.Count; i++)
			{
				this.recentlyFilledTroughs.Remove(list[i]);
			}
			long num;
			return this.recentlyFilledTroughs.TryGetValue(pos, out num) && elapsedMilliseconds - num < this.troughCooldownMs;
		}

		// Token: 0x06000251 RID: 593 RVA: 0x00003881 File Offset: 0x00001A81
		private void MarkTroughFilled(BlockPos pos)
		{
			this.recentlyFilledTroughs[pos.Copy()] = this.entity.World.ElapsedMilliseconds;
		}

		// Token: 0x06000252 RID: 594 RVA: 0x0001292C File Offset: 0x00010B2C
		private void PerformFilling(ItemSlot itemSlot, ContentConfig contentConfig)
		{
			bool flag = this.nearestTrough == null;
			bool flag2 = !flag;
			if (flag2)
			{
				int num = itemSlot.TryPutInto(this.entity.World, this.nearestTrough.Inventory[0], contentConfig.QuantityPerFillLevel);
				this.entity.World.Logger.Notification("Amount moved to trough: " + num.ToString());
				this.nearestTrough.Inventory[0].MarkDirty();
				this.MarkTroughFilled(this.nearestTrough.Pos);
				SimpleParticleProperties simpleParticleProperties = new SimpleParticleProperties(10f, 15f, ColorUtil.ToRgba(255, 255, 233, 83), this.nearestTrough.Position.AddCopy(-0.4, 0.8, -0.4), this.nearestTrough.Position.AddCopy(-0.6, 0.8, -0.6), new Vec3f(-0.25f, 0f, -0.25f), new Vec3f(0.25f, 0f, 0.25f), 2f, 1f, 0.2f, 1f, EnumParticleModel.Cube);
				simpleParticleProperties.MinPos = this.nearestTrough.Position.AddCopy(0.5, 1.0, 0.5);
				this.entity.World.SpawnParticles(simpleParticleProperties, null);
				this.entity.AnimManager.StopAnimation("hoe-till");
			}
		}

		// Token: 0x06000253 RID: 595 RVA: 0x00012AE0 File Offset: 0x00010CE0
		private bool isValidTrough(IPointOfInterest poi)
		{
			BlockEntityTrough blockEntityTrough = poi as BlockEntityTrough;
			bool flag = blockEntityTrough == null || this.IsTroughOnCooldown(blockEntityTrough.Pos);
			bool flag2 = flag;
			bool flag3;
			if (flag2)
			{
				flag3 = false;
			}
			else
			{
				ItemSlot itemSlot = blockEntityTrough.Inventory[0];
				bool flag4 = itemSlot == null || itemSlot.Empty;
				bool flag5 = flag4;
				if (flag5)
				{
					flag3 = this.entity.World.Rand.NextDouble() < 0.3;
				}
				else
				{
					int stackSize = itemSlot.StackSize;
					int maxStackSize = itemSlot.Itemstack.Collectible.MaxStackSize;
					flag3 = stackSize < maxStackSize && this.entity.World.Rand.NextDouble() < 0.3;
				}
			}
			return flag3;
		}

		// Token: 0x0400016F RID: 367
		private BlockEntityTrough nearestTrough;

		// Token: 0x04000170 RID: 368
		private Dictionary<BlockPos, long> recentlyFilledTroughs;

		// Token: 0x04000171 RID: 369
		private long troughCooldownMs = 60000L;
	}
}
