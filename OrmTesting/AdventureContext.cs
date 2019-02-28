namespace OrmTesting
{
	using System;
	using System.Data.Entity;
	using System.ComponentModel.DataAnnotations.Schema;
	using System.Linq;

	public partial class AdventureContext : DbContext
	{
		public AdventureContext() : base("name=AdventureWorks")
		{
		}

		public virtual DbSet<Address> Addresses { get; set; }
		public virtual DbSet<AddressType> AddressTypes { get; set; }
		public virtual DbSet<BusinessEntity> BusinessEntities { get; set; }
		public virtual DbSet<BusinessEntityAddress> BusinessEntityAddresses { get; set; }
		public virtual DbSet<CountryRegion> CountryRegions { get; set; }
		public virtual DbSet<EmailAddress> EmailAddresses { get; set; }
		public virtual DbSet<Person> People { get; set; }
		public virtual DbSet<PersonPhone> PersonPhones { get; set; }
		public virtual DbSet<PhoneNumberType> PhoneNumberTypes { get; set; }
		public virtual DbSet<StateProvince> StateProvinces { get; set; }
		public virtual DbSet<Product> Products { get; set; }
		public virtual DbSet<ShipMethod> ShipMethods { get; set; }
		public virtual DbSet<CountryRegionCurrency> CountryRegionCurrencies { get; set; }
		public virtual DbSet<CreditCard> CreditCards { get; set; }
		public virtual DbSet<Customer> Customers { get; set; }
		public virtual DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }
		public virtual DbSet<SalesOrderHeader> SalesOrderHeaders { get; set; }
		public virtual DbSet<SalesPerson> SalesPersons { get; set; }
		public virtual DbSet<SalesTerritory> SalesTerritories { get; set; }
		public virtual DbSet<SpecialOffer> SpecialOffers { get; set; }
		public virtual DbSet<Store> Stores { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Address>()
				.HasMany(e => e.BusinessEntityAddresses)
				.WithRequired(e => e.Address)
				.WillCascadeOnDelete(false);

			//modelBuilder.Entity<Address>()
			//	.HasMany(e => e.SalesOrderHeaders)
			//	.WithRequired(e => e.Address)
			//	.HasForeignKey(e => e.BillToAddress)
			//	.WillCascadeOnDelete(false);

			//modelBuilder.Entity<Address>()
			//	.HasMany(e => e.SalesOrderHeaders1)
			//	.WithRequired(e => e.Address1)
			//	.HasForeignKey(e => e.ShipToAddressID)
			//	.WillCascadeOnDelete(false);

			////
			//modelBuilder.Entity<SalesOrderHeader>()
			//	.HasRequired(e => e.Address)
			//	.WithMany(e => e.SalesOrderHeaders)
			//	.HasForeignKey(e => e.BillToAddressID)
			//	.WillCascadeOnDelete(false);


			modelBuilder.Entity<AddressType>()
				.HasMany(e => e.BusinessEntityAddresses)
				.WithRequired(e => e.AddressType)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<BusinessEntity>()
				.HasMany(e => e.BusinessEntityAddresses)
				.WithRequired(e => e.BusinessEntity)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<BusinessEntity>()
				.HasOptional(e => e.Person)
				.WithRequired(e => e.BusinessEntity);

			modelBuilder.Entity<BusinessEntity>()
				.HasOptional(e => e.SalesPerson)
				.WithRequired(e => e.BusinessEntity);

			modelBuilder.Entity<BusinessEntity>()
				.HasOptional(e => e.Store)
				.WithRequired(e => e.BusinessEntity);

			modelBuilder.Entity<CountryRegion>()
				.HasMany(e => e.CountryRegionCurrencies)
				.WithRequired(e => e.CountryRegion)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<CountryRegion>()
				.HasMany(e => e.SalesTerritories)
				.WithRequired(e => e.CountryRegion)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<CountryRegion>()
				.HasMany(e => e.StateProvinces)
				.WithRequired(e => e.CountryRegion)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<Person>()
				.Property(e => e.PersonType)
				.IsFixedLength();

			modelBuilder.Entity<Person>()
				.HasMany(e => e.EmailAddresses)
				.WithRequired(e => e.Person)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<Person>()
				.HasMany(e => e.Customers)
				.WithOptional(e => e.Person)
				.HasForeignKey(e => e.PersonID);

			modelBuilder.Entity<Person>()
				.HasMany(e => e.PersonPhones)
				.WithRequired(e => e.Person)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<PhoneNumberType>()
				.HasMany(e => e.PersonPhones)
				.WithRequired(e => e.PhoneNumberType)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<StateProvince>()
				.Property(e => e.StateProvinceCode)
				.IsFixedLength();

			modelBuilder.Entity<StateProvince>()
				.HasMany(e => e.Addresses)
				.WithRequired(e => e.StateProvince)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<Product>()
				.Property(e => e.StandardCost)
				.HasPrecision(19, 4);

			modelBuilder.Entity<Product>()
				.Property(e => e.ListPrice)
				.HasPrecision(19, 4);

			modelBuilder.Entity<Product>()
				.Property(e => e.SizeUnitMeasureCode)
				.IsFixedLength();

			modelBuilder.Entity<Product>()
				.Property(e => e.WeightUnitMeasureCode)
				.IsFixedLength();

			modelBuilder.Entity<Product>()
				.Property(e => e.Weight)
				.HasPrecision(8, 2);

			modelBuilder.Entity<Product>()
				.Property(e => e.ProductLine)
				.IsFixedLength();

			modelBuilder.Entity<Product>()
				.Property(e => e.Class)
				.IsFixedLength();

			modelBuilder.Entity<Product>()
				.Property(e => e.Style)
				.IsFixedLength();

			modelBuilder.Entity<ShipMethod>()
				.Property(e => e.ShipBase)
				.HasPrecision(19, 4);

			modelBuilder.Entity<ShipMethod>()
				.Property(e => e.ShipRate)
				.HasPrecision(19, 4);

			modelBuilder.Entity<ShipMethod>()
				.HasMany(e => e.SalesOrderHeaders)
				.WithRequired(e => e.ShipMethod)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<CountryRegionCurrency>()
				.Property(e => e.CurrencyCode)
				.IsFixedLength();

			modelBuilder.Entity<Customer>()
				.Property(e => e.AccountNumber)
				.IsUnicode(false);

			modelBuilder.Entity<Customer>()
				.HasMany(e => e.SalesOrderHeaders)
				.WithRequired(e => e.Customer)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<SalesOrderDetail>()
				.Property(e => e.UnitPrice)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesOrderDetail>()
				.Property(e => e.UnitPriceDiscount)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesOrderDetail>()
				.Property(e => e.LineTotal)
				.HasPrecision(38, 6);

			modelBuilder.Entity<SalesOrderHeader>()
				.Property(e => e.CreditCardApprovalCode)
				.IsUnicode(false);

			modelBuilder.Entity<SalesOrderHeader>()
				.Property(e => e.SubTotal)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesOrderHeader>()
				.Property(e => e.TaxAmt)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesOrderHeader>()
				.Property(e => e.Freight)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesOrderHeader>()
				.Property(e => e.TotalDue)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesPerson>()
				.Property(e => e.SalesQuota)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesPerson>()
				.Property(e => e.Bonus)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesPerson>()
				.Property(e => e.CommissionPct)
				.HasPrecision(10, 4);

			modelBuilder.Entity<SalesPerson>()
				.Property(e => e.SalesYTD)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesPerson>()
				.Property(e => e.SalesLastYear)
				.HasPrecision(19, 4);

			//
			modelBuilder.Entity<SalesPerson>()
				.HasRequired(e => e.Person)
				.WithOptional(e => e.SalesPerson);

			modelBuilder.Entity<SalesPerson>()
				.HasMany(e => e.SalesOrderHeaders)
				.WithOptional(e => e.SalesPerson)
				.HasForeignKey(e => e.SalesPersonID);

			modelBuilder.Entity<SalesPerson>()
				.HasMany(e => e.Stores)
				.WithOptional(e => e.SalesPerson)
				.HasForeignKey(e => e.SalesPersonID);

			modelBuilder.Entity<SalesTerritory>()
				.Property(e => e.SalesYTD)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesTerritory>()
				.Property(e => e.SalesLastYear)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesTerritory>()
				.Property(e => e.CostYTD)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesTerritory>()
				.Property(e => e.CostLastYear)
				.HasPrecision(19, 4);

			modelBuilder.Entity<SalesTerritory>()
				.HasMany(e => e.StateProvinces)
				.WithRequired(e => e.SalesTerritory)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<SpecialOffer>()
				.Property(e => e.DiscountPct)
				.HasPrecision(10, 4);

			modelBuilder.Entity<Store>()
				.HasMany(e => e.Customers)
				.WithOptional(e => e.Store)
				.HasForeignKey(e => e.StoreID);
		}
	}
}
