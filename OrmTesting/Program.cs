using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static OrmTesting.Utilities;

namespace OrmTesting
{
	class Program
	{
		static void Main(string[] args)
		{
			DateTime start = DateTime.Now;

			OrmTester tester = new OrmTester();
			CrudTime elapsedTime = tester.Test();
			Console.WriteLine();
			Console.WriteLine(elapsedTime);

			Console.WriteLine($"Общее время тестирования: {DateTime.Now - start}");
			Console.ReadKey();
		}
	}
}
