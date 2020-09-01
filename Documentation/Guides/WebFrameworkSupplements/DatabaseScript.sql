create table ServiceTypes(
	ServiceTypeId int
		not null
		constraint ServiceTypesPk primary key,
	ServiceTypeName varchar( 100 )
		not null
)
go

insert into ServiceTypes values( 1, 'General service' )
insert into ServiceTypes values( 2, 'Flat tire repair' )
go

create table ServiceOrders(
	ServiceOrderId int
		not null
		constraint ServiceOrdersPk primary key,
	CustomerName varchar( 100 )
		not null,
	CustomerEmail varchar( 100 )
		not null,
	BicycleDescription varchar( 100 )
		not null,
	ServiceTypeId int
		not null
		constraint ServiceOrdersServiceTypeIdFk references ServiceTypes,
	CustomerBudget decimal( 9, 2 )
		null
)
go

create table ServiceOrderRequests(
	ServiceOrderRequestId int
		not null
		constraint ServiceOrderRequestsPk primary key,
	ServiceOrderId int
		not null
		constraint ServiceOrderRequestsServiceOrderIdFk references ServiceOrders,
	RequestDescription varchar( 100 )
		not null
)
go