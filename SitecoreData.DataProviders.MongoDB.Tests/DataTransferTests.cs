using System;
using System.Collections.Generic;
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
            var connectionString = "mongodb://localhost:27017/?safe=true";
            _server = MongoServer.Create(connectionString);
            _db = _server.GetDatabase("nosqlmongotest");

        }

        [TearDown]
        public void ClearDataBase()
        {
            //_db.Drop();
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
        public void CanTransferData()
        {
            var sourceDatabase = Factory.GetDatabase("master");
            var targetDatabase = Factory.GetDatabase("nosqlmongotest");
            var item = sourceDatabase.GetItem("/sitecore/layout");
            

            var dataProvider = targetDatabase.GetDataProviders().First() as DataProviderWrapper;
            TransferRecursive(item, dataProvider);
        }

        public void TransferRecursive(Item item, DataProviderWrapper provider)
        {
            
            ItemDefinition parentDefinition = null;

            if (item.Parent != null)
            {
                parentDefinition = new ItemDefinition(item.Parent.ID, item.Parent.Name, item.Parent.TemplateID, item.Parent.BranchId);
            }

            // Create the item in database
            if (provider.CreateItem(item.ID, item.Name, item.TemplateID, parentDefinition, null))
            {
                foreach (var language in item.Languages)
                {
                    using (new LanguageSwitcher(language))
                    {
                        var itemInLanguage = item.Database.GetItem(item.ID);

                        if (itemInLanguage != null)
                        {
                            // Add a version
                            var itemDefinition = provider.GetItemDefinition(itemInLanguage.ID, null);

                            // TODO: Add all version and not just v1
                            provider.AddVersion(itemDefinition, new VersionUri(language, Sitecore.Data.Version.First), null);

                            // Send the field values to the provider
                            var changes = new ItemChanges(itemInLanguage);

                            foreach (Field field in itemInLanguage.Fields)
                            {
                                changes.FieldChanges[field.ID] = new FieldChange(field, field.Value);
                            }

                            provider.SaveItem(itemDefinition, changes, null);
                        }
                    }
                }
            }

            if (!item.HasChildren)
            {
                return;
            }

            foreach (Item child in item.Children)
            {
                TransferRecursive(child, provider);
            }
        }

        [Test]
        public void CanAccessSitecoreLayoutItem()
        {
            Item item = Factory.GetDatabase("master").GetItem("/sitecore/layout");
            Assert.That(item.ID.ToString(), Is.EqualTo("{EB2E4FFD-2761-4653-B052-26A64D385227}"));

        }


    }
}
