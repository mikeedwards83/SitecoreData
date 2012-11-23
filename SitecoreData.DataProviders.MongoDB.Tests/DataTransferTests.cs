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
            TransferPath("/sitecore/layout", sourceDatabase, targetDatabase);
            Assert.That(_db.GetCollectionNames().Contains("items"), Is.True);
        }

        [Test]
        public void CanTransferData()
        {
            var sourceDatabase = Factory.GetDatabase("master");
            var targetDatabase = Factory.GetDatabase("nosqlmongotest");
            TransferPath("/sitecore/layout", sourceDatabase, targetDatabase);
            Assert.That(_db.GetCollection("items").Count(), Is.EqualTo(59));
        }

        [Test]
        public void CanTransferDeepHierarchy()
        {
            var sourceDatabase = Factory.GetDatabase("master");
            var targetDatabase = Factory.GetDatabase("nosqlmongotest");
            TransferPath("/sitecore/layout/renderings", sourceDatabase, targetDatabase);
            Assert.That(_db.GetCollection("items").Count(), Is.EqualTo(13));

        }

        private void TransferPath(string itemPath, Database sourceDatabase, Database targetDatabase)
        {
            var item = sourceDatabase.GetItem(itemPath);
            var dataProvider = targetDatabase.GetDataProviders().First() as DataProviderWrapper;
            TransferAncestors(item.Parent, dataProvider);
            TransferItemAndDescendants(item, dataProvider);
        }

        //TODO Move to separate class, and refactor Transfer.aspx to use this new class.
        public void TransferItemAndDescendants(Item item, DataProviderWrapper provider)
        {
            
            TransferSingleItem(item, provider);

            if (!item.HasChildren)
            {
                return;
            }

            foreach (Item child in item.Children)
            {
                TransferItemAndDescendants(child, provider);
            }
        }
        
        public void TransferAncestors(Item item, DataProviderWrapper provider)
        {
            if (item == null) return;
            TransferAncestors(item.Parent,provider);
            TransferSingleItem(item, provider);
        }

        private static void TransferSingleItem(Item item, DataProviderWrapper provider)
        {
            ItemDefinition parentDefinition = null;

            if (item.Parent != null)
            {
                parentDefinition = new ItemDefinition(item.Parent.ID, item.Parent.Name, item.Parent.TemplateID,
                                                      item.Parent.BranchId);
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
        }

        [Test]
        public void CanAccessSitecoreLayoutItem()
        {
            Item item = Factory.GetDatabase("master").GetItem("/sitecore/layout");
            Assert.That(item.ID.ToString(), Is.EqualTo("{EB2E4FFD-2761-4653-B052-26A64D385227}"));

        }


    }
}
