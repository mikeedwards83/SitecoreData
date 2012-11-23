using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Globalization;

namespace SitecoreData.DataProviders.MongoDB.Tests
{
    [TestFixture]
    [Category("MongoDB Data Transfer Tests")]
    class DataTransferTests
    {
        private MongoServer _server;
        private MongoDatabase _db;
        [SetUp]
        public void CopyDataFromTemplatesFolder()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["nosqlmongotest"].ConnectionString;
            _server = MongoServer.Create(connectionString);
            var databaseName = MongoUrl.Create(connectionString).DatabaseName;
            _db = _server.GetDatabase(databaseName);

        }

        [TearDown]
        public void ClearDataBase()
        {
            _db.Drop();
        }

        [Test]
        public void CanMakeConnectionToMongoServer()
        {
            Assert.That(_server,Is.Not.Null);
        }

        [Test]
        public void CanCreateTestDatabase()
        {
            Assert.That(_db, Is.Not.Null);
        }

        [Test]
        public void MongoDbHasItemsCollection()
        {
            var sourceDatabase = Factory.GetDatabase("master");
            var targetDatabase = Factory.GetDatabase("nosqlmongotest");
            TransferUtil.TransferPath("/sitecore/layout", sourceDatabase, targetDatabase);
            Assert.That(_db.GetCollectionNames().Contains("items"), Is.True);
        }

        [Test]
        public void CanTransferData()
        {
            var sourceDatabase = Factory.GetDatabase("master");
            var targetDatabase = Factory.GetDatabase("nosqlmongotest");
            TransferUtil.TransferPath("/sitecore/layout", sourceDatabase, targetDatabase);
            Assert.That(_db.GetCollection("items").Count(), Is.EqualTo(59));
        }

        [Test]
        public void CanTransferDeepHierarchy()
        {
            var sourceDatabase = Factory.GetDatabase("master");
            var targetDatabase = Factory.GetDatabase("nosqlmongotest");
            TransferUtil.TransferPath("/sitecore/layout/renderings", sourceDatabase, targetDatabase);
            Assert.That(_db.GetCollection("items").Count(), Is.EqualTo(13));

        }

        [Test]
        public void CanAccessSitecoreLayoutItem()
        {
            Item item = Factory.GetDatabase("master").GetItem("/sitecore/layout");
            Assert.That(item.ID.ToString(), Is.EqualTo("{EB2E4FFD-2761-4653-B052-26A64D385227}"));

        }


    }
}
