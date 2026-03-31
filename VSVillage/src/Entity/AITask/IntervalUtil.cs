using System;
using Vintagestory.API.Common;

namespace VsVillage
{
	// Token: 0x0200001F RID: 31
	public class IntervalUtil
	{
		// Token: 0x060000DE RID: 222 RVA: 0x00009474 File Offset: 0x00007674
		public static bool matchesCurrentTime(DayTimeFrame[] dayTimeFrames, IWorldAccessor world, float offset = 0f)
		{
			bool flag = false;
			if (dayTimeFrames != null)
			{
				float num = world.Calendar.HourOfDay / world.Calendar.HoursPerDay * 24f;
				int num2 = 0;
				while (!flag && num2 < dayTimeFrames.Length)
				{
					flag |= dayTimeFrames[num2].Matches((double)(num + offset));
					num2++;
				}
			}
			return flag;
		}
	}
}
