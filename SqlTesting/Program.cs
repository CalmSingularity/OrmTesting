using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static OrmTesting.Utilities;


namespace SqlTesting
{
	class Program
	{
		static void Main(string[] args)
		{
			DateTime start = DateTime.Now;

			SqlTester tester = new SqlTester();
			CrudTime elapsedTime = tester.Test();
			Console.WriteLine();
			Console.WriteLine(elapsedTime);

			TimeSpan totalTime = DateTime.Now - start;
			Console.WriteLine($"Общее время тестирования: {totalTime}");
			Console.WriteLine($"Накладные расходы: {totalTime - elapsedTime.TotalTime}");
			Console.WriteLine($"Дата и время тестирования SQL: {DateTime.Now}");
		}
	}
}
