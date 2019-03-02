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
				//foreach (var sp in db.SalesPersons.ToList()) // запускаем тесты для каждого из продавцов
				SalesPerson sp = db.SalesPersons.Find(282);
				{
					for (int day = 1; day < 2; ++day) // запускаем 30 дневных тестов
					{
						_elapsedTime += SimulateDailyReporting(db, sp);
						_elapsedTime += SimulateDailyOperations(db, sp, 8);
					}
				}
			}
			return _elapsedTime;
		}

		/// <summary>
		/// Симуляция дневных операций с БД отдельного взятого продавца
		/// </summary>
		/// <param name="iterations">Количество создаваемых заказов, при этом клиентов будет создано в 2 раза меньше</param>
		public CrudTime SimulateDailyOperations(AdventureContext db, SalesPerson sp, int iterations)
		{
			Random rand = new Random();
			Stopwatch createStopWatch = new Stopwatch();
			Stopwatch readStopWatch = new Stopwatch();
			Stopwatch updateStopWatch = new Stopwatch();
			Stopwatch deleteStopWatch = new Stopwatch();

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

			readStopWatch.Start();
			// получаем список клиентов магазина
			var customers = db.Customers.Where(c => c.StoreID == store.BusinessEntityID).ToList();
			// получаем список всех продуктов
			var products = db.Products.ToList();
			// получаем список всех кредитных карт
			var creditCards = db.CreditCards.ToList();
			readStopWatch.Stop();

			// генерируем новые заказы случайным образом
			createStopWatch.Start();
			for (int h = 0; h < iterations; ++h)
			{
				Customer customer = customers[rand.Next(0, customers.Count)];
				Address addr = new Address  // генерируем новый адрес случайным образом
				{
					AddressLine1 = GenerateRandomAddressLine(),
					City = cities[rand.Next(0, cities.Count)],
					StateProvince = province,
					PostalCode = rand.Next(10000, 1000000).ToString(),
					ModifiedDate = DateTime.Now,
					rowguid = Guid.NewGuid()
				};
				SalesOrderHeader soHeader = new SalesOrderHeader
				{
					RevisionNumber = 8,
					OrderDate = DateTime.Now,
					DueDate = DateTime.Now + new TimeSpan(14, 0, 0, 0),
					Status = 5,
					OnlineOrderFlag = true,
					Customer = customer,
					SalesPerson = sp,
					SalesTerritory = territory,
					BillToAddress = addr,
					ShipToAddress = addr,
					ShipMethodID = rand.Next(1, 6),
					CreditCard = creditCards[rand.Next(0, creditCards.Count)],
					CreditCardApprovalCode = rand.Next(1000000000, 2147483647).ToString(),
					SubTotal = 0,
					TaxAmt = 0,
					Freight = 0,
					TotalDue = 0,
					rowguid = Guid.NewGuid(),
					ModifiedDate = DateTime.Now
				};
				db.Entry(addr).State = EntityState.Added;
				db.Entry(soHeader).State = EntityState.Added;
				db.SaveChanges();

				// генерируем от 1 до 15 позиций в заказе
				int linesCount = rand.Next(1, 17);
				for (int d = 0; d < linesCount; ++d)
				{
					Product product = products[rand.Next(0, products.Count)];
					SpecialOffer specialOffer = db.SpecialOffers.Find(1);
					SalesOrderDetail soDetail = new SalesOrderDetail
					{
						SalesOrderID = soHeader.SalesOrderID,
						OrderQty = (short)rand.Next(1, 8),
						Product = products[rand.Next(0, products.Count)],
						SpecialOffer = specialOffer,
						UnitPrice = rand.Next(3, 3579),
						UnitPriceDiscount = 0,
						rowguid = Guid.NewGuid(),
						ModifiedDate = DateTime.Now
					};
					db.Entry(soDetail).State = EntityState.Added;
					soHeader.SalesOrderDetails.Add(soDetail);
					soHeader.SubTotal += soDetail.LineTotal;
					db.SaveChanges();
				}
				// обновляем сумму
				soHeader.TaxAmt = soHeader.SubTotal * 0.096M;
				soHeader.Freight = soHeader.SubTotal * 0.03M;
				db.SaveChanges();

				Console.WriteLine($"Добавлен заказ:\n{PrintSalesOrder(db, soHeader)}");
			}
			createStopWatch.Stop();

			// обновляем контактные данные клиентов
			updateStopWatch.Start();
			for (int i = 0; i < iterations; ++i)
			{
				Customer customer = customers[rand.Next(0, customers.Count)];

				PersonPhone phone = new PersonPhone
				{
					BusinessEntityID = customer.Person.BusinessEntityID,
					PhoneNumber = rand.Next(1112223344, 2147483647).ToString(),
					PhoneNumberTypeID = rand.Next(1, 4),
					ModifiedDate = DateTime.Now
				};
				customer.Person.PersonPhones.Add(phone);

				EmailAddress email = new EmailAddress
				{
					BusinessEntityID = customer.Person.BusinessEntityID,
					EmailAddress1 = GenerateRandomString(8) + "@" + GenerateRandomString(6) + ".com",
					ModifiedDate = DateTime.Now,
					rowguid = Guid.NewGuid()
				};
				customer.Person.EmailAddresses.Add(email);

				// сохраняем обновленные сущности в БД
				db.Entry(phone).State = EntityState.Added;
				db.Entry(email).State = EntityState.Added;
				db.Entry(customer).State = EntityState.Modified;
				db.SaveChanges();
				Console.Write($"Обновлен клиент {customer.Person.FirstName} {customer.Person.LastName}. ");
				Console.WriteLine($"Новый тел.: {phone.PhoneNumber}, новый email: {email.EmailAddress1}");
			}
			updateStopWatch.Stop();

			readStopWatch.Start();
			// получаем список сегодняшних заказов
			var ordersToday = db.SalesOrderHeaders
				.Where(o => DbFunctions.TruncateTime(o.OrderDate) == DbFunctions.TruncateTime(DateTime.Now))
				.ToList();
			readStopWatch.Stop();

			// обновляем данные по ряду сегодняшних заказов
			updateStopWatch.Start();
			for (int o = 0; o < iterations; ++o)
			{
				SalesOrderHeader orderToUpdate = ordersToday[rand.Next(0, ordersToday.Count)];
				orderToUpdate.ShipDate = orderToUpdate.OrderDate + new TimeSpan(7, 0, 0, 0);
				orderToUpdate.ModifiedDate = DateTime.Now;
				orderToUpdate.CreditCard = creditCards[rand.Next(0, creditCards.Count)];
				// сохраняем обновленный заказ в БД
				db.Entry(orderToUpdate).State = EntityState.Modified;
				db.SaveChanges();
				Console.WriteLine($"Обновлен заказ № {orderToUpdate.SalesOrderNumber}");
			}
			updateStopWatch.Stop();

			// удаляем ряд заказов
			deleteStopWatch.Start();
			foreach (var orderToDelete in ordersToday.Take(iterations))
			{
				// сначала удаляем все позиции заказа
				foreach (SalesOrderDetail orderLine in orderToDelete.SalesOrderDetails)
					db.Entry(orderLine).State = EntityState.Deleted;
				db.Entry(orderToDelete).State = EntityState.Deleted; // удаляем сам заказ
				db.SaveChanges();
				Console.WriteLine($"Удален заказ № {orderToDelete.SalesOrderNumber}");
			}
			deleteStopWatch.Stop();


			return new CrudTime(
				createStopWatch.Elapsed,
				readStopWatch.Elapsed,
				updateStopWatch.Elapsed,
				deleteStopWatch.Elapsed);
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
				Console.WriteLine(PrintSalesOrder(db, order));

			//TODO
			//decimal totalSales = orders.Sum(o => o.TotalDue);
			//Console.WriteLine($"Всего по продавцу {sp.Person.FirstName} {sp.Person.LastName} за {date.ToShortDateString()} = {totalSales,10:F2}");
			readStopWatch.Stop();

			return new CrudTime(new TimeSpan(), readStopWatch.Elapsed, new TimeSpan(), new TimeSpan());
		}

		/// <summary>
		/// Выводит в консоль полную информацию о заказе из таблиц  
		/// SalesOrderHeader, SalesOrderDetails и других связанных таблиц
		/// </summary>
		/// <param name="db">Контекст модели базы данных</param>
		/// <param name="so">Экземпляр заказа</param>
		/// <returns>Информацию о заказе в виду строки</returns>
		public string PrintSalesOrder(AdventureContext db, SalesOrderHeader so)
		{
			StringBuilder sbOrder = new StringBuilder();
			sbOrder.Append($"Заказ № {so.SalesOrderNumber} ");
			sbOrder.Append($"Заказ на поставку № {so.PurchaseOrderNumber} ");
			sbOrder.Append($"Номер счета: {so.AccountNumber}\n");
			sbOrder.Append($"Дата заказа:   {so.OrderDate.ToShortDateString()} ");
			sbOrder.Append($"Срок поставки: {so.DueDate.ToShortDateString()} ");
			sbOrder.Append($"Дата поставки: {so.ShipDate?.ToShortDateString()}\n");
			if (so.OnlineOrderFlag)
				sbOrder.Append($"Заказ сделан онлайн\n");
			else
				sbOrder.Append($"Заказ сделан оффлайн\n");
			sbOrder.Append($"Территория: {so.SalesTerritory.Name} {so.SalesTerritory.CountryRegion.Name}\n");

			var person = so.Customer.Person;
			sbOrder.Append($"Покупатель: {person?.Title} {person?.FirstName} {person?.LastName}\n");
			var phone = person?.PersonPhones.FirstOrDefault();
			sbOrder.Append($"Тел.: {phone?.PhoneNumber} ({phone?.PhoneNumberType?.Name})\n");
			sbOrder.Append($"Email: {person?.EmailAddresses.FirstOrDefault()?.EmailAddress1}\n");

			person = so.SalesPerson.Person;
			sbOrder.Append($"Продавец: {person.FirstName} {person.LastName}\n");
			sbOrder.Append($"Магазин: {so.Customer.Store.Name}\n");
			sbOrder.Append($"Адрес магазина:\n{so.Customer.Store.BusinessEntity.BusinessEntityAddresses.FirstOrDefault()?.Address}\n");
			sbOrder.Append($"Адрес доставки:\n{so.ShipToAddress}\n");
			sbOrder.Append($"Метод доставки: {so.ShipMethod?.Name}\n");

			sbOrder.Append("Состав заказа:\n");
			foreach (var ol in so.SalesOrderDetails)
			{
				sbOrder.Append($"{ol.Product.ProductNumber,-10} {ol.Product.Name,-35} ");
				sbOrder.Append($"{ol.UnitPrice,8:F2} * {ol.OrderQty,2} -{ol.UnitPriceDiscount,3:P0} = {ol.LineTotal,8:F2} ");
				sbOrder.Append($"({ol.SpecialOffer.Description})\n");
			}

			var currency = so.SalesTerritory.CountryRegion.CountryRegionCurrencies.FirstOrDefault().CurrencyCode;
			sbOrder.Append($"Подитог:        {so.SubTotal,9:F2} {currency}\n");
			sbOrder.Append($"Налог:          {so.TaxAmt,9:F2} {currency}\n");
			sbOrder.Append($"Доставка:       {so.Freight,9:F2} {currency}\n");
			sbOrder.Append($"Всего к оплате: {so.TotalDue,9:F2} {currency}\n");
			sbOrder.Append($"Тип кредитной карты: {so.CreditCard?.CardType}\n");
			sbOrder.Append($"Код подтверждения: {so.CreditCardApprovalCode}\n");
			sbOrder.Append($"Адрес оплаты:\n{so.BillToAddress}\n");

			return sbOrder.ToString();
		}
	}
}
