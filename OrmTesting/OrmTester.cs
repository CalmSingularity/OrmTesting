using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using static OrmTesting.Utilities;

namespace OrmTesting
{
	public class OrmTester
	{
		public OrmTester()
		{
			SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
			SqlProviderServices.SqlServerTypesAssemblyName = typeof(SqlGeography).Assembly.FullName;
		}

		public CrudTime Test()
		{
			CrudTime _elapsedTime = new CrudTime(); // здесь будет суммироваться время, затраченное на операции с БД

			using (AdventureContext db = new AdventureContext()) // создаем контекст БД
			{
				foreach (var sp in db.SalesPersons.ToList()) // запускаем тесты для каждого из продавцов
				{
					for (int day = 1; day < 2; ++day) // запускаем 30 дневных тестов
					{
						_elapsedTime += SimulateDailyOperations(db, sp, 4);
						_elapsedTime += SimulateDailyReporting(db, sp);
						_elapsedTime += SimulateMonthlyReporting(db);
					}
				}
			}
			return _elapsedTime;
		}

		/// <summary>
		/// Симуляция дневных операций с БД отдельного взятого продавца
		/// </summary>
		public CrudTime SimulateDailyOperations(AdventureContext db, SalesPerson sp, int iterations)
		{
			Random rand = new Random();
			Stopwatch createStopWatch = new Stopwatch();
			Stopwatch readStopWatch = new Stopwatch();
			Stopwatch updateStopWatch = new Stopwatch();

			readStopWatch.Start();
			// получаем магазин, где работает продавец
			var store = db.Stores
				.Where(s => s.SalesPersonID == sp.BusinessEntityID)
				.FirstOrDefault();
			if (store == null)
				store = new Store
				{
					BusinessEntity = new BusinessEntity
					{
						ModifiedDate = DateTime.Now,
						rowguid = Guid.NewGuid()
					},
					Name = GenerateRandomString(15),
					SalesPerson = sp,
					ModifiedDate = DateTime.Now,
					rowguid = Guid.NewGuid()
				};
			// получаем территорию, где работает продавец
			var territory = sp.SalesTerritory;
			if (territory == null)
			{
				territory = db.SalesTerritories.First();
				sp.SalesTerritory = territory;
			}
			var country = territory.CountryRegion;
			var province = db.StateProvinces
				.Where(s => s.TerritoryID == territory.TerritoryID)
				.FirstOrDefault();
			if (province == null)
				province = db.StateProvinces.First();
			var cities = db.Addresses
				.Where(a => a.StateProvinceID == province.StateProvinceID)
				.Select(a => a.City)
				.ToList();
			if (cities.Count == 0)
				cities.Add(GenerateRandomString(6));
			readStopWatch.Stop();

			// генерируем новых клиентов случайным образом
			createStopWatch.Start();
			for (int i = 0; i < iterations; ++i)
			{
				Address addr = new Address  // генерируем новый адрес случайным образом
				{
					AddressLine1 = GenerateRandomAddressLine(),
					City = cities[rand.Next(0, cities.Count)],
					StateProvince = province,
					PostalCode = rand.Next(10000, 1000000).ToString(),
					ModifiedDate = DateTime.Now,
					rowguid = Guid.NewGuid()
				};

				BusinessEntity be = new BusinessEntity
				{
					ModifiedDate = DateTime.Now,
					rowguid = Guid.NewGuid()
				};

				BusinessEntityAddress beAddr = new BusinessEntityAddress
				{
					BusinessEntity = be,
					Address = addr,
					AddressTypeID = 4,
					ModifiedDate = DateTime.Now,
					rowguid = Guid.NewGuid()
				};

				Person person = new Person  // генерируем нового клиента случайным образом
				{
					PersonType = "IN",
					Title = "Mr.",
					FirstName = GenerateRandomString(rand.Next(3, 8)),
					LastName = GenerateRandomString(rand.Next(3, 10)),
					ModifiedDate = DateTime.Now,
					rowguid = Guid.NewGuid(),
					BusinessEntity = be
				};

				Customer customer = new Customer
				{
					Person = person,
					Store = store,
					SalesTerritory = territory,
					ModifiedDate = DateTime.Now,
					rowguid = Guid.NewGuid()
				};

				PersonPhone phone = new PersonPhone
				{
					BusinessEntityID = customer.Person.BusinessEntityID,
					PhoneNumber = rand.Next(1112223344, 2147483647).ToString(),
					PhoneNumberTypeID = rand.Next(1, 4),
					ModifiedDate = DateTime.Now
				};
				EmailAddress email = new EmailAddress
				{
					BusinessEntityID = customer.Person.BusinessEntityID,
					EmailAddress1 = GenerateRandomString(8) + "@" + GenerateRandomString(6) + ".com",
					ModifiedDate = DateTime.Now,
					rowguid = Guid.NewGuid()
				};
				customer.Person.PersonPhones.Add(phone);
				customer.Person.EmailAddresses.Add(email);

				// добавляем созданные сущности в БД
				db.Entry(addr).State = EntityState.Added;
				db.Entry(be).State = EntityState.Added;
				db.Entry(beAddr).State = EntityState.Added;
				db.Entry(person).State = EntityState.Added;
				db.Entry(customer).State = EntityState.Added;
				db.Entry(phone).State = EntityState.Added;
				db.Entry(email).State = EntityState.Added;
				db.SaveChanges();
				Console.WriteLine($"Добавлен клиент {person.FirstName} {person.LastName} с адресом:");
				Console.WriteLine(addr);
				Console.WriteLine($"Тел.: {phone.PhoneNumber}, Email: {email.EmailAddress1}");
			}
			createStopWatch.Stop();

			// получаем список клиентов магазина
			readStopWatch.Start();
			var customers = db.Customers  
				.Where(c => c.StoreID == store.BusinessEntityID)
				.ToList();
			readStopWatch.Stop();

			// обновляем контактные данные по ряду клиентов
			updateStopWatch.Start();
			for (int i = 0; i < iterations; i += 2)
			{
				Customer customer = customers[rand.Next(0, customers.Count)];

				PersonPhone phone = new PersonPhone
				{
					BusinessEntityID = customer.Person.BusinessEntityID,
					PhoneNumber = rand.Next(1112223344, 2147483647).ToString(),
					PhoneNumberTypeID = rand.Next(1, 4),
					ModifiedDate = DateTime.Now
				};
				EmailAddress email = new EmailAddress
				{
					BusinessEntityID = customer.Person.BusinessEntityID,
					EmailAddress1 = GenerateRandomString(8)+"@"+GenerateRandomString(6)+".com",
					ModifiedDate = DateTime.Now,
					rowguid = Guid.NewGuid()
				};

				// сохраняем обновленные сущности в БД
				db.Entry(phone).State = EntityState.Added;
				db.Entry(email).State = EntityState.Added;
				customer.Person.PersonPhones.Add(phone);
				customer.Person.EmailAddresses.Add(email);
				db.Entry(customer).State = EntityState.Modified;
				db.SaveChanges();
				Console.WriteLine($"Обновлен клиент {customer.Person.FirstName} {customer.Person.LastName}:");
				Console.WriteLine($"Тел.: {phone.PhoneNumber}, Email: {email.EmailAddress1}");
			}
			updateStopWatch.Stop();

			return new CrudTime(
				createStopWatch.Elapsed,
				readStopWatch.Elapsed,
				updateStopWatch.Elapsed,
				new TimeSpan());
		}

		/// <summary>
		/// Симуляция дневной отчетности из БД по отдельно взятому продавцу
		/// </summary>
		public CrudTime SimulateDailyReporting(AdventureContext db, SalesPerson sp)
		{
			Random rand = new Random();
			Stopwatch readStopWatch = new Stopwatch();
			readStopWatch.Start();

			var dates = db.SalesOrderHeaders  // получаем все даты, когда продавец работал
				.Where(so => so.SalesPerson.BusinessEntityID == sp.BusinessEntityID)
				.Select(so => so.OrderDate).Distinct().ToList();

			var date = dates[rand.Next(0, dates.Count())];  // выбираем случайную дату

			Console.WriteLine($"Заказы по продавцу {sp.Person.FirstName} {sp.Person.LastName} за {date.ToShortDateString()}");

			var orders = db.SalesOrderHeaders  // получаем все заказы по выбранным продавцу и дате
				.Where(so => so.OrderDate == date && so.SalesPerson.BusinessEntityID == sp.BusinessEntityID);

			foreach (var order in orders)  // читаем полную информацию по всем заказам
				Console.WriteLine(ReadSalesOrder(db, order));

			decimal totalSales = orders.Sum(o => o.TotalDue);
			Console.WriteLine($"Всего по продавцу {sp.Person.FirstName} {sp.Person.LastName} за {date.ToShortDateString()} = {totalSales,10:F2}");
			readStopWatch.Stop();

			return new CrudTime(new TimeSpan(), readStopWatch.Elapsed, new TimeSpan(), new TimeSpan());
		}

		/// <summary>
		/// Симуляция месячной отчетности по продажам компании
		/// </summary>
		/// <returns></returns>
		public CrudTime SimulateMonthlyReporting(AdventureContext db)
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
			StringBuilder printedOrder = new StringBuilder();
			printedOrder.Append($"Заказ № {so.SalesOrderNumber} ");
			printedOrder.Append($"Заказ на поставку № {so.PurchaseOrderNumber} ");
			printedOrder.Append($"Номер счета: {so.AccountNumber}\n");
			printedOrder.Append($"Дата заказа:   {so.OrderDate.ToShortDateString()} ");
			printedOrder.Append($"Срок поставки: {so.DueDate.ToShortDateString()} ");
			printedOrder.Append($"Дата поставки: {((DateTime)so.ShipDate).ToShortDateString()}\n");
			if (so.OnlineOrderFlag)
				printedOrder.Append($"Заказ сделан онлайн\n");
			else
				printedOrder.Append($"Заказ сделан оффлайн\n");
			printedOrder.Append($"Территория: {so.SalesTerritory.Name} {so.SalesTerritory.CountryRegion.Name}\n");

			var person = so.Customer.Person;
			printedOrder.Append($"Покупатель: {person.Title} {person.FirstName} {person.LastName}\n");
			var phone = person.PersonPhones.FirstOrDefault();
			printedOrder.Append($"Тел.: {phone.PhoneNumber} ({phone.PhoneNumberType.Name})\n");
			printedOrder.Append($"Email: {person.EmailAddresses.FirstOrDefault().EmailAddress1}\n");

			person = so.SalesPerson.Person;
			printedOrder.Append($"Продавец: {person.FirstName} {person.LastName}\n");
			printedOrder.Append($"Магазин: {so.Customer.Store.Name}\n");
			printedOrder.Append($"Адрес магазина:\n{so.Customer.Store.BusinessEntity.BusinessEntityAddresses.FirstOrDefault().Address}\n");
			printedOrder.Append($"Адрес доставки:\n{so.ShipToAddress}\n");
			printedOrder.Append($"Метод доставки: {so.ShipMethod.Name}\n");

			printedOrder.Append("Состав заказа:\n");
			foreach (var ol in so.SalesOrderDetails)
			{
				printedOrder.Append($"{ol.Product.ProductNumber,-10} {ol.Product.Name,-35} ");
				printedOrder.Append($"{ol.UnitPrice,8:F2} * {ol.OrderQty,2} -{ol.UnitPriceDiscount,3:P0} = {ol.LineTotal,8:F2} ");
				printedOrder.Append($"({ol.SpecialOffer.Description})\n");
			}

			var currency = so.SalesTerritory.CountryRegion.CountryRegionCurrencies.FirstOrDefault().CurrencyCode;
			printedOrder.Append($"Подитог:        {so.SubTotal,9:F2} {currency}\n");
			printedOrder.Append($"Налог:          {so.TaxAmt,9:F2} {currency}\n");
			printedOrder.Append($"Доставка:       {so.Freight,9:F2} {currency}\n");
			printedOrder.Append($"Всего к оплате: {so.TotalDue,9:F2} {currency}\n");
			printedOrder.Append($"Тип кредитной карты: {so.CreditCard?.CardType}\n");
			printedOrder.Append($"Код подтверждения: {so.CreditCardApprovalCode}\n");
			printedOrder.Append($"Адрес оплаты:\n{so.BillToAddress}\n");

			return printedOrder.ToString();
		}
	}
}
