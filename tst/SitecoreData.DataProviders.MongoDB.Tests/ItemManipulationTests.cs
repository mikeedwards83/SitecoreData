using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sitecore;
using Sitecore.Data;

namespace SitecoreData.DataProviders.MongoDB.Tests
{
	[TestFixture]
	class ItemManipulationTests :MongoTestsRefreshDbPerFixture
	{
 
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			TransferUtil.TransferPath("/sitecore", _sqlServerSourceDb, _mongoTargetDb, null);
		}

		[SetUp]
		public void TestSetupDCreateTestItem()
		{
			_mongoTargetDb.GetItem("/sitecore/content/home").Add("tests", new TemplateID(TemplateIDs.StandardTemplate));
			
		}

		[TearDown]
		public void TestTearDown()
		{
			var item = _mongoTargetDb.GetItem("/sitecore/content/home/tests");
			item.Editing.BeginEdit();
			item.Delete();
			item.Editing.EndEdit();
		}

		[Test]
		public void TestsItem_WhenCreated_HasNoChildren()
		{
			Assert.AreEqual(0, _mongoTargetDb.GetItem("/sitecore/content/home/tests").GetChildren().Count());
		}

		[Test]
		public void HomeItem_WhenTransfered_Exists()
		{
			Assert.IsNotNull(_mongoTargetDb.GetItem("/sitecore/content/home"));
		}

		[Test]
		public void MongoDB_ItemCreated_Exists()
		{
			 _mongoTargetDb.GetItem("/sitecore/content/home/tests").Add("item1", new TemplateID(TemplateIDs.StandardTemplate));
			Assert.IsNotNull(_mongoTargetDb.GetItem("/sitecore/content/home/tests/item1"));
		}

		[Test]
		public void MongoDB_ItemRenamed_Exists()
		{
			
			var newItem = _mongoTargetDb.GetItem("/sitecore/content/home/tests").Add("item1", new TemplateID(TemplateIDs.StandardTemplate));
			newItem.Editing.BeginEdit();
			newItem.Name = "item2";
			newItem.Editing.EndEdit();
			Assert.IsNotNull(_mongoTargetDb.GetItem("/sitecore/content/home/tests/item2"));
		}
	}
}
