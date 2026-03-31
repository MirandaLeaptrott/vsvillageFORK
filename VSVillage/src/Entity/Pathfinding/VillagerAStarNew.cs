using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VsVillage
{
	// Token: 0x0200002D RID: 45
	public class VillagerAStarNew
	{
		// Token: 0x06000135 RID: 309 RVA: 0x0000A9DC File Offset: 0x00008BDC
		public VillagerAStarNew(ICachingBlockAccessor blockAccessor)
		{
			this.blockAccessor = blockAccessor;
			this.collTester = new CollisionTester();
			this.traversableCodes = new List<string> { "door", "gate", "ladder", "multiblock" };
			this.doorCodes = new List<string> { "door", "gate", "multiblock" };
			this.climbableCodes = new List<string> { "ladder" };
			this.steppableCodes = new List<string> { "stair", "path", "bed-", "farmland", "furrowedland", "slab", "carpet" };
			this.tmpVec = new Vec3d();
			this.tmpPos = new BlockPos();
		}

		// Token: 0x06000136 RID: 310 RVA: 0x0000AB40 File Offset: 0x00008D40
		public List<VillagerPathNode> FindPath(BlockPos start, BlockPos end, int searchDepth = 1000)
		{
			bool flag = start == null || end == null;
			bool flag2 = flag;
			bool flag3 = flag2;
			List<VillagerPathNode> list;
			if (flag3)
			{
				list = null;
			}
			else
			{
				this.centerOffsetX = 0.3 + new Random().NextDouble() * 0.4;
				this.centerOffsetZ = 0.3 + new Random().NextDouble() * 0.4;
				SortedSet<VillagerPathNode> sortedSet = new SortedSet<VillagerPathNode>();
				HashSet<VillagerPathNode> hashSet = new HashSet<VillagerPathNode>();
				VillagerPathNode villagerPathNode = new VillagerPathNode(start, end, this.isDoor(this.blockAccessor.GetBlock(start)));
				sortedSet.Add(villagerPathNode);
				int num = 0;
				while (num < searchDepth && sortedSet.Count > 0)
				{
					num++;
					VillagerPathNode min = sortedSet.Min;
					sortedSet.Remove(min);
					hashSet.Add(min);
					bool flag4 = min.BlockPos.X == end.X && min.BlockPos.Z == end.Z && Math.Abs(min.BlockPos.Y - end.Y) <= 1;
					bool flag5 = flag4;
					bool flag6 = flag5;
					bool flag7 = flag6;
					if (flag7)
					{
						return min.RetracePath();
					}
					foreach (Cardinal cardinal in Cardinal.ALL)
					{
						VillagerPathNode villagerPathNode2 = new VillagerPathNode(min, cardinal);
						bool flag8 = hashSet.Contains(villagerPathNode2);
						bool flag9 = !flag8;
						bool flag10 = flag9;
						if (flag10)
						{
							float num2 = 0f;
							bool flag11 = !this.traversable(villagerPathNode2, end, cardinal, ref num2);
							bool flag12 = !flag11;
							bool flag13 = flag12;
							if (flag13)
							{
								villagerPathNode2.SetGCostFromParent(min, num2);
								villagerPathNode2.Init(end, this.isDoor(this.blockAccessor.GetBlock(villagerPathNode2.BlockPos)));
								VillagerPathNode villagerPathNode3 = null;
								foreach (VillagerPathNode villagerPathNode4 in sortedSet)
								{
									bool flag14 = villagerPathNode4.BlockPos.Equals(villagerPathNode2.BlockPos);
									bool flag15 = flag14;
									bool flag16 = flag15;
									if (flag16)
									{
										villagerPathNode3 = villagerPathNode4;
										break;
									}
								}
								bool flag17 = villagerPathNode3 != null;
								bool flag18 = flag17;
								bool flag19 = flag18;
								if (flag19)
								{
									bool flag20 = villagerPathNode2.gCost < villagerPathNode3.gCost - 0.0001f;
									bool flag21 = flag20;
									bool flag22 = flag21;
									if (flag22)
									{
										sortedSet.Remove(villagerPathNode3);
										sortedSet.Add(villagerPathNode2);
									}
								}
								else
								{
									sortedSet.Add(villagerPathNode2);
								}
							}
						}
					}
				}
				list = null;
			}
			return list;
		}

		// Token: 0x06000137 RID: 311 RVA: 0x0000AE24 File Offset: 0x00009024
		private bool isDoor(Block block)
		{
			bool flag = block == null || block.Code == null;
			bool flag2 = flag;
			bool flag3 = flag2;
			bool flag4;
			if (flag3)
			{
				flag4 = false;
			}
			else
			{
				foreach (string text in this.doorCodes)
				{
					bool flag5 = block.Code.Path.Contains(text);
					bool flag6 = flag5;
					bool flag7 = flag6;
					if (flag7)
					{
						return true;
					}
				}
				flag4 = false;
			}
			return flag4;
		}

		// Token: 0x06000138 RID: 312 RVA: 0x0000AEC8 File Offset: 0x000090C8
		public BlockPos GetStartPos(Vec3d startPos)
		{
			BlockPos asBlockPos = startPos.AsBlockPos;
			this.tmpVec.Set(startPos.X, startPos.Y, startPos.Z);
			bool flag = !this.collTester.IsColliding(this.blockAccessor, this.entityCollBox, this.tmpVec, false);
			bool flag2 = flag;
			bool flag3 = flag2;
			BlockPos blockPos;
			if (flag3)
			{
				blockPos = asBlockPos;
			}
			else
			{
				double num = startPos.X - Math.Truncate(startPos.X);
				double num2 = startPos.Z - Math.Truncate(startPos.Z);
				BlockPos[] array = new BlockPos[]
				{
					asBlockPos.NorthCopy(1),
					asBlockPos.SouthCopy(1),
					asBlockPos.WestCopy(1),
					asBlockPos.EastCopy(1)
				};
				foreach (BlockPos blockPos2 in array)
				{
					this.tmpVec.Set((double)blockPos2.X + 0.5, (double)blockPos2.Y, (double)blockPos2.Z + 0.5);
					bool flag4 = !this.collTester.IsColliding(this.blockAccessor, this.entityCollBox, this.tmpVec, false);
					bool flag5 = flag4;
					bool flag6 = flag5;
					if (flag6)
					{
						return blockPos2;
					}
				}
				blockPos = startPos.AsBlockPos;
			}
			return blockPos;
		}

		// Token: 0x06000139 RID: 313 RVA: 0x0000B02C File Offset: 0x0000922C
		protected virtual bool traversable(VillagerPathNode node, BlockPos target, Cardinal fromDir, ref float extraCost)
		{
			bool flag = node.BlockPos.X == target.X && node.BlockPos.Z == target.Z;
			bool flag2 = Math.Abs(node.BlockPos.Y - target.Y) <= 1;
			bool flag3 = flag && flag2;
			bool flag4 = flag3;
			bool flag5 = flag4;
			bool flag6;
			if (flag5)
			{
				flag6 = true;
			}
			else
			{
				this.tmpVec.Set((double)node.BlockPos.X + this.centerOffsetX, (double)node.BlockPos.Y, (double)node.BlockPos.Z + this.centerOffsetZ);
				this.tmpPos.Set(node.BlockPos.X, node.BlockPos.Y, node.BlockPos.Z);
				bool flag7 = !this.collTester.IsColliding(this.blockAccessor, this.entityCollBox, this.tmpVec, false);
				bool flag8 = flag7;
				bool flag9 = flag8;
				if (flag9)
				{
					int i = 0;
					while (i <= 5)
					{
						this.tmpPos.Set(node.BlockPos.X, node.BlockPos.Y - 1, node.BlockPos.Z);
						Block block = this.blockAccessor.GetBlock(this.tmpPos);
						bool flag10 = !block.CanStep;
						bool flag11 = flag10;
						bool flag12 = flag11;
						if (flag12)
						{
							return false;
						}
						float traversalCost = block.GetTraversalCost(this.tmpPos, EnumAICreatureType.Humanoid);
						bool flag13 = traversalCost > 10000f;
						bool flag14 = flag13;
						bool flag15 = flag14;
						if (flag15)
						{
							return false;
						}
						Cuboidf[] collisionBoxes = block.GetCollisionBoxes(this.blockAccessor, this.tmpPos);
						bool flag16 = collisionBoxes != null && collisionBoxes.Length != 0;
						bool flag17 = flag16;
						bool flag18 = flag17;
						if (flag18)
						{
							extraCost += traversalCost;
							bool isDiagnoal = fromDir.IsDiagnoal;
							bool flag19 = isDiagnoal;
							bool flag20 = flag19;
							if (flag20)
							{
								this.tmpVec.Add((double)(-(double)((float)fromDir.Normali.X) / 2f), 0.0, (double)(-(double)((float)fromDir.Normali.Z) / 2f));
								bool flag21 = this.collTester.IsColliding(this.blockAccessor, this.entityCollBox, this.tmpVec, false);
								bool flag22 = flag21;
								bool flag23 = flag22;
								if (flag23)
								{
									return false;
								}
							}
							this.tmpPos.Set(node.BlockPos.X, node.BlockPos.Y, node.BlockPos.Z);
							Block block2 = this.blockAccessor.GetBlock(this.tmpPos, 2);
							float traversalCost2 = block2.GetTraversalCost(this.tmpPos, EnumAICreatureType.Humanoid);
							bool flag24 = traversalCost2 > 10000f;
							bool flag25 = flag24;
							bool flag26 = flag25;
							if (flag26)
							{
								return false;
							}
							extraCost += traversalCost2;
							return true;
						}
						else
						{
							this.tmpVec.Y -= 1.0;
							bool flag27 = this.collTester.IsColliding(this.blockAccessor, this.entityCollBox, this.tmpVec, false);
							bool flag28 = flag27;
							bool flag29 = flag28;
							if (flag29)
							{
								return false;
							}
							i++;
							node.BlockPos.Y--;
						}
					}
					flag6 = false;
				}
				else
				{
					this.tmpPos.Set(node.BlockPos.X, node.BlockPos.Y, node.BlockPos.Z);
					Block block3 = this.blockAccessor.GetBlock(this.tmpPos);
					bool flag30 = !block3.CanStep;
					bool flag31 = flag30;
					bool flag32 = flag31;
					if (flag32)
					{
						flag6 = false;
					}
					else
					{
						float traversalCost3 = block3.GetTraversalCost(this.tmpPos, EnumAICreatureType.Humanoid);
						bool flag33 = traversalCost3 > 10000f;
						bool flag34 = flag33;
						bool flag35 = flag34;
						if (flag35)
						{
							flag6 = false;
						}
						else
						{
							extraCost += traversalCost3;
							float num = 0f;
							Cuboidf[] collisionBoxes2 = block3.GetCollisionBoxes(this.blockAccessor, this.tmpPos);
							bool flag36 = collisionBoxes2 != null && collisionBoxes2.Length != 0;
							bool flag37 = flag36;
							bool flag38 = flag37;
							if (flag38)
							{
								foreach (Cuboidf cuboidf in collisionBoxes2)
								{
									bool flag39 = cuboidf.Y2 > num;
									bool flag40 = flag39;
									bool flag41 = flag40;
									if (flag41)
									{
										num = cuboidf.Y2;
									}
								}
							}
							this.tmpVec.Set((double)node.BlockPos.X + this.centerOffsetX, (double)node.BlockPos.Y + 1.2000000476837158 + (double)num - 1.0, (double)node.BlockPos.Z + this.centerOffsetZ);
							bool flag42 = !this.collTester.IsColliding(this.blockAccessor, this.entityCollBox, this.tmpVec, false);
							bool flag43 = flag42;
							bool flag44 = flag43;
							if (flag44)
							{
								bool flag45 = fromDir.IsDiagnoal && collisionBoxes2 != null && collisionBoxes2.Length != 0;
								bool flag46 = flag45;
								bool flag47 = flag46;
								if (flag47)
								{
									this.tmpVec.Add((double)(-(double)((float)fromDir.Normali.X) / 2f), 0.0, (double)(-(double)((float)fromDir.Normali.Z) / 2f));
									bool flag48 = this.collTester.IsColliding(this.blockAccessor, this.entityCollBox, this.tmpVec, false);
									bool flag49 = flag48;
									bool flag50 = flag49;
									if (flag50)
									{
										return false;
									}
								}
								node.BlockPos.Y += (int)(1f + num - 1f);
								flag6 = true;
							}
							else
							{
								flag6 = false;
							}
						}
					}
				}
			}
			return flag6;
		}

		// Token: 0x0600013A RID: 314 RVA: 0x00002CD1 File Offset: 0x00000ED1
		public void SetEntityCollisionBox(Cuboidf collBox)
		{
			this.entityCollBox = collBox;
		}

		// Token: 0x0600013B RID: 315 RVA: 0x0000B614 File Offset: 0x00009814
		public void SetEntityCollisionBox(EntityAgent entity)
		{
			bool flag = entity != null && entity.CollisionBox != null;
			bool flag2 = flag;
			bool flag3 = flag2;
			if (flag3)
			{
				this.entityCollBox = entity.CollisionBox;
			}
			else
			{
				this.entityCollBox = new Cuboidf(-0.4f, 0f, -0.4f, 0.4f, 1.8f, 0.4f);
			}
		}

		// Token: 0x0400009D RID: 157
		public List<string> doorCodes;

		// Token: 0x0400009E RID: 158
		public List<string> climbableCodes;

		// Token: 0x0400009F RID: 159
		public List<string> steppableCodes;

		// Token: 0x040000A0 RID: 160
		public ICachingBlockAccessor blockAccessor;

		// Token: 0x040000A1 RID: 161
		public const float stepHeight = 1.2f;

		// Token: 0x040000A2 RID: 162
		public const int maxFallHeight = 5;

		// Token: 0x040000A3 RID: 163
		protected CollisionTester collTester;

		// Token: 0x040000A4 RID: 164
		protected Cuboidf entityCollBox = new Cuboidf(-0.4f, 0f, -0.4f, 0.4f, 1.8f, 0.4f);

		// Token: 0x040000A5 RID: 165
		protected double centerOffsetX = 0.5;

		// Token: 0x040000A6 RID: 166
		protected double centerOffsetZ = 0.5;

		// Token: 0x040000A7 RID: 167
		protected Vec3d tmpVec;

		// Token: 0x040000A8 RID: 168
		protected BlockPos tmpPos;

		// Token: 0x040000A9 RID: 169
		public List<string> traversableCodes;
	}
}
