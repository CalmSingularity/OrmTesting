using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace OrmTesting
{
	public class OrmTester
	{
		public OrmTester()
		{
			SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
			SqlProviderServices.SqlServerTypesAssemblyName = typeof(SqlGeography).Assembly.FullName;
		}

		public CrudTime Test(bool verbose = true)
		{
			CrudTime _elapsedTime = new CrudTime(); // здесь будет суммироваться временя, затраченное на операции с БД
			Stopwatch stopWatch = new Stopwatch(); // перепроверка общего времени через таймер
			stopWatch.Start();

			using (AdventureContext db = new AdventureContext()) // создаем контекст БД
			{
				foreach (var sp in db.SalesPersons) // запускаем тесты для каждого из продавцов
				{
					for (int day = 1; day < 31; ++day) // запускаем 30 дневных тестов
					{
						_elapsedTime += SimulateDailyStoreOperations(db, sp);
						_elapsedTime += SimulateDailyStoreReporting(db, sp);
					}
				}
			}
			stopWatch.Stop();
			Console.WriteLine($"Общее время тестирования по таймеру = {stopWatch.Elapsed}");
			return _elapsedTime;
		}

		/// <summary>
		/// Симуляция дневных операций с БД отдельного взятого продавца
		/// </summary>
		public CrudTime SimulateDailyStoreOperations(AdventureContext db, SalesPerson sp)
		{


			//TODO:
			return new CrudTime();
		}

		/// <summary>
		/// Симуляция дневной отчетности из БД по отдельно взятому продавцу
		/// </summary>
		public CrudTime SimulateDailyStoreReporting(AdventureContext db, SalesPerson sp)
		{
			Stopwatch readStopWatch = new Stopwatch();
			readStopWatch.Start();

			var dates = db.SalesOrderHeaders // получаем все даты, когда продавец работал
				.Where(so => so.SalesPerson.BusinessEntityID == sp.BusinessEntityID)
				.Select(so => so.OrderDate).Distinct().ToList();

			Random rand = new Random(); 
			var date = dates[rand.Next(0, dates.Count())]; // выбираем случайную дату

			Console.WriteLine($"Заказы по продавцу {sp.Person.FirstName} {sp.Person.LastName} за {date.ToShortDateString()}");

			var orders = db.SalesOrderHeaders // получаем все заказы по выбранным продавцу и дате
				.Where(so => so.OrderDate == date && so.SalesPerson.BusinessEntityID == sp.BusinessEntityID);

			foreach (var order in orders)
				Console.WriteLine(ReadSalesOrder(db, order));

			readStopWatch.Stop();
			//TODO:
			return new CrudTime(new TimeSpan(), readStopWatch.Elapsed, new TimeSpan(), new TimeSpan());
		}

		/// <summary>
		/// Симуляция месячной отчетности по продажам компании
		/// </summary>
		/// <returns></returns>
		public CrudTime SimulateMonthlyCompanyReporting(AdventureContext db)
		{
			//TODO:
			return new CrudTime();
		}

		/// <summary>
		/// Получает из БД полную информацию о заказе из таблиц SalesOrderHeader, SalesOrderDetails 
		/// и других связанных таблиц
		/// </summary>
		/// <param name="db">Контекст модели базы данных</param>
		/// <param name="so">Экземпляр заказа</param>
		/// <returns>Информацию о заказе в виду строки</returns>
		public string ReadSalesOrder(AdventureContext db, SalesOrderHeader so)
		{
			StringBuilder orderHeader = new StringBuilder();
			orderHeader.Append($"Заказ № {so.SalesOrderNumber} ");
			orderHeader.Append($"Заказ на поставку № {so.PurchaseOrderNumber} ");
			orderHeader.Append($"Номер счета: {so.AccountNumber}\n");
			orderHeader.Append($"Дата заказа:   {so.OrderDate.ToShortDateString()} ");
			orderHeader.Append($"Срок поставки: {so.DueDate.ToShortDateString()} ");
			orderHeader.Append($"Дата поставки: {((DateTime)so.ShipDate).ToShortDateString()}\n");
			if (so.OnlineOrderFlag)
				orderHeader.Append($"Заказ сделан онлайн\n");
			else
				orderHeader.Append($"Заказ сделан оффлайн\n");
			orderHeader.Append($"Территория: {so.SalesTerritory.Name} {so.SalesTerritory.CountryRegion.Name}\n");

			var person = so.Customer.Person;
			orderHeader.Append($"Покупатель: {person.Title} {person.FirstName} {person.LastName}\n");
			var phone = person.PersonPhones.FirstOrDefault();
			orderHeader.Append($"Тел.: {phone.PhoneNumber} ({phone.PhoneNumberType.Name})\n");
			orderHeader.Append($"Email: {person.EmailAddresses.FirstOrDefault().EmailAddress1}\n");

			person = so.SalesPerson.Person;
			orderHeader.Append($"Продавец: {person.FirstName} {person.LastName}\n");

			orderHeader.Append($"Магазин: {so.Customer.Store.Name}\n");
			orderHeader.Append($"Адрес магазина:\n{so.Customer.Store.BusinessEntity.BusinessEntityAddresses.FirstOrDefault().Address}\n");
			orderHeader.Append($"Адрес доставки:\n{so.ShipToAddress}\n");

			orderHeader.Append($"Адрес оплаты:\n{so.BillToAddress}\n");
			orderHeader.Append("\n");


			StringBuilder orderDetail = new StringBuilder();


			return orderHeader.Append(orderDetail).ToString();
		}
	}
}
