using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VsVillage
{
	// Token: 0x02000012 RID: 18
	public class AiTaskHealWounded : AiTaskGotoAndInteract
	{
		// Token: 0x06000075 RID: 117 RVA: 0x00002679 File Offset: 0x00000879
		public AiTaskHealWounded(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
			: base(entity, taskConfig, aiConfig)
		{
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00005CAC File Offset: 0x00003EAC
		protected override Vec3d GetTargetPos()
		{
			bool flag = !this.IsHerbalist();
			bool flag2 = flag;
			bool flag3 = flag2;
			Vec3d vec3d;
			if (flag3)
			{
				vec3d = null;
			}
			else
			{
				Entity[] array = this.entity.World.GetEntitiesAround(this.entity.ServerPos.XYZ, base.maxDistance, 5f, (Entity entity) => entity is EntityVillager || entity is EntityTrader || entity is EntityPlayer);
				EntityBehaviorVillager behavior = this.entity.GetBehavior<EntityBehaviorVillager>();
				bool flag4 = ((behavior != null) ? behavior.Village : null) != null;
				bool flag5 = flag4;
				bool flag6 = flag5;
				if (flag6)
				{
					Entity[] array2 = (from villager in behavior.Village.Villagers
						where villager != null && villager.entity != null && villager.entity.Alive
						select villager.entity).ToArray<Entity>();
					array = array.Concat(array2).ToArray<Entity>();
				}
				int num = 0;
				float num2 = 0f;
				for (int i = 0; i < array.Length; i++)
				{
					EntityBehaviorHealth behavior2 = array[i].GetBehavior<EntityBehaviorHealth>();
					bool flag7 = behavior2 != null && num2 < behavior2.MaxHealth - behavior2.Health;
					bool flag8 = flag7;
					bool flag9 = flag8;
					if (flag9)
					{
						num2 = behavior2.MaxHealth - behavior2.Health;
						num = i;
					}
					bool flag10 = behavior2 != null && behavior2.Health <= 0f;
					bool flag11 = flag10;
					bool flag12 = flag11;
					if (flag12)
					{
						num2 = float.MaxValue;
						num = i;
					}
				}
				bool flag13 = num2 > 0.5f;
				bool flag14 = flag13;
				bool flag15 = flag14;
				if (flag15)
				{
					this.woundedEntity = array[num];
				}
				Entity entity2 = this.woundedEntity;
				bool flag16 = entity2 == null;
				bool flag17 = flag16;
				Vec3d vec3d2;
				if (flag17)
				{
					vec3d2 = null;
				}
				else
				{
					EntityPos serverPos = entity2.ServerPos;
					vec3d2 = ((serverPos != null) ? serverPos.XYZ : null);
				}
				vec3d = vec3d2;
			}
			return vec3d;
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00005ECC File Offset: 0x000040CC
		protected override bool InteractionPossible()
		{
			return this.IsHerbalist() && this.woundedEntity != null && this.entity.ServerPos.SquareDistanceTo(this.woundedEntity.ServerPos) < 9f;
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00005F14 File Offset: 0x00004114
		protected override void ApplyInteractionEffect()
		{
			bool flag = !this.IsHerbalist();
			bool flag2 = !flag;
			bool flag3 = flag2;
			if (flag3)
			{
				this.entity.AnimManager.StartAnimation(new AnimationMetaData
				{
					Animation = "holdbothhands",
					Code = "holdbothhands",
					AnimationSpeed = 1f,
					BlendMode = EnumAnimationBlendMode.Average
				}.Init());
				this.entity.World.RegisterCallback(delegate(float dt)
				{
					this.PerformHealing();
				}, 1000);
			}
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00005FA0 File Offset: 0x000041A0
		private bool IsHerbalist()
		{
			EntityAgent entity = this.entity;
			bool flag = entity == null;
			bool flag2 = flag;
			bool? flag3;
			if (flag2)
			{
				flag3 = null;
			}
			else
			{
				AssetLocation code = entity.Code;
				bool flag4 = code == null;
				bool flag5 = flag4;
				if (flag5)
				{
					flag3 = null;
				}
				else
				{
					string path = code.Path;
					flag3 = ((path != null) ? new bool?(path.EndsWith("-herbalist")) : null);
				}
			}
			bool? flag6 = flag3;
			return flag6.GetValueOrDefault();
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00006034 File Offset: 0x00004234
		private void PerformHealing()
		{
			bool flag = this.woundedEntity == null;
			bool flag2 = !flag;
			bool flag3 = flag2;
			if (flag3)
			{
				bool alive = this.woundedEntity.Alive;
				bool flag4 = alive;
				bool flag5 = flag4;
				if (flag5)
				{
					this.woundedEntity.ReceiveDamage(new DamageSource
					{
						DamageTier = 0,
						HitPosition = this.woundedEntity.ServerPos.XYZ,
						Source = EnumDamageSource.Internal,
						SourceEntity = this.entity,
						Type = EnumDamageType.Heal
					}, 100f);
				}
				else
				{
					this.woundedEntity.Revive();
				}
				Vec3d xyz = this.woundedEntity.ServerPos.XYZ;
				SimpleParticleProperties simpleParticleProperties = new SimpleParticleProperties(20f, 30f, ColorUtil.ToRgba(75, 146, 175, 222), xyz.AddCopy(-0.3, 0.5, -0.3), xyz.AddCopy(0.3, 2.0, 0.3), new Vec3f(-0.25f, 0f, -0.25f), new Vec3f(0.25f, 0.5f, 0.25f), 0.8f, -0.075f, 0.5f, 3f, EnumParticleModel.Quad);
				simpleParticleProperties.MinPos = xyz.AddCopy(-0.5, 0.0, -0.5);
				simpleParticleProperties.SelfPropelled = true;
				this.entity.World.SpawnParticles(simpleParticleProperties, null);
				SimpleParticleProperties simpleParticleProperties2 = new SimpleParticleProperties(15f, 20f, ColorUtil.ToRgba(255, 255, 255, 200), xyz.AddCopy(-0.2, 0.5, -0.2), xyz.AddCopy(0.2, 1.5, 0.2), new Vec3f(-0.1f, 0.1f, -0.1f), new Vec3f(0.1f, 0.3f, 0.1f), 0.5f, 0f, 0.2f, 0.8f, EnumParticleModel.Quad);
				simpleParticleProperties2.MinPos = xyz.AddCopy(-0.3, 0.5, -0.3);
				this.entity.World.SpawnParticles(simpleParticleProperties2, null);
				this.entity.AnimManager.StopAnimation("holdbothhands");
				this.woundedEntity = null;
			}
		}

		// Token: 0x0400003B RID: 59
		public Entity woundedEntity;
	}
}
