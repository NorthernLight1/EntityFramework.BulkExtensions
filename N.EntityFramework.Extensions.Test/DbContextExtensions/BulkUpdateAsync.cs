﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using N.EntityFramework.Extensions.Test.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace N.EntityFramework.Extensions.Test.DbContextExtensions
{
    [TestClass]
    public class BulkUpdateAsync : DbContextExtensionsBase
    {
        [TestMethod]
        public async Task With_Default_Options()
        {
            var dbContext = SetupDbContext(true);
            var orders = dbContext.Orders.Where(o => o.Price == 1.25M).OrderBy(o => o.Id).ToList();
            long maxId = 0;
            foreach (var order in orders)
            {
                order.Price = 2.35M;
                maxId = order.Id;
            }
            int rowsUpdated = await dbContext.BulkUpdateAsync(orders);
            var newOrders = dbContext.Orders.Where(o => o.Price == 2.35M).OrderBy(o => o.Id).Count();
            int entitiesWithChanges = dbContext.ChangeTracker.Entries().Where(t => t.State == EntityState.Modified).Count();

            Assert.IsTrue(orders.Count > 0, "There must be orders in database that match this condition (Price = $1.25)");
            Assert.IsTrue(rowsUpdated == orders.Count, "The number of rows updated must match the count of entities that were retrieved");
            Assert.IsTrue(newOrders == rowsUpdated, "The count of new orders must be equal the number of rows updated in the database.");
        }
        [TestMethod]
        public async Task With_Default_Options_Tpc()
        {
            var dbContext = SetupDbContext(true, PopulateDataMode.Tpc);
            var customers = dbContext.TpcPeople.Where(o => o.LastName != "BulkUpdateTest").OfType<TpcCustomer>().ToList();
            var vendors = dbContext.TpcPeople.OfType<TpcVendor>().ToList();
            foreach (var customer in customers)
            {
                customer.FirstName = string.Format("Id={0}", customer.Id);
                customer.LastName = "BulkUpdate_Tpc";
            }
            int rowsUpdated = await dbContext.BulkUpdateAsync(customers, options => { options.UpdateOnCondition = (s, t) => s.Id == t.Id; });
            var newCustomers = dbContext.TpcPeople.Where(o => o.LastName == "BulkUpdate_Tpc").OfType<TpcCustomer>().Count();
            int entitiesWithChanges = dbContext.ChangeTracker.Entries().Where(t => t.State == EntityState.Modified).Count();

            Assert.IsTrue(vendors.Count > 0 && vendors.Count != customers.Count, "There should be vendor records in the database");
            Assert.IsTrue(customers.Count > 0, "There must be customers in database that match this condition (Price = $1.25)");
            Assert.IsTrue(rowsUpdated == customers.Count, "The number of rows updated must match the count of entities that were retrieved");
            Assert.IsTrue(newCustomers == rowsUpdated, "The count of new customers must be equal the number of rows updated in the database.");
        }
        [TestMethod]
        public async Task With_Default_Options_Tph()
        {
            var dbContext = SetupDbContext(true, PopulateDataMode.Tph);
            var customers = dbContext.TphPeople.Where(o => o.LastName != "BulkUpdateTest").OfType<TphCustomer>().ToList();
            var vendors = dbContext.TphPeople.OfType<TphVendor>().ToList();
            foreach (var customer in customers)
            {
                customer.FirstName = string.Format("Id={0}", customer.Id);
                customer.LastName = "BulkUpdateTest";
            }
            int rowsUpdated = await dbContext.BulkUpdateAsync(customers);
            var newCustomers = dbContext.TphPeople.Where(o => o.LastName == "BulkUpdateTest").OrderBy(o => o.Id).Count();
            int entitiesWithChanges = dbContext.ChangeTracker.Entries().Where(t => t.State == EntityState.Modified).Count();

            Assert.IsTrue(vendors.Count > 0 && vendors.Count != customers.Count, "There should be vendor records in the database");
            Assert.IsTrue(customers.Count > 0, "There must be customers in database that match this condition (Price = $1.25)");
            Assert.IsTrue(rowsUpdated == customers.Count, "The number of rows updated must match the count of entities that were retrieved");
            Assert.IsTrue(newCustomers == rowsUpdated, "The count of new customers must be equal the number of rows updated in the database.");
        }
        [TestMethod]
        public async Task With_Options_IgnoreColumns_PropertyExpression()
        {
            var dbContext = SetupDbContext(true);
            var orders = dbContext.Orders.Where(o => o.Price == 1.25M && o.ExternalId != null).OrderBy(o => o.Id).ToList();
            foreach (var order in orders)
            {
                order.Price = 2.35M;
                order.ExternalId = null;
            }
            var oldTotal = dbContext.Orders.Where(o => o.Price == 2.35M && o.ExternalId != null).Count();
            int rowsUpdated = await dbContext.BulkUpdateAsync(orders, options => { options.IgnoreColumns = o => o.ExternalId; });
            var newTotal1 = dbContext.Orders.Where(o => o.Price == 2.35M && o.ExternalId != null).Count();
            var newTotal2 = dbContext.Orders.Where(o => o.Price == 1.25M && o.ExternalId != null).Count();

            Assert.IsTrue(orders.Count > 0, "There must be orders in database that match this condition (Price = $1.25)");
            Assert.IsTrue(newTotal1 == rowsUpdated + oldTotal, "The count of new orders must be equal the number of rows updated in the database.");
            Assert.IsTrue(newTotal2 == 0, "There should be not records with condition (Price = $1.25)");
        }
        [TestMethod]
        public async Task With_Options_IgnoreColumns_NewExpression()
        {
            var dbContext = SetupDbContext(true);
            var orders = dbContext.Orders.Where(o => o.Price == 1.25M && o.ExternalId != null).OrderBy(o => o.Id).ToList();
            foreach (var order in orders)
            {
                order.Price = 2.35M;
                order.ExternalId = null;
            }
            var oldTotal = dbContext.Orders.Where(o => o.Price == 2.35M && o.ExternalId != null).Count();
            int rowsUpdated = await dbContext.BulkUpdateAsync(orders, options => { options.IgnoreColumns = o => new { o.ExternalId }; });
            var newTotal1 = dbContext.Orders.Where(o => o.Price == 2.35M && o.ExternalId != null).Count();
            var newTotal2 = dbContext.Orders.Where(o => o.Price == 1.25M && o.ExternalId != null).Count();

            Assert.IsTrue(orders.Count > 0, "There must be orders in database that match this condition (Price = $1.25)");
            Assert.IsTrue(newTotal1 == rowsUpdated + oldTotal, "The count of new orders must be equal the number of rows updated in the database.");
            Assert.IsTrue(newTotal2 == 0, "There should be not records with condition (Price = $1.25)");
        }
        [TestMethod]
        public async Task With_Options_UpdateOnCondition()
        {
            var dbContext = SetupDbContext(true);
            var orders = dbContext.Orders.Where(o => o.Price == 1.25M).OrderBy(o => o.Id).ToList();
            int ordersWithExternalId = orders.Where(o => o.ExternalId != null).Count();
            foreach (var order in orders)
            {
                order.Price = 2.35M;
            }
            var oldTotal = dbContext.Orders.Where(o => o.Price == 2.35M && o.ExternalId != null).Count();
            int rowsUpdated = await dbContext.BulkUpdateAsync(orders, options => { options.UpdateOnCondition = (s, t) => s.ExternalId == t.ExternalId; });
            var newTotal = dbContext.Orders.Where(o => o.Price == 2.35M && o.ExternalId != null).Count();

            Assert.IsTrue(orders.Count > 0, "There must be orders in database that match this condition (Price = $1.25)");
            Assert.IsTrue(rowsUpdated == ordersWithExternalId, "The number of rows updated must match the count of entities that were retrieved");
            Assert.IsTrue(newTotal == rowsUpdated + oldTotal, "The count of new orders must be equal the number of rows updated in the database.");
        }
        [TestMethod]
        public async Task With_Transaction()
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
                rowsUpdated = await dbContext.BulkUpdateAsync(orders);
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
