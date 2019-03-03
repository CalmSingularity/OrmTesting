DELETE FROM Sales.SalesOrderDetail
WHERE SalesOrderID = @so
DELETE FROM Sales.SalesOrderHeader
WHERE SalesOrderID = @so