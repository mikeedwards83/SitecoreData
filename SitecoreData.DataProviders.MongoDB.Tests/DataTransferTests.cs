using System.Configuration;
using System.Linq;
using MongoDB.Driver;
using NUnit.Framework;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace SitecoreData.DataProviders.MongoDB.Tests
{
    [TestFixture]
    [Category("MongoDB Data Transfer Tests")]
    class DataTransferTests
    {
        private MongoServer _server;
        private MongoDatabase _db;
        private Database _sourceDatabase;
        private Database _targetDatabase;

        [SetUp]
        public void CopyDataFromTemplatesFolder()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["nosqlmongotest"].ConnectionString;
            _server = MongoServer.Create(connectionString);
            var databaseName = MongoUrl.Create(connectionString).DatabaseName;
            _db = _server.GetDatabase(databaseName);
            _sourceDatabase = Factory.GetDatabase("master");
            _targetDatabase = Factory.GetDatabase("nosqlmongotest");

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
            TransferUtil.TransferPath("/sitecore/layout", _sourceDatabase, _targetDatabase, null);
            Assert.That(_db.GetCollectionNames().Contains("items"), Is.True);
        }

        [Test]
        public void CanTransferData()
        {
            TransferUtil.TransferPath("/sitecore/layout", _sourceDatabase, _targetDatabase, null);
            Assert.That(_db.GetCollection("items").Count(), Is.EqualTo(59));
        }

        [Test]
        public void CanTransferDeepHierarchy()
        {
            TransferUtil.TransferPath("/sitecore/layout/renderings", _sourceDatabase, _targetDatabase, null);
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
