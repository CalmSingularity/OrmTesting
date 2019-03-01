
SELECT *
FROM Person.Address
WHERE DATEDIFF(DAY, ModifiedDate, GETDATE()) = 0

SELECT *
FROM Person.BusinessEntity
WHERE DATEDIFF(DAY, ModifiedDate, GETDATE()) = 0

SELECT *
FROM Person.BusinessEntityAddress
WHERE DATEDIFF(DAY, ModifiedDate, GETDATE()) = 0

SELECT *
FROM Person.Person
WHERE DATEDIFF(DAY, ModifiedDate, GETDATE()) = 0

SELECT *
FROM Sales.Customer
WHERE DATEDIFF(DAY, ModifiedDate, GETDATE()) = 0