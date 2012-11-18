using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Sitecore.Configuration;

namespace SitecoreData.DataProviders.MongoDB.Tests
{
    [TestFixture]
    class DataTransferTests
    {
        private MongoServer _server;
        private MongoDatabase _db;
        [SetUp]
        public void CopyDataFromTemplatesFolder()
        {
            var connectionString = "mongodb://localhost:27017/?safe=true";
            _server = MongoServer.Create(connectionString);
            _db = _server.GetDatabase("UnitTesting");

        }

        [TearDown]
        public void ClearDataBase()
        { }

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
        public void CanCopyDataToTestDatabase()
        {
            
        }

        [Test]
        public void CanAccessSitecoreDatabase()
        {
            Assert.That(Factory.GetDatabase("master"), Is.Not.Null);
        }



    }
}
