using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using static OrmTesting.Utilities;
using System.Diagnostics;

namespace SqlTesting
{
	class SqlTester
	{

		public CrudTime Test()
		{
			CrudTime _elapsedTime = new CrudTime(); // здесь будет суммироваться время, затраченное на операции с БД

			// создаем подключение к БД
			SqlConnection conn = null;

			SqlDataReader salesPersonID = null;
			try
			{
				conn = new SqlConnection();
				conn.ConnectionString = ConfigurationManager.ConnectionStrings["AdventureWorks"].ConnectionString;
				conn.Open();

				// получаем всех продавцов
				SqlCommand selectAllSalesPeopleIDs = new SqlCommand(
					"SELECT BusinessEntityID FROM Sales.SalesPerson",
					conn);
				salesPersonID = selectAllSalesPeopleIDs.ExecuteReader();

				//while (salesPersonID.Read()) // запускаем тесты для каждого из продавцов
				{
					_elapsedTime += SimulateDailyReporting(conn, 282);//(int)salesPersonID[0]);
					_elapsedTime += SimulateDailyOperations(conn, 282, 5); //(int)salesPersonID[0], 5);
				}
			}
			finally
			{
				if (conn != null)  // закрываем соединение
					conn.Close();
				if (salesPersonID != null)
					salesPersonID.Close();
			}

			return _elapsedTime;
		}

		/// <summary>
		/// Симуляция дневной отчетности из БД по отдельно взятому продавцу:
		/// загружает из БД и выводит в консоль полную информацию по всем заказам продавца
		/// на случайно выбранную дату
		/// </summary>
		public CrudTime SimulateDailyReporting(SqlConnection conn, int salesPersonID)
		{
			Random rand = new Random();
			Stopwatch readStopWatch = new Stopwatch();
			readStopWatch.Start();

			// выбираем случайную дату, когда продавец работал
			SqlCommand getRandomDate = new SqlCommand(
				"SELECT TOP 1 OrderDate FROM Sales.SalesOrderHeader WHERE SalesPersonID = "
				+ salesPersonID.ToString() + " ORDER BY NewId()",
				conn);
			DateTime date = (DateTime)getRandomDate.ExecuteScalar();

			SqlCommand getSalesPerson = new SqlCommand(
				@"SELECT FirstName, LastName FROM Sales.SalesPerson sp
				JOIN Person.Person p ON sp.BusinessEntityID = p.BusinessEntityID
				WHERE sp.BusinessEntityID = " + salesPersonID.ToString(),
				conn);
			SqlDataReader salesPerson = getSalesPerson.ExecuteReader();
			salesPerson.Read();
			Console.WriteLine($"Заказы по продавцу {salesPerson[0]} {salesPerson[1]} за {date.ToShortDateString()}");
			//if (salesPerson != null)
			salesPerson.Close();

			// читаем из БД перечень заказов по выбранным продавцу и дате
			SqlCommand getSalesOrders = new SqlCommand(
				"SELECT SalesOrderID FROM Sales.SalesOrderHeader WHERE SalesPersonID = "
				+ salesPersonID.ToString() + " AND OrderDate = @d",
				conn);
			getSalesOrders.Parameters.Add(
				new SqlParameter
				{
					ParameterName = "@d",
					SqlDbType = System.Data.SqlDbType.Date,
					Value = date
				});
			SqlDataReader orders = getSalesOrders.ExecuteReader();

			while (orders.Read()) // выводим на экран полную информацию по всему перечню заказов
				Console.WriteLine(PrintSalesOrder(conn, (int)orders[0]));

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
		public CrudTime SimulateDailyOperations(SqlConnection conn, int salesPersonID, int nIterations)
		{
			// таймеры для подсчета времени, затраченного на операции CRUD (создание, чтение, изменение, удаление) в БД
			Stopwatch createStopWatch = new Stopwatch();
			Stopwatch readStopWatch = new Stopwatch();
			Stopwatch updateStopWatch = new Stopwatch();
			Stopwatch deleteStopWatch = new Stopwatch();

			Random rand = new Random();

			//	readStopWatch.Start();

			//	var store = db.Stores  // получаем магазин, где работает продавец
			//		.Where(s => s.SalesPersonID == sp.BusinessEntityID)
			//		.FirstOrDefault();
			//	if (store == null)  // если магазин отсутствует, создаем новый
			//		store = new Store
			//		{
			//			BusinessEntity = new BusinessEntity
			//			{
			//				ModifiedDate = DateTime.Now,
			//				rowguid = Guid.NewGuid()
			//			},
			//			Name = GenerateRandomString(15),
			//			SalesPerson = sp,
			//			ModifiedDate = DateTime.Now,
			//			rowguid = Guid.NewGuid()
			//		};

			//	var territory = sp.SalesTerritory;  // получаем территорию, где работает продавец
			//	if (territory == null)  // если территория не указана, выбираем первую попавшуюся из БД
			//	{
			//		territory = db.SalesTerritories.First();
			//		sp.SalesTerritory = territory;
			//	}
			//	var country = territory.CountryRegion;
			//	var province = db.StateProvinces
			//		.Where(s => s.TerritoryID == territory.TerritoryID)
			//		.FirstOrDefault();
			//	if (province == null)
			//		province = db.StateProvinces.First();
			//	var cities = db.Addresses
			//		.Where(a => a.StateProvinceID == province.StateProvinceID)
			//		.Select(a => a.City)
			//		.ToList();
			//	if (cities.Count == 0)
			//		cities.Add(GenerateRandomString(6));
			//	readStopWatch.Stop();

			//	// 1. Создаем N новых клиентов случайным образом
			//	createStopWatch.Start();
			//	for (int i = 0; i < nIterations; ++i)
			//	{
			//		Address addr = new Address  // генерируем новый адрес случайным образом
			//		{
			//			AddressLine1 = GenerateRandomAddressLine(),
			//			City = cities[rand.Next(0, cities.Count)],
			//			StateProvince = province,
			//			PostalCode = rand.Next(10000, 1000000).ToString(),
			//			ModifiedDate = DateTime.Now,
			//			rowguid = Guid.NewGuid()
			//		};

			//		// вносим данные в связанные таблицы
			//		BusinessEntity be = new BusinessEntity
			//		{
			//			ModifiedDate = DateTime.Now,
			//			rowguid = Guid.NewGuid()
			//		};
			//		BusinessEntityAddress beAddr = new BusinessEntityAddress
			//		{
			//			BusinessEntity = be,
			//			Address = addr,
			//			AddressTypeID = 4,
			//			ModifiedDate = DateTime.Now,
			//			rowguid = Guid.NewGuid()
			//		};

			//		Person person = new Person  // генерируем нового клиента случайным образом
			//		{
			//			PersonType = "IN",
			//			Title = "Mr.",
			//			FirstName = GenerateRandomString(rand.Next(3, 8)),
			//			LastName = GenerateRandomString(rand.Next(3, 10)),
			//			ModifiedDate = DateTime.Now,
			//			rowguid = Guid.NewGuid(),
			//			BusinessEntity = be
			//		};
			//		Customer customer = new Customer
			//		{
			//			Person = person,
			//			Store = store,
			//			SalesTerritory = territory,
			//			ModifiedDate = DateTime.Now,
			//			rowguid = Guid.NewGuid()
			//		};

			//		// добавляем контактные данные
			//		PersonPhone phone = new PersonPhone
			//		{
			//			BusinessEntityID = customer.Person.BusinessEntityID,
			//			PhoneNumber = rand.Next(1112223344, 2147483647).ToString(),
			//			PhoneNumberTypeID = rand.Next(1, 4),
			//			ModifiedDate = DateTime.Now
			//		};
			//		EmailAddress email = new EmailAddress
			//		{
			//			BusinessEntityID = customer.Person.BusinessEntityID,
			//			EmailAddress1 = GenerateRandomString(8) + "@" + GenerateRandomString(6) + ".com",
			//			ModifiedDate = DateTime.Now,
			//			rowguid = Guid.NewGuid()
			//		};
			//		customer.Person.PersonPhones.Add(phone);
			//		customer.Person.EmailAddresses.Add(email);

			//		// добавляем все созданные сущности в БД
			//		db.Entry(addr).State = EntityState.Added;
			//		db.Entry(be).State = EntityState.Added;
			//		db.Entry(beAddr).State = EntityState.Added;
			//		db.Entry(person).State = EntityState.Added;
			//		db.Entry(customer).State = EntityState.Added;
			//		db.Entry(phone).State = EntityState.Added;
			//		db.Entry(email).State = EntityState.Added;
			//		db.SaveChanges();  // сохраняем изменения в БД

			//		Console.WriteLine($"Добавлен клиент {person.FirstName} {person.LastName} с адресом:");
			//		Console.WriteLine(addr);
			//		Console.WriteLine($"Тел.: {phone.PhoneNumber}, Email: {email.EmailAddress1}");
			//	}
			//	createStopWatch.Stop();

			//	readStopWatch.Start();
			//	var customers = db.Customers  // получаем список клиентов магазина
			//		.Where(c => c.StoreID == store.BusinessEntityID)
			//		.ToList();
			//	var products = db.Products.ToList();  // получаем список всех продуктов
			//	var creditCards = db.CreditCards.ToList();  // получаем список всех кредитных карт
			//	readStopWatch.Stop();

			//	// 2. Создаем N новых заказов случайным образом
			//	createStopWatch.Start();
			//	for (int h = 0; h < nIterations; ++h)
			//	{
			//		Customer customer = customers[rand.Next(0, customers.Count)]; // выбираем случайного клиента
			//		Address addr = new Address  // генерируем новый адрес случайным образом
			//		{
			//			AddressLine1 = GenerateRandomAddressLine(),
			//			City = cities[rand.Next(0, cities.Count)],
			//			StateProvince = province,
			//			PostalCode = rand.Next(10000, 1000000).ToString(),
			//			ModifiedDate = DateTime.Now,
			//			rowguid = Guid.NewGuid()
			//		};
			//		SalesOrderHeader soHeader = new SalesOrderHeader
			//		{
			//			RevisionNumber = 8,
			//			OrderDate = DateTime.Now,
			//			DueDate = DateTime.Now + new TimeSpan(14, 0, 0, 0),
			//			Status = 5,
			//			OnlineOrderFlag = true,
			//			Customer = customer,
			//			SalesPerson = sp,
			//			SalesTerritory = territory,
			//			BillToAddress = addr,
			//			ShipToAddress = addr,
			//			ShipMethodID = rand.Next(1, 6),
			//			CreditCard = creditCards[rand.Next(0, creditCards.Count)],
			//			CreditCardApprovalCode = rand.Next(1000000000, 2147483647).ToString(),
			//			SubTotal = 0,
			//			TaxAmt = 0,
			//			Freight = 0,
			//			TotalDue = 0,
			//			rowguid = Guid.NewGuid(),
			//			ModifiedDate = DateTime.Now
			//		};
			//		// добавляем все созданные сущности в БД
			//		db.Entry(addr).State = EntityState.Added;
			//		db.Entry(soHeader).State = EntityState.Added;
			//		db.SaveChanges();  // сохраняем изменения в БД

			//		// генерируем от 1 до 15 случайных позиций в заказе
			//		int linesCount = rand.Next(1, 17);
			//		for (int d = 0; d < linesCount; ++d)
			//		{
			//			Product product = products[rand.Next(0, products.Count)];
			//			SpecialOffer specialOffer = db.SpecialOffers.Find(1);
			//			SalesOrderDetail soDetail = new SalesOrderDetail
			//			{
			//				SalesOrderID = soHeader.SalesOrderID,
			//				OrderQty = (short)rand.Next(1, 8),
			//				Product = products[rand.Next(0, products.Count)],
			//				SpecialOffer = specialOffer,
			//				UnitPrice = rand.Next(3, 3579),
			//				UnitPriceDiscount = 0,
			//				rowguid = Guid.NewGuid(),
			//				ModifiedDate = DateTime.Now
			//			};
			//			db.Entry(soDetail).State = EntityState.Added;
			//			soHeader.SalesOrderDetails.Add(soDetail);  // добавляем позицию в заказ
			//			soHeader.SubTotal += soDetail.LineTotal;  // обновляем общую сумму заказа
			//			db.SaveChanges();  // сохраняем изменения в БД
			//		}
			//		// обновляем суммы налога и доставки в заказе
			//		soHeader.TaxAmt = soHeader.SubTotal * 0.096M;
			//		soHeader.Freight = soHeader.SubTotal * 0.03M;
			//		db.SaveChanges();  // сохраняем изменения в БД

			//		Console.WriteLine($"Добавлен заказ:\n{PrintSalesOrder(db, soHeader)}");
			//	}
			//	createStopWatch.Stop();

			//	// 3. Обновляем контактные данные N клиентов
			//	updateStopWatch.Start();
			//	for (int i = 0; i < nIterations; ++i)
			//	{
			//		Customer customer = customers[rand.Next(0, customers.Count)];  // выбираем случайного клиента

			//		PersonPhone phone = new PersonPhone  // генерируем новый номер телефона
			//		{
			//			BusinessEntityID = customer.Person.BusinessEntityID,
			//			PhoneNumber = rand.Next(1112223344, 2147483647).ToString(),
			//			PhoneNumberTypeID = rand.Next(1, 4),
			//			ModifiedDate = DateTime.Now
			//		};
			//		customer.Person.PersonPhones.Add(phone);

			//		EmailAddress email = new EmailAddress  // генерируем новый email
			//		{
			//			BusinessEntityID = customer.Person.BusinessEntityID,
			//			EmailAddress1 = GenerateRandomString(8) + "@" + GenerateRandomString(6) + ".com",
			//			ModifiedDate = DateTime.Now,
			//			rowguid = Guid.NewGuid()
			//		};
			//		customer.Person.EmailAddresses.Add(email);

			//		// сохраняем обновленные сущности в БД
			//		db.Entry(phone).State = EntityState.Added;
			//		db.Entry(email).State = EntityState.Added;
			//		db.Entry(customer).State = EntityState.Modified;
			//		db.SaveChanges();  // сохраняем изменения в БД
			//		Console.Write($"Обновлен клиент {customer.Person.FirstName} {customer.Person.LastName}. ");
			//		Console.WriteLine($"Новый тел.: {phone.PhoneNumber}, новый email: {email.EmailAddress1}");
			//	}
			//	updateStopWatch.Stop();

			//	readStopWatch.Start();
			//	// получаем список сегодняшних заказов
			//	var ordersToday = db.SalesOrderHeaders
			//		.Where(o => DbFunctions.TruncateTime(o.OrderDate) == DbFunctions.TruncateTime(DateTime.Now))
			//		.ToList();
			//	readStopWatch.Stop();

			//	// 4. Обновляем данные по N сегодняшних заказов
			//	updateStopWatch.Start();
			//	for (int o = 0; o < nIterations; ++o)
			//	{
			//		SalesOrderHeader orderToUpdate = ordersToday[rand.Next(0, ordersToday.Count)];
			//		orderToUpdate.ShipDate = orderToUpdate.OrderDate + new TimeSpan(7, 0, 0, 0);
			//		orderToUpdate.ModifiedDate = DateTime.Now;
			//		orderToUpdate.CreditCard = creditCards[rand.Next(0, creditCards.Count)];

			//		db.Entry(orderToUpdate).State = EntityState.Modified;
			//		db.SaveChanges();  // сохраняем изменения в БД
			//		Console.WriteLine($"Обновлен заказ № {orderToUpdate.SalesOrderNumber}");
			//	}
			//	updateStopWatch.Stop();

			//	// 5. Удаляем N сегодняшних заказов
			//	deleteStopWatch.Start();
			//	foreach (var orderToDelete in ordersToday.Take(nIterations))
			//	{
			//		// сначала удаляем все позиции заказа
			//		foreach (SalesOrderDetail orderLine in orderToDelete.SalesOrderDetails)
			//			db.Entry(orderLine).State = EntityState.Deleted;

			//		db.Entry(orderToDelete).State = EntityState.Deleted;  // удаляем сам заказ
			//		db.SaveChanges();  // сохраняем изменения в БД
			//		Console.WriteLine($"Удален заказ № {orderToDelete.SalesOrderNumber}");
			//	}
			//	deleteStopWatch.Stop();


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
		/// <returns>Информацию о заказе в виде строки</returns>
		public string PrintSalesOrder(SqlConnection conn, int salesOrderID)
		{
			// читаем заголовок заказа
			SqlCommand getSalesOrderHeader = new SqlCommand(
				@"SELECT so.SalesOrderNumber [Номер заказа]
				,so.PurchaseOrderNumber [Заказ на поставку]
				,so.AccountNumber [Номер счета]
				,so.OrderDate [Дата заказа]
				,so.DueDate [Срок поставки]
				,so.ShipDate [Дата поставки]
				,so.OnlineOrderFlag [Заказ сделан онлайн]
				,territory.Name [Территория]
				,customer.Title [Клиент]
				,customer.FirstName [Имя]
				,customer.LastName [Фамилия]
				,customer_phone.PhoneNumber [Номер телефона]
				,customer_phone_type.Name [Вид номера]
				,customer_email.EmailAddress [Email]
				,sales_person.FirstName [Имя продавца]
				,sales_person.LastName [Фамилия продавца]
				,store.Name [Название магазина]
				,store_addr_type.Name [Адрес магазина]
				,store_addr.AddressLine1 [Адрес магазина (1)]
				,store_addr.AddressLine2 [Адрес магазина (2)]
				,store_addr.City [Город магазина]
				,store_province.Name [Провинция/штат магазина]
				,store_addr.PostalCode [Индекс магазина]
				,store_country.Name [Страна магазина]
				,ship_addr.AddressLine1 [Адрес доставки (1)]
				,ship_addr.AddressLine2 [Адрес доставки (2)]
				,ship_addr.City [Город доставки]
				,ship_province.Name  [Провинция/штат доставки]
				,ship_addr.PostalCode  [Индекс доставки]
				,ship_country.Name  [Страна доставки]
				,ship_method.Name  [Метод доставки]
				,so.SubTotal [Подитог]
				,so.TaxAmt [Налог]
				,so.Freight [Стоимость доставки]
				,so.TotalDue [Всего к оплате]
				,currency.CurrencyCode  [Валюта]
				,credit_card.CardType  [Тип кредитной карты]
				,so.CreditCardApprovalCode [Код подтверждения]
				,bill_addr.AddressLine1 [Адрес оплаты (1)]
				,bill_addr.AddressLine2 [Адрес оплаты (2)]
				,bill_addr.City  [Город оплаты]
				,bill_province.Name  [Провинция/штат оплаты]
				,bill_addr.PostalCode  [Индекс оплаты]
				,bill_country.Name  [Страна оплаты]
				FROM Sales.SalesOrderHeader so
				LEFT JOIN Sales.Customer cust ON so.CustomerID = cust.CustomerID
				LEFT JOIN Person.Person customer ON cust.PersonID = customer.BusinessEntityID
				LEFT JOIN Person.PersonPhone customer_phone ON customer.BusinessEntityID = customer_phone.BusinessEntityID
				LEFT JOIN Person.PhoneNumberType customer_phone_type ON customer_phone.PhoneNumberTypeID = customer_phone_type.PhoneNumberTypeID
				LEFT JOIN Person.EmailAddress customer_email ON customer.BusinessEntityID = customer_email.BusinessEntityID
				LEFT JOIN Sales.Store store ON cust.StoreID = store.BusinessEntityID
				LEFT JOIN Person.BusinessEntityAddress store_be_addr ON store.BusinessEntityID = store_be_addr.BusinessEntityID
				LEFT JOIN Person.Address store_addr ON store_be_addr.AddressID = store_addr.AddressID
				LEFT JOIN Person.StateProvince store_province ON store_addr.StateProvinceID = store_province.StateProvinceID
				LEFT JOIN Person.CountryRegion store_country ON store_province.CountryRegionCode = store_country.CountryRegionCode
				LEFT JOIN Person.AddressType store_addr_type ON store_be_addr.AddressTypeID = store_addr_type.AddressTypeID
				LEFT JOIN Person.Person sales_person ON so.SalesPersonID = sales_person.BusinessEntityID
				LEFT JOIN Sales.CreditCard credit_card ON so.CreditCardID = credit_card.CreditCardID
				LEFT JOIN Sales.SalesTerritory territory ON so.TerritoryID = territory.TerritoryID
				LEFT JOIN Sales.CountryRegionCurrency currency ON territory.CountryRegionCode = currency.CountryRegionCode 
				LEFT JOIN Person.Address bill_addr ON so.BillToAddressID = bill_addr.AddressID
				LEFT JOIN Person.StateProvince bill_province ON bill_addr.StateProvinceID = bill_province.StateProvinceID
				LEFT JOIN Person.CountryRegion bill_country ON bill_province.CountryRegionCode = bill_country.CountryRegionCode
				LEFT JOIN Person.Address ship_addr ON so.ShipToAddressID = ship_addr.AddressID
				LEFT JOIN Person.StateProvince ship_province ON ship_addr.StateProvinceID = ship_province.StateProvinceID
				LEFT JOIN Person.CountryRegion ship_country ON ship_province.CountryRegionCode = ship_country.CountryRegionCode
				LEFT JOIN Purchasing.ShipMethod ship_method ON so.ShipMethodID = ship_method.ShipMethodID
				WHERE SalesOrderID = " + salesOrderID.ToString(),
				conn);
			SqlDataReader orderHeader = getSalesOrderHeader.ExecuteReader();
			StringBuilder sbOrder = new StringBuilder();
			while (orderHeader.Read())
			{
				for (int i = 0; i < orderHeader.FieldCount; i++)
					sbOrder.Append($"{orderHeader.GetName(i), -25}: {orderHeader[i]}\n");
			}

			// читаем состав заказа
			SqlCommand getSalesOrderDetails = new SqlCommand(
				@"SELECT product.ProductNumber
				,product.Name ProductName
				,so.UnitPrice
				,so.OrderQty
				,so.UnitPriceDiscount
				,so.LineTotal
				,special.Description
				FROM Sales.SalesOrderDetail so
				LEFT JOIN Production.Product product ON so.ProductID = product.ProductID
				LEFT JOIN Sales.SpecialOffer special ON so.SpecialOfferID = special.SpecialOfferID
				WHERE SalesOrderID = " + salesOrderID.ToString(),
				conn);
			SqlDataReader orderDetails = getSalesOrderDetails.ExecuteReader();
			sbOrder.Append("Состав заказа:\n");
			while (orderDetails.Read())
			{
				sbOrder.Append($"{orderDetails[0],-10} {orderDetails[1],-35} ");
				sbOrder.Append($"{orderDetails[2],8:F2} * {orderDetails[3],2} -{orderDetails[4],3:P0} = {orderDetails[5],8:F2} ");
				sbOrder.Append($"({orderDetails[6]})\n");
			}

			return sbOrder.ToString();
		}
	}
}
