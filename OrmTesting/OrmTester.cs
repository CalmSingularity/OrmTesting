using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
				var salesPersonIDs = db.SalesPersons.AsNoTracking()
					.Select(s => s.BusinessEntityID).ToList();  // получаем список всех продавцов
				foreach (var salesPersonID in salesPersonIDs)  // запускаем тесты для каждого из продавцов
				{
					_elapsedTime += SimulateDailyReporting(salesPersonID);
					_elapsedTime += SimulateDailyOperations(salesPersonID, 1);
				}
			}
			return _elapsedTime;
		}

		/// <summary>
		/// Симуляция дневной отчетности из БД по отдельно взятому продавцу:
		/// загружает из БД и выводит в консоль полную информацию по всем заказам продавца
		/// на случайно выбранную дату
		/// </summary>
		public CrudTime SimulateDailyReporting(int salesPersonID)//AdventureContext db, SalesPerson sp)
		{
			Random rand = new Random();
			Stopwatch readStopWatch = new Stopwatch();
			readStopWatch.Start();
			using (AdventureContext db = new AdventureContext()) // создаем контекст БД
			{
				// получаем все даты, когда продавец работал
				var dates = db.SalesOrderHeaders.AsNoTracking()
					.Where(so => so.SalesPerson.BusinessEntityID == salesPersonID)
					.Select(so => so.OrderDate).Distinct().ToList();

				var date = dates[rand.Next(0, dates.Count())];  // выбираем случайную дату

				SalesPerson sp = db.SalesPersons.Find(salesPersonID);
				Console.WriteLine($"Заказы по продавцу {sp.Person.FirstName} {sp.Person.LastName} за {date.ToShortDateString()}");

				// читаем из БД все заказы по выбранным продавцу и дате
				var orders = db.SalesOrderHeaders.AsNoTracking()
					.Where(so => so.OrderDate == date && so.SalesPerson.BusinessEntityID == sp.BusinessEntityID)
					.ToList();

				// выводим на экран полную информацию по всем прочитанным заказам
				foreach (var order in orders)
					Console.WriteLine(PrintSalesOrder(db, order));
			}
			readStopWatch.Stop();
			return new CrudTime(new TimeSpan(), readStopWatch.Elapsed, new TimeSpan(), new TimeSpan());
		}

		/// <summary>
		/// Симуляция дневных операций с БД отдельного взятого продавца:
		/// 1. Создает N новых клиентов, 
		/// 2. Создает N новые заказов,
		/// 3. Изменяет контактные данные N клиентов, 
		/// 4. Изменяет N заказов, 
		/// 5. Удаляет N заказов
		/// </summary>
		/// <param name="nIterations">Количество создаваемых и изменяемых клиентов и заказов (N)</param>
		public CrudTime SimulateDailyOperations(int spID, int nIterations)//AdventureContext db, SalesPerson sp, int nIterations)
		{
			// таймеры для подсчета времени, затраченного на операции CRUD (создание, чтение, изменение, удаление) в БД
			Stopwatch createStopWatch = new Stopwatch();
			Stopwatch readStopWatch = new Stopwatch();
			Stopwatch updateStopWatch = new Stopwatch();
			Stopwatch deleteStopWatch = new Stopwatch();
			Random rand = new Random();

			int storeID;
			int territoryID;
			string countryCode;
			int provinceID;
			List<string> cities;
			List<int> customerIDs;
			List<int> productIDs;
			List<int> creditCardIDs;

			// вначале получаем необходимую информацию о продавце
			readStopWatch.Start();
			using (AdventureContext db = new AdventureContext()) // создаем контекст БД
			{
				SalesPerson sp = db.SalesPersons.Find(spID);

				// получаем один из магазинов, где работает продавец
				var store = db.Stores
					.Where(s => s.SalesPersonID == sp.BusinessEntityID)
					.FirstOrDefault();
				if (store == null)
				{
					store = new Store  // если магазин отсутствует, создаем новый
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
					db.Stores.Add(store);
					db.SaveChanges();
				}
				storeID = store.BusinessEntityID;

				var territory = sp.SalesTerritory;  // получаем территорию, где работает продавец
				if (territory == null)  // если территория не указана, выбираем первую попавшуюся из БД
				{
					territory = db.SalesTerritories.First();
					sp.SalesTerritory = territory;
				}
				territoryID = sp.SalesTerritory.TerritoryID;

				countryCode = territory.CountryRegion.CountryRegionCode;
				var province = db.StateProvinces
					.Where(s => s.TerritoryID == territory.TerritoryID)
					.FirstOrDefault();
				if (province == null)
				{
					province = db.StateProvinces.First();
					db.StateProvinces.Add(province);
					db.SaveChanges();
				}
				provinceID = province.StateProvinceID;

				cities = db.Addresses.AsNoTracking()
					.Where(a => a.StateProvinceID == province.StateProvinceID)
					.Select(a => a.City)
					.ToList();
				if (cities.Count == 0)
					cities.Add(GenerateRandomString(6));
			}
			readStopWatch.Stop();

			// 1. Создаем N новых клиентов случайным образом
			createStopWatch.Start();
			using (AdventureContext db = new AdventureContext()) // создаем контекст БД
			{
				// массивы для хранения новых объектов до записи их в БД
				BusinessEntity[] newBusinessEntities = new BusinessEntity[nIterations];
				Address[] newAddresses = new Address[nIterations];
				BusinessEntityAddress[] newBusinessEntityAddresses = new BusinessEntityAddress[nIterations];
				Person[] newPeople = new Person[nIterations];
				Customer[] newCustomers = new Customer[nIterations];
				PersonPhone[] newPhones = new PersonPhone[nIterations];
				EmailAddress[] newEmails = new EmailAddress[nIterations];
				SalesTerritory territory = db.SalesTerritories.Find(territoryID);

				for (int i = 0; i < nIterations; ++i) // генерируем новых клиентов
				{
					newBusinessEntities[i] = new BusinessEntity  // генерируем новые бизнес-сущности
					{
						ModifiedDate = DateTime.Now,
						rowguid = Guid.NewGuid()
					};

					newAddresses[i] = new Address  // генерируем новый адрес случайным образом
					{
						AddressLine1 = GenerateRandomAddressLine(),
						City = cities[rand.Next(0, cities.Count)],
						StateProvinceID = provinceID,
						PostalCode = rand.Next(10000, 1000000).ToString(),
						ModifiedDate = DateTime.Now,
						rowguid = Guid.NewGuid()
					};

					newBusinessEntityAddresses[i] = new BusinessEntityAddress  // привязываем адрес к бизнес-сущности
					{
						BusinessEntity = newBusinessEntities[i],
						Address = newAddresses[i],
						AddressTypeID = 4,
						ModifiedDate = DateTime.Now,
						rowguid = Guid.NewGuid()
					};

					newPeople[i] = new Person  // генерируем нового клиента случайным образом
					{
						BusinessEntity = newBusinessEntities[i],
						PersonType = "IN",
						Title = "Mr.",
						FirstName = GenerateRandomString(rand.Next(3, 8)),
						LastName = GenerateRandomString(rand.Next(3, 10)),
						ModifiedDate = DateTime.Now,
						rowguid = Guid.NewGuid()
					};
					newCustomers[i] = new Customer
					{
						Person = newPeople[i],
						StoreID = storeID,
						SalesTerritory = territory,
						ModifiedDate = DateTime.Now,
						rowguid = Guid.NewGuid()
					};
				}

				// сохраняем новых клиентов в БД
				db.BusinessEntityAddresses.AddRange(newBusinessEntityAddresses);
				db.BusinessEntities.AddRange(newBusinessEntities);
				db.Addresses.AddRange(newAddresses);
				db.SaveChanges();  // сохраняем изменения в БД
				db.People.AddRange(newPeople);
				db.Customers.AddRange(newCustomers);
				db.SaveChanges();  // сохраняем изменения в БД

				for (int i = 0; i < nIterations; ++i) // добавляем контактные данные
				{
					newPhones[i] = new PersonPhone
					{
						BusinessEntityID = newBusinessEntities[i].BusinessEntityID,
						PhoneNumber = rand.Next(1112223344, 2147483647).ToString(),
						PhoneNumberTypeID = rand.Next(1, 4),
						ModifiedDate = DateTime.Now
					};
					newEmails[i] = new EmailAddress
					{
						BusinessEntityID = newBusinessEntities[i].BusinessEntityID,
						EmailAddress1 = GenerateRandomString(8, false) + "@" + GenerateRandomString(6, false) + ".com",
						ModifiedDate = DateTime.Now,
						rowguid = Guid.NewGuid()
					};
					Console.WriteLine($"Добавлен клиент {newPeople[i].FirstName} {newPeople[i].LastName} с адресом:");
					Console.WriteLine($"Тел.: {newPhones[i].PhoneNumber}, Email: {newEmails[i].EmailAddress1}");
				}
				db.PersonPhones.AddRange(newPhones);
				db.EmailAddresses.AddRange(newEmails);
				db.SaveChanges();  // сохраняем изменения в БД
			}
			createStopWatch.Stop();

			// получаем список клиентов магазина, всех продуктов и кредитных карт
			readStopWatch.Start();
			using (AdventureContext db = new AdventureContext()) // создаем контекст БД
			{
				customerIDs = db.Customers.AsNoTracking()
					.Where(c => c.StoreID == storeID)
					.Select(c => c.CustomerID)
					.ToList();

				productIDs = db.Products.AsNoTracking()  // получаем список всех продуктов
					.Select(p => p.ProductID).ToList();
				creditCardIDs = db.CreditCards.AsNoTracking()  // получаем список всех кредитных карт
					.Select(cc => cc.CreditCardID).ToList();
			}
			readStopWatch.Stop();

			// 2. Создаем N новых заказов случайным образом
			createStopWatch.Start();
			using (AdventureContext db = new AdventureContext()) // создаем контекст БД
			{
				// массивы для хранения новых объектов до записи их в БД
				SalesOrderHeader[] newSalesOrderHeaders = new SalesOrderHeader[nIterations];
				Address[] newAddresses = new Address[nIterations];

				SalesTerritory territory = db.SalesTerritories.Find(territoryID);
				StateProvince province = db.StateProvinces.Find(provinceID);
				SalesPerson sp = db.SalesPersons.Find(spID);

				for (int h = 0; h < nIterations; ++h)
				{
					newAddresses[h] = new Address  // генерируем новый адрес случайным образом
					{
						AddressLine1 = GenerateRandomAddressLine(),
						City = cities[rand.Next(0, cities.Count)],
						StateProvince = province,
						PostalCode = rand.Next(10000, 1000000).ToString(),
						ModifiedDate = DateTime.Now,
						rowguid = Guid.NewGuid()
					};
					newSalesOrderHeaders[h] = new SalesOrderHeader
					{
						RevisionNumber = 8,
						OrderDate = DateTime.Now,
						DueDate = DateTime.Now + new TimeSpan(14, 0, 0, 0),
						Status = 5,
						OnlineOrderFlag = true,
						Customer = db.Customers.Find(customerIDs[rand.Next(0, customerIDs.Count)]),
						SalesPerson = sp,
						SalesTerritory = territory,
						BillToAddress = newAddresses[h],
						ShipToAddress = newAddresses[h],
						ShipMethod = db.ShipMethods.Find(rand.Next(1, 6)),
						CreditCard = db.CreditCards.Find(creditCardIDs[rand.Next(0, creditCardIDs.Count)]),
						CreditCardApprovalCode = rand.Next(1000000000, 2147483647).ToString(),
						SubTotal = 0,
						TaxAmt = 0,
						Freight = 0,
						TotalDue = 0,
						rowguid = Guid.NewGuid(),
						ModifiedDate = DateTime.Now
					};
				}
				db.SalesOrderHeaders.AddRange(newSalesOrderHeaders);  // добавляем все созданные сущности в БД
				db.SaveChanges();  // сохраняем изменения в БД

				SpecialOffer specialOffer = db.SpecialOffers.Find(1);
				// генерируем от 1 до 15 случайных позиций для каждого созданного заказа
				for (int h = 0; h < nIterations; ++h)
				{
					int linesCount = rand.Next(1, 16);
					SalesOrderDetail[] newSalesOrderDetails = new SalesOrderDetail[linesCount];
					for (int d = 0; d < linesCount; ++d)
					{
						newSalesOrderDetails[d] = new SalesOrderDetail
						{
							SalesOrderHeader = newSalesOrderHeaders[h],
							OrderQty = (short)rand.Next(1, 8),
							Product = db.Products.Find(productIDs[rand.Next(0, productIDs.Count)]),
							SpecialOffer = specialOffer,
							UnitPrice = rand.Next(3, 3579),
							UnitPriceDiscount = 0,
							rowguid = Guid.NewGuid(),
							ModifiedDate = DateTime.Now
						};
					}
					db.SalesOrderDetails.AddRange(newSalesOrderDetails); // добавляем позиции в заказ
					db.SaveChanges();  // сохраняем изменения в БД

					newSalesOrderHeaders[h].SubTotal = newSalesOrderHeaders[h]  // обновляем общую сумму заказа
						.SalesOrderDetails.Sum(d => d.LineTotal);
					// обновляем суммы налога и доставки в заказе
					newSalesOrderHeaders[h].TaxAmt = newSalesOrderHeaders[h].SubTotal * 0.096M;
					newSalesOrderHeaders[h].Freight = newSalesOrderHeaders[h].SubTotal * 0.03M;
					
					db.SaveChanges();  // сохраняем изменения в БД

					Console.WriteLine($"Добавлен заказ:\n{PrintSalesOrder(db, newSalesOrderHeaders[h])}");
				}
			}
			createStopWatch.Stop();

			// 3. Обновляем контактные данные N клиентов
			updateStopWatch.Start();
			using (AdventureContext db = new AdventureContext()) // создаем контекст БД
			{
				// массивы для хранения новых объектов до записи их в БД
				PersonPhone[] newPhones = new PersonPhone[nIterations];
				EmailAddress[] newEmails = new EmailAddress[nIterations];

				for (int u = 0; u < nIterations; ++u)
				{
					Customer customer;  // выбираем случайного клиента с заполненным полем Person
					do
						customer = db.Customers.Find(customerIDs[rand.Next(0, customerIDs.Count)]);
					while (customer.Person == null);

					newPhones[u] = new PersonPhone  // генерируем новый номер телефона
					{
						BusinessEntityID = customer.Person.BusinessEntityID,
						PhoneNumber = rand.Next(1112223344, 2147483647).ToString(),
						PhoneNumberTypeID = rand.Next(1, 4),
						ModifiedDate = DateTime.Now
					};
					//customer.Person.PersonPhones.Add(phone);

					newEmails[u] = new EmailAddress  // генерируем новый email
					{
						BusinessEntityID = customer.Person.BusinessEntityID,
						EmailAddress1 = GenerateRandomString(8, false) + "@" + GenerateRandomString(6, false) + ".com",
						ModifiedDate = DateTime.Now,
						rowguid = Guid.NewGuid()
					};
					//customer.Person.EmailAddresses.Add(email);

					//db.Entry(phone).State = EntityState.Added;
					//db.Entry(email).State = EntityState.Added;
					//db.Entry(customer).State = EntityState.Modified;
					Console.Write($"Обновлен клиент {customer.Person.FirstName} {customer.Person.LastName}. ");
					Console.WriteLine($"Добавлен тел.: {newPhones[u].PhoneNumber}, новый email: {newEmails[u].EmailAddress1}");
				}
				// сохраняем обновленные сущности в БД
				db.PersonPhones.AddRange(newPhones);
				db.EmailAddresses.AddRange(newEmails);
				db.SaveChanges();  // сохраняем изменения в БД
			}
			updateStopWatch.Stop();

			using (AdventureContext db = new AdventureContext()) // создаем контекст БД
			{
				// получаем список сегодняшних заказов
				readStopWatch.Start();
				var ordersToday = db.SalesOrderHeaders
					.Where(o => DbFunctions.TruncateTime(o.OrderDate) == DbFunctions.TruncateTime(DateTime.Now))
					.ToList();
				readStopWatch.Stop();

				// 4. Обновляем данные по N сегодняшних заказов
				updateStopWatch.Start();
				for (int o = 0; o < nIterations; ++o)
				{
					SalesOrderHeader orderToUpdate = ordersToday[rand.Next(0, ordersToday.Count)];
					orderToUpdate.ShipDate = orderToUpdate.OrderDate + new TimeSpan(7, 0, 0, 0);
					orderToUpdate.ModifiedDate = DateTime.Now;
					orderToUpdate.CreditCardID = creditCardIDs[rand.Next(0, creditCardIDs.Count)];
					db.Entry(orderToUpdate).State = EntityState.Modified;
					Console.WriteLine($"Обновлен заказ № {orderToUpdate.SalesOrderNumber}");
				}
				db.SaveChanges();  // сохраняем изменения в БД
				updateStopWatch.Stop();

				// 5. Удаляем N сегодняшних заказов
				deleteStopWatch.Start();
				foreach (var orderToDelete in ordersToday.Take(nIterations))
				{
					// сначала удаляем все позиции заказа
					foreach (SalesOrderDetail orderLine in orderToDelete.SalesOrderDetails.ToList())
						db.Entry(orderLine).State = EntityState.Deleted;

					db.Entry(orderToDelete).State = EntityState.Deleted;  // удаляем сам заказ
					
					Console.WriteLine($"Удален заказ № {orderToDelete.SalesOrderNumber}");
				}
				db.SaveChanges();  // сохраняем изменения в БД
				deleteStopWatch.Stop();
			}

			return new CrudTime(
				createStopWatch.Elapsed,
				readStopWatch.Elapsed,
				updateStopWatch.Elapsed,
				deleteStopWatch.Elapsed);
		}

		/// <summary>
		/// Выводит в консоль полную информацию о заказе из таблиц  
		/// SalesOrderHeader, SalesOrderDetails и других связанных таблиц
		/// </summary>
		/// <param name="db">Контекст модели базы данных</param>
		/// <param name="so">Экземпляр заказа</param>
		/// <returns>Информацию о заказе в виде строки</returns>
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
