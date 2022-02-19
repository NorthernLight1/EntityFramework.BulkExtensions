﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using N.EntityFramework.Extensions.Test.Data;
using System.Data.Entity;
using System.Linq;

namespace N.EntityFramework.Extensions.Test.DbContextExtensions
{
    [TestClass]
    public class BulkUpdate : DbContextExtensionsBase
    {
        [TestMethod]
        public void With_Default_Options()
        {
            var dbContext = SetupDbContext(true);
            var orders = dbContext.Orders.Where(o => o.Price == 1.25M).OrderBy(o => o.Id).ToList();
            long maxId = 0;
            foreach (var order in orders)
            {
                order.Price = 2.35M;
                maxId = order.Id;
            }
            int rowsUpdated = dbContext.BulkUpdate(orders);
            var newOrders = dbContext.Orders.Where(o => o.Price == 2.35M).OrderBy(o => o.Id).Count();
            int entitiesWithChanges = dbContext.ChangeTracker.Entries().Where(t => t.State == EntityState.Modified).Count();

            Assert.IsTrue(orders.Count > 0, "There must be orders in database that match this condition (Price = $1.25)");
            Assert.IsTrue(rowsUpdated == orders.Count, "The number of rows updated must match the count of entities that were retrieved");
            Assert.IsTrue(newOrders == rowsUpdated, "The count of new orders must be equal the number of rows updated in the database.");
            //Assert.IsTrue(entitiesWithChanges == 0, "There should be no pending Order entities with changes after BulkInsert completes");
        }
        [TestMethod]
        public void With_Default_Options_Tph()
        {
            var dbContext = SetupDbContext(true);
            var customers = dbContext.TphPeople.Where(o => o.LastName != "BulkUpdateTest").OfType<TphCustomer>().ToList();
            var vendors = dbContext.TphPeople.OfType<TphVendor>().ToList();
            foreach (var customer in customers)
            {
                customer.FirstName = string.Format("Id={0}", customer.Id);
                customer.LastName = "BulkUpdateTest";
            }
            int rowsUpdated = dbContext.BulkUpdate(customers);
            var newCustomers = dbContext.TphPeople.Where(o => o.LastName == "BulkUpdateTest").OrderBy(o => o.Id).Count();
            int entitiesWithChanges = dbContext.ChangeTracker.Entries().Where(t => t.State == EntityState.Modified).Count();

            Assert.IsTrue(vendors.Count > 0 && vendors.Count != customers.Count, "There should be vendor records in the database");
            Assert.IsTrue(customers.Count > 0, "There must be customers in database that match this condition (Price = $1.25)");
            Assert.IsTrue(rowsUpdated == customers.Count, "The number of rows updated must match the count of entities that were retrieved");
            Assert.IsTrue(newCustomers == rowsUpdated, "The count of new customers must be equal the number of rows updated in the database.");
        }
        [TestMethod]
        public void With_Options_UpdateOnCondition()
        {
            var dbContext = SetupDbContext(true);
            var orders = dbContext.Orders.Where(o => o.Price == 1.25M).OrderBy(o => o.Id).ToList();
            int ordersWithExternalId = orders.Where(o => o.ExternalId != null).Count();
            foreach (var order in orders)
            {
                order.Price = 2.35M;
            }
            var oldTotal = dbContext.Orders.Where(o => o.Price == 2.35M && o.ExternalId != null).Count();
            int rowsUpdated = dbContext.BulkUpdate(orders, options => { options.UpdateOnCondition = (s, t) => s.ExternalId == t.ExternalId; });
            var newTotal = dbContext.Orders.Where(o => o.Price == 2.35M && o.ExternalId != null).Count();

            Assert.IsTrue(orders.Count > 0, "There must be orders in database that match this condition (Price = $1.25)");
            Assert.IsTrue(rowsUpdated == ordersWithExternalId, "The number of rows updated must match the count of entities that were retrieved");
            Assert.IsTrue(newTotal == rowsUpdated + oldTotal, "The count of new orders must be equal the number of rows updated in the database.");
            //Assert.IsTrue(entitiesWithChanges == 0, "There should be no pending Order entities with changes after BulkInsert completes");
        }
        [TestMethod]
        public void With_Transaction()
        {
            var dbContext = SetupDbContext(true);
            var orders = dbContext.Orders.Where(o => o.Price == 1.25M).OrderBy(o => o.Id).ToList();
            long maxId = 0;
            foreach (var order in orders)
            {
                order.Price = 2.35M;
                maxId = order.Id;
            }
            int rowsUpdated, newOrders;
            using (var transaction = dbContext.Database.BeginTransaction())
            {
                rowsUpdated = dbContext.BulkUpdate(orders);
                newOrders = dbContext.Orders.Where(o => o.Price == 2.35M).Count();
                transaction.Rollback();
            }
            int rollbackTotal = dbContext.Orders.Where(o => o.Price == 1.25M).Count();

            Assert.IsTrue(orders.Count > 0, "There must be orders in database that match this condition (Price = $1.25)");
            Assert.IsTrue(rowsUpdated == orders.Count, "The number of rows updated must match the count of entities that were retrieved");
            Assert.IsTrue(newOrders == rowsUpdated, "The count of new orders must be equal the number of rows updated in the database.");
            Assert.IsTrue(rollbackTotal == orders.Count, "The number of rows after the transacation has been rollbacked should match the original count");
        }
    }
}
