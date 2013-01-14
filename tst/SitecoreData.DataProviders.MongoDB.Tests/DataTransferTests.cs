using System;
using System.Configuration;
using System.Linq;
using FakeItEasy;
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
        private MongoDatabase _db;
        private Database _sourceDatabase;
        private Database _targetDatabase;


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

        [Test]
        public void CallbackMethodCalled()
        {
            var action = A.Fake<Action<string>>();
            using (var scope = Fake.CreateScope())
            {
                TransferUtil.TransferPath("/sitecore/layout/devices/print", _sourceDatabase, _targetDatabase, action);
                using (scope.OrderedAssertions())
                {
                    A.CallTo(() => action.Invoke("/sitecore")).MustHaveHappened(Repeated.Exactly.Once);
                    A.CallTo(() => action.Invoke("/sitecore/layout")).MustHaveHappened(Repeated.Exactly.Once);
                    A.CallTo(() => action.Invoke("/sitecore/layout/Devices")).MustHaveHappened(Repeated.Exactly.Once);
                    A.CallTo(() => action.Invoke("/sitecore/layout/Devices/Print")).MustHaveHappened(
                        Repeated.Exactly.Once);
                }
            }
        }

        [SetUp]
        public void CopyDataFromTemplatesFolder()
        {
            EnableConfigurationPatches();
            InitializeMongoConnection();
            _sourceDatabase = Factory.GetDatabase("master");
            _targetDatabase = Factory.GetDatabase("nosqlmongotest");
        }

        private void InitializeMongoConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["nosqlmongotest"].ConnectionString;
            var server = MongoServer.Create(connectionString);
            var databaseName = MongoUrl.Create(connectionString).DatabaseName;
            _db = server.GetDatabase(databaseName);
        }

        private static void EnableConfigurationPatches()
        {
            try
            {
                Sitecore.Context.IsUnitTesting = true;
            }
            catch (Exception)
            {
                //expect  on first call
            }
        }

        [TearDown]
        public void ClearDataBase()
        {
            _db.Drop();
        }
    }
}
