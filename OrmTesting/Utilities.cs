using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrmTesting
{
	/// <summary>
	/// Структура для хранения времени, потраченного на операции create, read, update, delete
	/// </summary>
	public struct CrudTime
	{
		public TimeSpan CreateTime { get; private set; }
		public TimeSpan ReadTime { get; private set; }
		public TimeSpan UpdateTime { get; private set; }
		public TimeSpan DeleteTime { get; private set; }
		public TimeSpan TotalTime
		{
			get { return CreateTime + ReadTime + UpdateTime + DeleteTime; }
		}

		public CrudTime(TimeSpan createTime, TimeSpan readTime, TimeSpan updateTime, TimeSpan deleteTime)
		{
			CreateTime = createTime;
			ReadTime = readTime;
			UpdateTime = updateTime;
			DeleteTime = deleteTime;
		}

		public static CrudTime operator +(CrudTime t1, CrudTime t2)
		{
			return new CrudTime(
				t1.CreateTime + t2.CreateTime,
				t1.ReadTime + t2.ReadTime,
				t1.UpdateTime + t2.UpdateTime,
				t1.DeleteTime + t2.DeleteTime);
		}
		public static CrudTime operator -(CrudTime t1, CrudTime t2)
		{
			return new CrudTime(
				t1.CreateTime - t2.CreateTime,
				t1.ReadTime - t2.ReadTime,
				t1.UpdateTime - t2.UpdateTime,
				t1.DeleteTime - t2.DeleteTime);
		}

		public override string ToString()
		{
			return $"Create={CreateTime}; Read={ReadTime}; Update={UpdateTime}; Delete={DeleteTime};\nTotal={TotalTime}";
		}
	}
}
