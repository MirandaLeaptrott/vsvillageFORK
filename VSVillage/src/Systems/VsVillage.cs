using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace VsVillage
{
	// Token: 0x02000051 RID: 81
	public class VsVillage : ModSystem
	{
		// Token: 0x060001FB RID: 507 RVA: 0x0001006C File Offset: 0x0000E26C
		public override void Start(ICoreAPI api)
		{
			base.Start(api);
			api.RegisterEntityBehaviorClass("Villager", typeof(EntityBehaviorVillager));
			api.RegisterEntityBehaviorClass("replacewithentity", typeof(EntityBehaviorReplaceWithEntity));
			api.RegisterItemClass("ItemVillagerGear", typeof(ItemVillagerGear));
			api.RegisterItemClass("ItemVillagerHorn", typeof(ItemVillagerHorn));
			api.RegisterBlockEntityClass("VillagerBed", typeof(BlockEntityVillagerBed));
			api.RegisterBlockEntityClass("VillagerWorkstation", typeof(BlockEntityVillagerWorkstation));
			api.RegisterBlockEntityClass("VillagerWaypoint", typeof(BlockEntityVillagerWaypoint));
			api.RegisterBlockEntityClass("VillagerBrazier", typeof(BlockEntityVillagerBrazier));
			api.RegisterBlockClass("MayorWorkstation", typeof(BlockMayorWorkstation));
			AiTaskRegistry.Register<AiTaskVillagerMeleeAttack>("villagermeleeattack");
			AiTaskRegistry.Register<AiTaskVillagerSeekEntity>("villagerseekentity");
			AiTaskRegistry.Register<AiTaskVillagerSleep>("villagersleep");
			AiTaskRegistry.Register<AiTaskVillagerSocialize>("villagersocialize");
			AiTaskRegistry.Register<AiTaskVillagerGotoWork>("villagergotowork");
			AiTaskRegistry.Register<AiTaskVillagerGotoGatherspot>("villagergotogather");
			AiTaskRegistry.Register<AiTaskVillagerFillTrough>("villagerfilltrough");
			AiTaskRegistry.Register<AiTaskVillagerCultivateCrops>("villagercultivatecrops");
			AiTaskRegistry.Register<AiTaskVillagerFlipWeapon>("villagerflipweapon");
			AiTaskRegistry.Register<AiTaskStayCloseToEmployer>("villagerstayclose");
			AiTaskRegistry.Register<AiTaskHealWounded>("villagerhealwounded");
			AiTaskRegistry.Register<AiTaskVillagerRangedAttack>("villagerrangedattack");
			AiTaskRegistry.Register<AiTaskVillagerWander>("villagerwander");
			ActivityModSystem.ActionTypes.TryAdd("GotoPointOfInterest", typeof(GotoPointOfInterestAction));
			ActivityModSystem.ActionTypes.TryAdd("Sleep", typeof(SleepAction));
			ActivityModSystem.ActionTypes.TryAdd("ToggleBrazierFire", typeof(ToggleBrazierFireAction));
			ActivityModSystem.ConditionTypes.TryAdd("CloseToPointOfInterest", typeof(CloseToPointOfInterestCondition));
			ActivityModSystem.ConditionTypes.TryAdd("CooldownCondition", typeof(CooldownCondition));
			ActivityModSystem.ActionTypes.TryAdd("CultivateCrops", typeof(CultivateCropsAction));
			ActivityModSystem.ActionTypes.TryAdd("FillTrough", typeof(FillTroughAction));
		}
	}
}
