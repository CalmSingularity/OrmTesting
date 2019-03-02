
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

SELECT *
FROM Sales.SalesOrderHeader
--WHERE DATEDIFF(DAY, ModifiedDate, GETDATE()) = 0
WHERE TotalDue IS NULL

--WITH c AS
--(SELECT COUNT(*) nLines
--FROM Sales.SalesOrderDetail
--GROUP BY SalesOrderID)
--SELECT AVG(c.nLines), MIN(c.nLines), MAX(c.nLines)
--FROM c

SELECT AVG(UnitPrice), MIN(UnitPrice), MAX(UnitPrice)
FROM Sales.SalesOrderDetail
