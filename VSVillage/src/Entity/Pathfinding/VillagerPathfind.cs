using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace VsVillage
{
	// Token: 0x0200002E RID: 46
	public class VillagerPathfind
	{
		// Token: 0x0600013C RID: 316 RVA: 0x0000B674 File Offset: 0x00009874
		public VillagerPathfind(ICoreServerAPI sapi)
		{
			ICachingBlockAccessor cachingBlockAccessor = sapi.World.GetCachingBlockAccessor(true, true);
			this.villagerAStar = new VillagerAStarNew(cachingBlockAccessor);
			this.waypointAStar = new WaypointAStar(cachingBlockAccessor);
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00002CDB File Offset: 0x00000EDB
		public BlockPos GetStartPos(Vec3d startPos)
		{
			return this.villagerAStar.GetStartPos(startPos);
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00002CE9 File Offset: 0x00000EE9
		public List<VillagerPathNode> FindPath(BlockPos start, BlockPos end, Village village)
		{
			return this.villagerAStar.FindPath(start, end, 5000);
		}

		// Token: 0x0600013F RID: 319 RVA: 0x0000B6B0 File Offset: 0x000098B0
		public List<Vec3d> FindPathAsWaypoints(BlockPos start, BlockPos end, Village village)
		{
			List<VillagerPathNode> list = this.FindPath(start, end, village);
			if (list != null)
			{
				return this.ToWaypoints(list);
			}
			return null;
		}

		// Token: 0x06000140 RID: 320 RVA: 0x0000B6D4 File Offset: 0x000098D4
		public List<Vec3d> ToWaypoints(List<VillagerPathNode> path)
		{
			List<Vec3d> list = new List<Vec3d>(path.Count + 1);
			for (int i = 1; i < path.Count; i++)
			{
				list.Add(path[i].ToWaypoint());
			}
			return list;
		}

		// Token: 0x040000AA RID: 170
		private VillagerAStarNew villagerAStar;

		// Token: 0x040000AB RID: 171
		private WaypointAStar waypointAStar;
	}
}
