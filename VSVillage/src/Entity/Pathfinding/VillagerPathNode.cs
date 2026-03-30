using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using Vintagestory.Essentials;

namespace VsVillage
{
	// Token: 0x0200002F RID: 47
	public class VillagerPathNode : IEquatable<VillagerPathNode>, IComparable<VillagerPathNode>
	{
		// Token: 0x06000141 RID: 321 RVA: 0x0000B714 File Offset: 0x00009914
		public PathNode ToPathNode()
		{
			return new PathNode(this.BlockPos);
		}

		// Token: 0x06000142 RID: 322 RVA: 0x0000B734 File Offset: 0x00009934
		public Vec3d ToWaypoint()
		{
			return new Vec3d((double)this.BlockPos.X + 0.5, (double)this.BlockPos.Y, (double)this.BlockPos.Z + 0.5);
		}

		// Token: 0x06000143 RID: 323 RVA: 0x0000B784 File Offset: 0x00009984
		public VillagerPathNode(BlockPos blockPos, BlockPos target, bool isDoor)
		{
			this.BlockPos = blockPos;
			this.IsDoor = isDoor;
			this.gCost = 0f;
			this.hCost = (float)Math.Sqrt((double)blockPos.DistanceSqTo((double)target.X, (double)target.Y, (double)target.Z));
			this.pathLength = 0;
			this.Parent = null;
		}

		// Token: 0x06000144 RID: 324 RVA: 0x0000B7E8 File Offset: 0x000099E8
		public VillagerPathNode(VillagerPathNode parent, Cardinal cardinal)
		{
			this.Parent = parent;
			this.BlockPos = parent.BlockPos.AddCopy(cardinal.Normali.X, cardinal.Normali.Y, cardinal.Normali.Z);
			this.pathLength = parent.pathLength + 1;
			this.gCost = 0f;
			this.hCost = 0f;
			this.IsDoor = false;
		}

		// Token: 0x06000145 RID: 325 RVA: 0x00002CFD File Offset: 0x00000EFD
		public void Init(BlockPos target, bool isDoor)
		{
			this.hCost = (float)Math.Sqrt((double)this.BlockPos.DistanceSqTo((double)target.X, (double)target.Y, (double)target.Z));
			this.IsDoor = isDoor;
		}

		// Token: 0x06000146 RID: 326 RVA: 0x0000B864 File Offset: 0x00009A64
		public bool Equals(VillagerPathNode other)
		{
			return this.BlockPos.Equals((other != null) ? other.BlockPos : null);
		}

		// Token: 0x06000147 RID: 327 RVA: 0x0000B890 File Offset: 0x00009A90
		public override bool Equals(object obj)
		{
			VillagerPathNode villagerPathNode = obj as VillagerPathNode;
			return villagerPathNode != null && this.Equals(villagerPathNode);
		}

		// Token: 0x06000148 RID: 328 RVA: 0x0000B8B8 File Offset: 0x00009AB8
		public override int GetHashCode()
		{
			return this.BlockPos.GetHashCode();
		}

		// Token: 0x06000149 RID: 329 RVA: 0x0000B8D8 File Offset: 0x00009AD8
		public List<VillagerPathNode> RetracePath()
		{
			List<VillagerPathNode> list = new List<VillagerPathNode>(this.pathLength + 1);
			for (int i = 0; i <= this.pathLength; i++)
			{
				list.Add(null);
			}
			VillagerPathNode villagerPathNode = this;
			for (int j = this.pathLength; j >= 0; j--)
			{
				list[j] = villagerPathNode;
				villagerPathNode = villagerPathNode.Parent;
			}
			return list;
		}

		// Token: 0x0600014A RID: 330 RVA: 0x0000B950 File Offset: 0x00009B50
		public int CompareTo(VillagerPathNode other)
		{
			int num = this.fCost.CompareTo(other.fCost);
			bool flag = num == 0;
			bool flag2 = flag;
			if (flag2)
			{
				num = this.hCost.CompareTo(other.hCost);
			}
			bool flag3 = num == 0;
			bool flag4 = flag3;
			if (flag4)
			{
				num = this.BlockPos.GetHashCode().CompareTo(other.BlockPos.GetHashCode());
			}
			return num;
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000B9C8 File Offset: 0x00009BC8
		public float distanceTo(VillagerPathNode other)
		{
			int num = Math.Abs(this.BlockPos.X - other.BlockPos.X);
			int num2 = Math.Abs(this.BlockPos.Y - other.BlockPos.Y);
			int num3 = Math.Abs(this.BlockPos.Z - other.BlockPos.Z);
			int num4 = Math.Min(num, num3);
			int num5 = Math.Max(num, num3);
			return (float)(1.414 * (double)num4 + (double)(num5 - num4) + (double)num2);
		}

		// Token: 0x0600014C RID: 332 RVA: 0x0000BA5C File Offset: 0x00009C5C
		public void SetGCostFromParent(VillagerPathNode parent, float extraCost)
		{
			bool flag = parent != null;
			bool flag2 = flag;
			if (flag2)
			{
				this.gCost = parent.gCost + parent.distanceTo(this) + extraCost;
			}
			else
			{
				this.gCost = extraCost;
			}
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x0600014D RID: 333 RVA: 0x0000BA98 File Offset: 0x00009C98
		public float fCost
		{
			get
			{
				return this.gCost + this.hCost;
			}
		}

		// Token: 0x040000AC RID: 172
		public VillagerPathNode Parent;

		// Token: 0x040000AD RID: 173
		public BlockPos BlockPos;

		// Token: 0x040000AE RID: 174
		public bool IsDoor;

		// Token: 0x040000AF RID: 175
		public float gCost;

		// Token: 0x040000B0 RID: 176
		public float hCost;

		// Token: 0x040000B1 RID: 177
		public int pathLength;
	}
}
