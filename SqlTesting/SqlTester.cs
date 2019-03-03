using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using static OrmTesting.Utilities;
using System.Diagnostics;
using System.Data;

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
					_elapsedTime += SimulateDailyReporting(conn, 287);// (int)salesPersonID[0]);
					_elapsedTime += SimulateDailyOperations(conn, 287, 3);// (int)salesPersonID[0], 3);
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
			SqlCommand selectRandomDate = new SqlCommand(
				@"SELECT TOP 1 OrderDate FROM Sales.SalesOrderHeader 
				WHERE SalesPersonID = @sp
				ORDER BY NewId()",
				conn);
			selectRandomDate.Parameters.AddWithValue("@sp", salesPersonID);
			DateTime date = (DateTime)selectRandomDate.ExecuteScalar();

			SqlCommand selectSalesPerson = new SqlCommand(
				@"SELECT FirstName, LastName FROM Sales.SalesPerson sp
				JOIN Person.Person p ON sp.BusinessEntityID = p.BusinessEntityID
				WHERE sp.BusinessEntityID = @sp",
				conn);
			selectSalesPerson.Parameters.AddWithValue("@sp", salesPersonID);
			SqlDataReader salesPerson = selectSalesPerson.ExecuteReader();
			salesPerson.Read();
			Console.WriteLine($"Заказы по продавцу {salesPerson[0]} {salesPerson[1]} за {date.ToShortDateString()}");
			//if (salesPerson != null)
			salesPerson.Close();

			// читаем из БД перечень заказов по выбранным продавцу и дате
			SqlCommand selectSalesOrders = new SqlCommand(
				@"SELECT SalesOrderID FROM Sales.SalesOrderHeader 
				WHERE SalesPersonID = @sp AND DATEDIFF(dd, OrderDate, @d) = 0",
				conn);
			selectSalesOrders.Parameters.AddWithValue("@sp", salesPersonID);
			selectSalesOrders.Parameters.Add("@d", SqlDbType.Date).Value = date;
			SqlDataReader orders = selectSalesOrders.ExecuteReader();

			while (orders.Read()) // выводим на экран полную информацию по всему перечню заказов
				Console.WriteLine(PrintSalesOrder(conn, (int)orders[0]));
			orders.Close();

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

			readStopWatch.Start();

			// получаем один из магазинов, где работает продавец
			SqlCommand selectStore = new SqlCommand(
				@"SELECT TOP 1 BusinessEntityID FROM Sales.Store
				WHERE SalesPersonID = @sp
				ORDER BY NewId()",
				conn);
			selectStore.Parameters.AddWithValue("@sp", salesPersonID);
			int? storeID = (int)selectStore.ExecuteScalar();

			//if (storeID == null)  // если магазин отсутствует, создаем новый
			//{
			//	readStopWatch.Stop();
			//	createStopWatch.Start();
			//	SqlCommand insertBusinessEntity = new SqlCommand(
			//		@"INSERT INTO Person.BusinessEntity 
			//		OUTPUT inserted.BusinessEntityID
			//		DEFAULT VALUES",
			//		conn);
			//	storeID = (int)insertBusinessEntity.ExecuteScalar();
			//	SqlCommand insertStore = new SqlCommand(
			//		@"INSERT INTO Sales.Store (BusinessEntityID, Name, SalesPersonID)
			//		VALUES (@be, @name, @sp)",
			//		conn);
			//	insertStore.Parameters.AddWithValue("@be", storeID);
			//	insertStore.Parameters.AddWithValue("@name", GenerateRandomString(15));
			//	insertStore.Parameters.AddWithValue("@sp", salesPersonID);
			//	insertStore.ExecuteNonQuery();
			//	createStopWatch.Stop();
			//	readStopWatch.Start();
			//}

			// получаем территорию, страну, штат/провинцию
			SqlCommand selectTerritoryCountry = new SqlCommand(
				@"SELECT TOP 1 sp.TerritoryID, terr.CountryRegionCode, pro.StateProvinceID
				FROM Sales.SalesPerson sp
				JOIN Sales.SalesTerritory terr ON terr.TerritoryID = sp.TerritoryID
				JOIN Person.StateProvince pro ON terr.CountryRegionCode = pro.CountryRegionCode
				WHERE sp.BusinessEntityID = @sp
				ORDER BY NEWID()",
				conn);
			selectTerritoryCountry.Parameters.AddWithValue("@sp", salesPersonID);
			SqlDataReader territoryReader = selectTerritoryCountry.ExecuteReader();
			territoryReader.Read();
			int territoryID = (int)territoryReader[0];
			string countryRegionCode = (string)territoryReader[1];
			int provinceID = (int)territoryReader[2];
			territoryReader.Close();

			// получаем список городов
			SqlCommand selectCities = new SqlCommand(
				@"SELECT City FROM Person.Address
				WHERE StateProvinceID = @prov",
				conn);
			selectCities.Parameters.AddWithValue("@prov", provinceID);
			SqlDataReader citiesReader = selectCities.ExecuteReader();
			List<string> cities = new List<string>();
			while (citiesReader.Read())
				cities.Add((string)citiesReader[0]);
			citiesReader.Close();
			if (cities.Count < 1)
				cities.Add(GenerateRandomString(7));

			readStopWatch.Stop();

			// 1. Создаем N новых клиентов случайным образом
			createStopWatch.Start();
			for (int i = 0; i < nIterations; ++i)
			{
				// генерируем новый адрес случайным образом
				string addressLine = GenerateRandomAddressLine();
				string city = cities[rand.Next(0, cities.Count)];
				string postCode = rand.Next(10000, 1000000).ToString();
				SqlCommand insertAddress = new SqlCommand(
					@"INSERT INTO Person.Address (AddressLine1, City, StateProvinceID, PostalCode)
					OUTPUT inserted.AddressID
					VALUES (@addr1, @city, @prov, @post)",
					conn);
				insertAddress.Parameters.AddWithValue("@addr1", addressLine);
				insertAddress.Parameters.AddWithValue("@city", city);
				insertAddress.Parameters.AddWithValue("@prov", provinceID);
				insertAddress.Parameters.AddWithValue("@post", postCode);
				int addressID = (int)insertAddress.ExecuteScalar();

				// вносим данные в связанные таблицы
				SqlCommand insertBusinessEntity = new SqlCommand(
					@"INSERT INTO Person.BusinessEntity 
					OUTPUT inserted.BusinessEntityID
					DEFAULT VALUES",
					conn);
				int beID = (int)insertBusinessEntity.ExecuteScalar();

				SqlCommand insertBusinessEntityAddress = new SqlCommand(
					@"INSERT INTO Person.BusinessEntityAddress (BusinessEntityID, AddressID, AddressTypeID)
					VALUES (@be, @addr, @addrtype)",
					conn);
				insertBusinessEntityAddress.Parameters.AddWithValue("@be", beID);
				insertBusinessEntityAddress.Parameters.AddWithValue("@addr", addressID);
				insertBusinessEntityAddress.Parameters.AddWithValue("@addrtype", 4);
				insertBusinessEntityAddress.ExecuteNonQuery();

				// генерируем нового клиента случайным образом
				string firstName = GenerateRandomString(rand.Next(3, 8));
				string lastName = GenerateRandomString(rand.Next(3, 10));
				SqlCommand insertPerson = new SqlCommand(
					@"INSERT INTO Person.Person (BusinessEntityID, PersonType, Title, FirstName, LastName)
					VALUES (@be, @type, @title, @firstn, @lastn)",
					conn);
				insertPerson.Parameters.AddWithValue("@be", beID);
				insertPerson.Parameters.AddWithValue("@type", "IN");
				insertPerson.Parameters.AddWithValue("@title", "Mr.");
				insertPerson.Parameters.AddWithValue("@firstn", firstName);
				insertPerson.Parameters.AddWithValue("@lastn", lastName);
				insertPerson.ExecuteNonQuery();

				SqlCommand insertCustomer = new SqlCommand(
					@"INSERT INTO Sales.Customer (PersonID, StoreID, TerritoryID)
					VALUES (@per, @st, @terr)",
					conn);
				insertCustomer.Parameters.AddWithValue("@per", beID);
				insertCustomer.Parameters.AddWithValue("@st", storeID);
				insertCustomer.Parameters.AddWithValue("@terr", territoryID);
				insertCustomer.ExecuteNonQuery();

				// добавляем контактные данные
				string phoneNumber = rand.Next(1112223344, 2147483647).ToString();
				string email = GenerateRandomString(8, false) + "@" + GenerateRandomString(6, false) + ".com";

				SqlCommand insertPhone = new SqlCommand(
					@"INSERT INTO Person.PersonPhone (BusinessEntityID, PhoneNumber, PhoneNumberTypeID)
					VALUES (@be, @ph, @type)",
					conn);
				insertPhone.Parameters.AddWithValue("@be", beID);
				insertPhone.Parameters.AddWithValue("@ph", phoneNumber);
				insertPhone.Parameters.AddWithValue("@type", rand.Next(1, 4));
				insertPhone.ExecuteNonQuery();

				SqlCommand insertEmail = new SqlCommand(
					@"INSERT INTO Person.EmailAddress (BusinessEntityID, EmailAddress)
					VALUES (@be, @email)",
					conn);
				insertEmail.Parameters.AddWithValue("@be", beID);
				insertEmail.Parameters.AddWithValue("@email", email);
				insertEmail.ExecuteNonQuery();

				Console.WriteLine($"Добавлен клиент {firstName} {lastName} с адресом:");
				Console.WriteLine($"{addressLine}\n{city} {postCode}");
				Console.WriteLine($"Тел.: {phoneNumber}, Email: {email}");
			}
			createStopWatch.Stop();

			readStopWatch.Start();
			// получаем список клиентов магазина
			SqlCommand selectCustomers = new SqlCommand(
				@"SELECT CustomerID FROM Sales.Customer
				WHERE StoreID = @s",
				conn);
			selectCustomers.Parameters.AddWithValue("@s", storeID);
			SqlDataReader customersReader = selectCustomers.ExecuteReader();
			List<int> customers = new List<int>();
			while (customersReader.Read())
				customers.Add((int)customersReader[0]);
			customersReader.Close();

			// получаем список всех продуктов
			SqlCommand selectProducts = new SqlCommand(
				@"SELECT ProductID FROM Production.Product",
				conn);
			SqlDataReader productsReader = selectProducts.ExecuteReader();
			List<int> allProducts = new List<int>();
			while (productsReader.Read())
				allProducts.Add((int)productsReader[0]);
			productsReader.Close();

			// получаем список всех кредитных карт
			SqlCommand selectCreditCards = new SqlCommand(
				@"SELECT CreditCardID FROM Sales.CreditCard",
				conn);
			SqlDataReader creditCardsReader = selectCreditCards.ExecuteReader();
			List<int> allCreditCards = new List<int>();
			while (creditCardsReader.Read())
				allCreditCards.Add((int)creditCardsReader[0]);
			productsReader.Close();

			readStopWatch.Stop();

			// 2. Создаем N новых заказов случайным образом
			createStopWatch.Start();
			for (int h = 0; h < nIterations; ++h)
			{
				int customerID = customers[rand.Next(0, customers.Count)]; // выбираем случайного клиента
				
				// генерируем новый адрес случайным образом
				string addressLine = GenerateRandomAddressLine();
				string city = cities[rand.Next(0, cities.Count)];
				string postCode = rand.Next(10000, 1000000).ToString();
				SqlCommand insertAddress = new SqlCommand(
					@"INSERT INTO Person.Address (AddressLine1, City, StateProvinceID, PostalCode)
					OUTPUT inserted.AddressID
					VALUES (@addr1, @city, @prov, @post)",
					conn);
				insertAddress.Parameters.AddWithValue("@addr1", addressLine);
				insertAddress.Parameters.AddWithValue("@city", city);
				insertAddress.Parameters.AddWithValue("@prov", provinceID);
				insertAddress.Parameters.AddWithValue("@post", postCode);
				int addressID = (int)insertAddress.ExecuteScalar();

				// генерируем заголовок заказа
				SqlCommand insertOrderHeader = new SqlCommand(
					@"INSERT INTO Sales.SalesOrderHeader 
					(RevisionNumber, OrderDate, DueDate, Status, OnlineOrderFlag, 
						CustomerID, SalesPersonID, TerritoryID, BillToAddressID, ShipToAddressID, 
						ShipMethodID, CreditCardID, CreditCardApprovalCode, 
						SubTotal, TaxAmt, Freight)
					OUTPUT inserted.SalesOrderID
					VALUES (8, @date, @due, 5, 1, @cus, @sp, @terr, @billto, 
						@shipto, @shipm, @cc, @ccappr, 0, 0, 0)",
					conn);
				insertOrderHeader.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now;
				insertOrderHeader.Parameters.Add("@due", SqlDbType.DateTime).Value = DateTime.Now + new TimeSpan(14, 0, 0, 0);
				insertOrderHeader.Parameters.AddWithValue("@cus", customerID);
				insertOrderHeader.Parameters.AddWithValue("@sp", salesPersonID);
				insertOrderHeader.Parameters.AddWithValue("@terr", territoryID);
				insertOrderHeader.Parameters.AddWithValue("@billto", addressID);
				insertOrderHeader.Parameters.AddWithValue("@shipto", addressID);
				insertOrderHeader.Parameters.AddWithValue("@shipm", rand.Next(1, 6));
				insertOrderHeader.Parameters.AddWithValue("@cc", allCreditCards[rand.Next(0, allCreditCards.Count)]);
				insertOrderHeader.Parameters.AddWithValue("@ccappr", rand.Next(1000000000, 2147483647).ToString());
				int orderID = (int)insertOrderHeader.ExecuteScalar();

				// генерируем от 1 до 15 случайных позиций в заказе
				int linesCount = rand.Next(1, 17);
				for (int d = 0; d < linesCount; ++d)
				{
					SqlCommand insertOrderDetail = new SqlCommand(
						@"DECLARE @t TABLE (SodID int)
						INSERT INTO Sales.SalesOrderDetail
						(SalesOrderID, OrderQty, ProductID, SpecialOfferID, UnitPrice, UnitPriceDiscount)
						OUTPUT inserted.SalesOrderDetailID INTO @t
						VALUES(@so, @qty, @prod, 1, @price, 0)
						SELECT * FROM @t",
						conn);
					insertOrderDetail.Parameters.AddWithValue("@so", orderID);
					insertOrderDetail.Parameters.AddWithValue("@qty", (short)rand.Next(1, 8));
					insertOrderDetail.Parameters.AddWithValue("@prod", allProducts[rand.Next(0, allProducts.Count)]);
					insertOrderDetail.Parameters.AddWithValue("@price", rand.Next(3, 3579));
					int orderDetailID = (int)insertOrderDetail.ExecuteScalar();

					// обновляем общую сумму заказа
					SqlCommand selectLineTotal = new SqlCommand(
						@"SELECT LineTotal FROM Sales.SalesOrderDetail
						WHERE SalesOrderDetailID = @sod",
						conn);
					selectLineTotal.Parameters.AddWithValue("@sod", orderDetailID);
					decimal lineTotal = (decimal)selectLineTotal.ExecuteScalar();

					SqlCommand selectSubTotal = new SqlCommand(
						@"SELECT SubTotal FROM Sales.SalesOrderHeader
						WHERE SalesOrderID = @so",
						conn);
					selectSubTotal.Parameters.AddWithValue("@so", orderID);
					decimal subTotal = (decimal)selectSubTotal.ExecuteScalar();

					SqlCommand updateSubTotal = new SqlCommand(
						@"UPDATE Sales.SalesOrderHeader
						SET SubTotal = @st
						WHERE SalesOrderID = @so",
						conn);
					updateSubTotal.Parameters.AddWithValue("@st", subTotal + lineTotal);
					updateSubTotal.Parameters.AddWithValue("@so", orderID);
					updateSubTotal.ExecuteNonQuery();
				}

				// обновляем суммы налога и доставки в заказе
				SqlCommand selectFinalSubTotal = new SqlCommand(
					@"SELECT SubTotal FROM Sales.SalesOrderHeader
					WHERE SalesOrderID = @so",
					conn);
				selectFinalSubTotal.Parameters.AddWithValue("@so", orderID);
				decimal finalSubTotal = (decimal)selectFinalSubTotal.ExecuteScalar();

				SqlCommand updateTaxFreight = new SqlCommand(
					@"UPDATE Sales.SalesOrderHeader
					SET TaxAmt = @st,
						Freight = @f
					WHERE SalesOrderID = @so",
					conn);
				updateTaxFreight.Parameters.AddWithValue("@st", finalSubTotal * 0.096M);
				updateTaxFreight.Parameters.AddWithValue("@f", finalSubTotal * 0.03M);
				updateTaxFreight.Parameters.AddWithValue("@so", orderID);
				updateTaxFreight.ExecuteNonQuery();

				Console.WriteLine($"Добавлен заказ:\n{PrintSalesOrder(conn, orderID)}");
			}
			createStopWatch.Stop();

			// 3. Обновляем контактные данные N клиентов
			updateStopWatch.Start();
			for (int i = 0; i < nIterations; ++i)
			{
				int customerID = customers[rand.Next(0, customers.Count)]; // выбираем случайного клиента

				// генерируем новый номер телефона и email
				string phoneNumber = rand.Next(1112223344, 2147483647).ToString();
				string email = GenerateRandomString(8, false) + "@" + GenerateRandomString(6, false) + ".com";

				SqlCommand updatePhone = new SqlCommand(
					@"UPDATE Person.PersonPhone 
					SET PhoneNumber = @ph
					WHERE BusinessEntityID = @be",
					conn);
				updatePhone.Parameters.AddWithValue("@be", customerID);
				updatePhone.Parameters.AddWithValue("@ph", phoneNumber);
				updatePhone.ExecuteNonQuery();

				SqlCommand updateEmail = new SqlCommand(
					@"UPDATE Person.EmailAddress 
					SET EmailAddress = @email
					WHERE BusinessEntityID = @be",
					conn);
				updateEmail.Parameters.AddWithValue("@be", customerID);
				updateEmail.Parameters.AddWithValue("@email", email);
				updateEmail.ExecuteNonQuery();

				// получаем имя клиента
				SqlCommand selectName = new SqlCommand(
					@"SELECT p.FirstName, p.LastName
					FROM Sales.Customer c
					JOIN Person.Person p ON c.PersonID = p.BusinessEntityID
					WHERE CustomerID =@cust",
					conn);
				selectName.Parameters.AddWithValue("@cust", customerID);
				SqlDataReader nameReader = selectName.ExecuteReader();
				nameReader.Read();
				string firstName = (string)nameReader[0];
				string lastName = (string)nameReader[1];
				nameReader.Close();

				Console.Write($"Обновлен клиент {firstName} {lastName}. ");
				Console.WriteLine($"Новый тел.: {phoneNumber}, новый email: {email}");
			}
			updateStopWatch.Stop();

			readStopWatch.Start();
			// получаем список сегодняшних заказов
			SqlCommand selectTodaysOrders = new SqlCommand(
				@"SELECT SalesOrderID
				FROM Sales.SalesOrderHeader
				WHERE DATEDIFF(dd, OrderDate, GETDATE()) = 0",
				conn);
			SqlDataReader todayOrdersReader = selectTodaysOrders.ExecuteReader();
			List<int> todaysOrders = new List<int>();
			while (todayOrdersReader.Read())
				todaysOrders.Add((int)todayOrdersReader[0]);
			todayOrdersReader.Close();
			readStopWatch.Stop();

			// 4. Обновляем данные по N сегодняшних заказов
			updateStopWatch.Start();
			for (int o = 0; o < nIterations; ++o)
			{
				int orderToUpdate = todaysOrders[rand.Next(0, todaysOrders.Count)];
				SqlCommand updateOrder = new SqlCommand(
					@"UPDATE Sales.SalesOrderHeader
					SET ShipDate = DATEADD(dd, 7, OrderDate),
						CreditCardID = @cc
					WHERE SalesOrderID = @so",
					conn);
				updateOrder.Parameters.AddWithValue("@cc", allCreditCards[rand.Next(0, allCreditCards.Count)]);
				updateOrder.Parameters.AddWithValue("@so", orderToUpdate);
				updateOrder.ExecuteNonQuery();
				Console.WriteLine($"Обновлен заказ № {orderToUpdate}");
			}
			updateStopWatch.Stop();

			// 5. Удаляем N сегодняшних заказов
			deleteStopWatch.Start();
			foreach (var orderToDelete in todaysOrders.Take(nIterations))
			{
				// удаляем все позиции заказа и затем сам заказ
				SqlCommand deleteOrder = new SqlCommand(
					@"DELETE FROM Sales.SalesOrderDetail
					WHERE SalesOrderID = @so
					DELETE FROM Sales.SalesOrderHeader
					WHERE SalesOrderID = @so",
					conn);
				deleteOrder.Parameters.AddWithValue("@so", orderToDelete);
				deleteOrder.ExecuteNonQuery();
				Console.WriteLine($"Удален заказ № {orderToDelete}");
			}
			deleteStopWatch.Stop();

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
					sbOrder.Append($"{orderHeader.GetName(i),-25}: {orderHeader[i]}\n");
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
