using System;
using System.Configuration;
using MongoDB.Driver;
using NUnit.Framework;
using Sitecore.Configuration;
using Sitecore.Data;

namespace SitecoreData.DataProviders.MongoDB.Tests
{
	internal abstract class MongoTestsBase
	{
		protected MongoDatabase _db;
		private static string _mongoTestDbConnectionString;
		private static MongoServer _mongoServer;
		protected Database _sqlServerSourceDb;
		protected Database _mongoTargetDb;

		[SetUp]
		public void SetupMongoTestDBConnectionAndRemoveAllItems()
		{
			Sitecore.Context.IsUnitTesting = true;
			_mongoTestDbConnectionString = ConfigurationManager.ConnectionStrings["nosqlmongotest"].ConnectionString;
			_mongoServer = MongoServer.Create(_mongoTestDbConnectionString);

			InitializeMongoConnection();
			RemoveAllItemsFromMongoTestDatabase();
			_sqlServerSourceDb = Factory.GetDatabase("master");
			_mongoTargetDb = Factory.GetDatabase("nosqlmongotest");
		}

		private void InitializeMongoConnection()
		{
			var databaseName = MongoUrl.Create(_mongoTestDbConnectionString).DatabaseName;
			_db = _mongoServer.GetDatabase(databaseName);
		}

		private void RemoveAllItemsFromMongoTestDatabase()
		{
			_mongoServer.GetDatabase("test")["items"].RemoveAll();
		}
	}
}