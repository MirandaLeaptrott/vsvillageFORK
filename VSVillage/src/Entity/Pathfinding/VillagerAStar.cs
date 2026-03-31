using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Essentials;

namespace VsVillage
{
	// Token: 0x02000027 RID: 39
	public class VillagerAStar
	{
		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600011B RID: 283 RVA: 0x00002C1F File Offset: 0x00000E1F
		// (set) Token: 0x0600011C RID: 284 RVA: 0x00002C27 File Offset: 0x00000E27
		public List<string> traversableCodes { get; set; } = new List<string> { "door", "gate", "ladder", "multiblock" };

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x0600011D RID: 285 RVA: 0x00002C30 File Offset: 0x00000E30
		// (set) Token: 0x0600011E RID: 286 RVA: 0x00002C38 File Offset: 0x00000E38
		public List<string> climbableCodes { get; set; } = new List<string> { "ladder" };

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600011F RID: 287 RVA: 0x00002C41 File Offset: 0x00000E41
		// (set) Token: 0x06000120 RID: 288 RVA: 0x00002C49 File Offset: 0x00000E49
		public List<string> steppableCodes { get; set; } = new List<string> { "stair", "path", "bed-", "farmland", "slab" };

		// Token: 0x06000121 RID: 289 RVA: 0x00009E58 File Offset: 0x00008058
		public VillagerAStar(ICoreAPI api)
		{
			this.api = api;
			this.blockAccess = api.World.GetCachingBlockAccessor(true, true);
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00009F2C File Offset: 0x0000812C
		public List<PathNode> FindPath(BlockPos start, BlockPos end, int maxFallHeight, float stepHeight, int searchDepth = 999, bool allowReachAlmost = true)
		{
			this.blockAccess.Begin();
			this.NodesChecked = 0;
			PathNode pathNode = new PathNode(start);
			PathNode pathNode2 = new PathNode(end);
			this.openSet.Clear();
			this.closedSet.Clear();
			this.openSet.Add(pathNode);
			while (this.openSet.Count > 0)
			{
				int nodesChecked = this.NodesChecked;
				this.NodesChecked = nodesChecked + 1;
				if (nodesChecked > searchDepth)
				{
					return null;
				}
				PathNode pathNode3 = this.openSet.RemoveNearest();
				this.closedSet.Add(pathNode3);
				if (pathNode3 == pathNode2 || (allowReachAlmost && Math.Abs(pathNode3.X - pathNode2.X) <= 1 && Math.Abs(pathNode3.Z - pathNode2.Z) <= 1 && Math.Abs(pathNode3.Y - pathNode2.Y) <= 2))
				{
					return this.retracePath(pathNode, pathNode3);
				}
				foreach (PathNode pathNode4 in this.findValidNeighbourNodes(pathNode3, pathNode2, stepHeight, maxFallHeight))
				{
					float num = 0f;
					PathNode pathNode5 = this.openSet.TryFindValue(pathNode4);
					if (pathNode5 != null)
					{
						float num2 = pathNode3.gCost + pathNode3.distanceTo(pathNode4);
						if (pathNode5.gCost > num2 + 0.0001f && this.traversable(pathNode4, pathNode2, stepHeight, maxFallHeight) && pathNode5.gCost > num2 + num + 0.0001f)
						{
							this.UpdateNode(pathNode3, pathNode5, num);
						}
					}
					else if (!this.closedSet.Contains(pathNode4) && this.traversable(pathNode4, pathNode2, stepHeight, maxFallHeight))
					{
						this.UpdateNode(pathNode3, pathNode4, num);
						pathNode4.hCost = pathNode4.distanceTo(pathNode2);
						this.openSet.Add(pathNode4);
					}
				}
			}
			return null;
		}

		// Token: 0x06000123 RID: 291 RVA: 0x0000A114 File Offset: 0x00008314
		protected virtual IEnumerable<PathNode> findValidNeighbourNodes(PathNode nearestNode, PathNode targetNode, float stepHeight, int maxFallHeight)
		{
			Block current = this.blockAccess.GetBlock(new BlockPos(nearestNode.X, nearestNode.Y, nearestNode.Z, 0));
			if (this.climbableCodes.Exists((string code) => current.Code.Path.Contains(code)))
			{
				string text = current.Variant["side"];
				Cardinal cardinal;
				List<PathNode> list;
				if (!(text == "east"))
				{
					if (!(text == "west"))
					{
						if (!(text == "south"))
						{
							cardinal = Cardinal.North;
							list = new List<PathNode>(new PathNode[]
							{
								new PathNode(nearestNode, Cardinal.East),
								new PathNode(nearestNode, Cardinal.South),
								new PathNode(nearestNode, Cardinal.West)
							});
						}
						else
						{
							cardinal = Cardinal.South;
							list = new List<PathNode>(new PathNode[]
							{
								new PathNode(nearestNode, Cardinal.North),
								new PathNode(nearestNode, Cardinal.East),
								new PathNode(nearestNode, Cardinal.West)
							});
						}
					}
					else
					{
						cardinal = Cardinal.West;
						list = new List<PathNode>(new PathNode[]
						{
							new PathNode(nearestNode, Cardinal.North),
							new PathNode(nearestNode, Cardinal.East),
							new PathNode(nearestNode, Cardinal.South)
						});
					}
				}
				else
				{
					cardinal = Cardinal.East;
					list = new List<PathNode>(new PathNode[]
					{
						new PathNode(nearestNode, Cardinal.North),
						new PathNode(nearestNode, Cardinal.South),
						new PathNode(nearestNode, Cardinal.West)
					});
				}
				int i = 1;
				while (this.traversableCodes.Exists((string code) => this.blockAccess.GetBlock(new BlockPos(nearestNode.X, nearestNode.Y + i, nearestNode.Z, 0)).Code.Path.Contains(code)))
				{
					int j = i;
					i = j + 1;
				}
				PathNode pathNode = new PathNode(nearestNode, cardinal);
				pathNode.Y += i;
				list.Add(pathNode);
				return list;
			}
			return new PathNode[]
			{
				new PathNode(nearestNode, Cardinal.North),
				new PathNode(nearestNode, Cardinal.East),
				new PathNode(nearestNode, Cardinal.South),
				new PathNode(nearestNode, Cardinal.West)
			};
		}

		// Token: 0x06000124 RID: 292 RVA: 0x00002C52 File Offset: 0x00000E52
		private void UpdateNode(PathNode nearestNode, PathNode neighbourNode, float extraCost)
		{
			neighbourNode.gCost = nearestNode.gCost + nearestNode.distanceTo(neighbourNode) + extraCost;
			neighbourNode.Parent = nearestNode;
			neighbourNode.pathLength = nearestNode.pathLength + 1;
		}

		// Token: 0x06000125 RID: 293 RVA: 0x0000A410 File Offset: 0x00008610
		protected virtual bool traversable(PathNode node, PathNode target, float stepHeight, int maxFallHeight)
		{
			if (target.X == node.X && target.Z == node.Z && target.Y == node.Y)
			{
				return true;
			}
			if (this.traversable(this.blockAccess.GetBlock(new BlockPos(node.X, node.Y, node.Z, 0))) && this.traversable(this.blockAccess.GetBlock(new BlockPos(node.X, node.Y + 1, node.Z, 0))))
			{
				while (0 <= maxFallHeight)
				{
					Block block = this.blockAccess.GetBlock(new BlockPos(node.X, node.Y - 1, node.Z, 0));
					if (this.canStep(block))
					{
						return true;
					}
					if (!this.traversable(block))
					{
						return false;
					}
					node.Y--;
					maxFallHeight--;
				}
				while (this.climbableCodes.Exists((string code) => this.blockAccess.GetBlock(new BlockPos(node.X, node.Y, node.Z, 0)).Code.Path.Contains(code)))
				{
					Block block2 = this.blockAccess.GetBlock(new BlockPos(node.X, node.Y - 1, node.Z, 0));
					if (this.canStep(block2))
					{
						return true;
					}
					node.Y--;
				}
			}
			else
			{
				while (1f < stepHeight)
				{
					node.Y++;
					if (this.canStep(this.blockAccess.GetBlock(new BlockPos(node.X, node.Y - 1, node.Z, 0))) && this.traversable(this.blockAccess.GetBlock(new BlockPos(node.X, node.Y, node.Z, 0))) && this.traversable(this.blockAccess.GetBlock(new BlockPos(node.X, node.Y + 1, node.Z, 0))))
					{
						return true;
					}
					stepHeight -= 1f;
				}
			}
			return false;
		}

		// Token: 0x06000126 RID: 294 RVA: 0x0000A6A4 File Offset: 0x000088A4
		public BlockPos GetStartPos(Vec3d startPos)
		{
			BlockPos asBlockPos = startPos.AsBlockPos;
			Block block = this.blockAccess.GetBlock(asBlockPos);
			if (this.traversable(block))
			{
				return asBlockPos;
			}
			if (this.getDecimalPart(startPos.Z) < 0.5 && this.traversable(this.blockAccess.GetBlock(asBlockPos.NorthCopy(1))))
			{
				return asBlockPos.NorthCopy(1);
			}
			if (this.getDecimalPart(startPos.Z) > 0.5 && this.traversable(this.blockAccess.GetBlock(asBlockPos.SouthCopy(1))))
			{
				return asBlockPos.SouthCopy(1);
			}
			if (this.getDecimalPart(startPos.X) < 0.5 && this.traversable(this.blockAccess.GetBlock(asBlockPos.West())))
			{
				return asBlockPos;
			}
			if (this.getDecimalPart(startPos.X) > 0.5 && this.traversable(this.blockAccess.GetBlock(asBlockPos.East())))
			{
				return asBlockPos;
			}
			if (this.getDecimalPart(startPos.Z) < 0.5 && this.traversable(this.blockAccess.GetBlock(asBlockPos.NorthCopy(1))))
			{
				return asBlockPos.NorthCopy(1);
			}
			if (this.getDecimalPart(startPos.Z) > 0.5 && this.traversable(this.blockAccess.GetBlock(asBlockPos.SouthCopy(1))))
			{
				return asBlockPos.SouthCopy(1);
			}
			return startPos.AsBlockPos;
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00002C7F File Offset: 0x00000E7F
		private double getDecimalPart(double number)
		{
			return number - Math.Truncate(number);
		}

		// Token: 0x06000128 RID: 296 RVA: 0x0000A820 File Offset: 0x00008A20
		protected virtual bool canStep(Block belowBlock)
		{
			return belowBlock.SideSolid[BlockFacing.UP.Index] || this.steppableCodes.Exists((string code) => belowBlock.Code.Path.Contains(code));
		}

		// Token: 0x06000129 RID: 297 RVA: 0x0000A870 File Offset: 0x00008A70
		protected virtual bool traversable(Block block)
		{
			return block.CollisionBoxes == null || block.CollisionBoxes.Length == 0 || this.traversableCodes.Exists((string code) => block.Code.Path.Contains(code));
		}

		// Token: 0x0600012A RID: 298 RVA: 0x0000A8C0 File Offset: 0x00008AC0
		private List<PathNode> retracePath(PathNode startNode, PathNode endNode)
		{
			int pathLength = endNode.pathLength;
			List<PathNode> list = new List<PathNode>(pathLength + 1);
			for (int i = 0; i < pathLength + 1; i++)
			{
				list.Add(null);
			}
			PathNode pathNode = endNode;
			for (int j = pathLength; j >= 0; j--)
			{
				list[j] = pathNode;
				pathNode = pathNode.Parent;
			}
			return list;
		}

		// Token: 0x0400008A RID: 138
		protected ICoreAPI api;

		// Token: 0x0400008B RID: 139
		protected ICachingBlockAccessor blockAccess;

		// Token: 0x0400008F RID: 143
		public int NodesChecked;

		// Token: 0x04000090 RID: 144
		public const double centerOffsetX = 0.5;

		// Token: 0x04000091 RID: 145
		public const double centerOffsetZ = 0.5;

		// Token: 0x04000092 RID: 146
		public PathNodeSet openSet = new PathNodeSet();

		// Token: 0x04000093 RID: 147
		public HashSet<PathNode> closedSet = new HashSet<PathNode>();
	}
}
