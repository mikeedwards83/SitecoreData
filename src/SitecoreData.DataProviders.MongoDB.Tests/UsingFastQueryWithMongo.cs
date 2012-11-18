using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sitecore.Data;

namespace SitecoreData.DataProviders.MongoDB.Tests
{
    [TestFixture]
    [Category("MongoDB Fast Query Tests")]
    public class UsingFastQueryWithMongo
    {
        private MongoDataProvider _provider;

        [SetUp]
        public void Setup()
        {
            _provider  = new MongoDataProvider(@"mongodb://localhost:27017/master");
        }

        [Test]
        public void CanRetrieveAnItem()
        {
            
            ItemDto rootItem = _provider.GetItem(Sitecore.ItemIDs.RootID.Guid);
            Assert.AreEqual(rootItem.Name, "sitecore");
        }

        [Test]
        public void FastQuery_CanRetrieveSingleItem()
        {
            //Assign
            string query = "fast:/#sitecore#/#content#/#home#";

            //Act
            var result = _provider.SelectIds(query, null);

            //Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new Guid("{110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9}"), result[0].Guid);


        }

        [Test]
        public void FastQuery_CanRetrieveChildItems()
        {
            //Assign
            string query = "fast:/#sitecore#/#content#/*";

            //Act
            var result = _provider.SelectIds(query, null);

            //Assert
        }

        [Test]
        public void FastQuery_CanRetrieveDescendants()
        {
            //Assign
            string query = "fast:/#sitecore#/#layout#/#Devices#";

            //Act
            var result = _provider.SelectIds(query, null);

            //Assert
        }


    }
}
