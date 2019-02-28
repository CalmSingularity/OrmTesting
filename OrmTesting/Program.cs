using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrmTesting
{
	class Program
	{
		static void Main(string[] args)
		{
			//using (AdventureContext db = new AdventureContext())
			//{
			//	var orders = db.SalesOrderHeaders.Where(o => o.OrderDate == new DateTime(2011, 10, 01));
			//	foreach (var order in orders)
			//		Console.WriteLine($"{order.SalesOrderID} {order.OrderDate} {order.TotalDue} {order.SalesTerritory.CountryRegion.CountryRegionCurrencies.FirstOrDefault().CurrencyCode} ");
			//}
			OrmTester tester = new OrmTester();
			CrudTime elapsedTime = tester.Test();
			Console.WriteLine(elapsedTime);
		}
	}
}
