
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace SitecoreData.DataProviders.MongoDB.Tests
{
    [TestFixture]
    class FastQueryAcceptanceTests : MongoTestsBase
    {

        public enum Database
        {
            Mongo, SqlServer
        }

        [SetUp]
        public void SetUp()
        {
            TransferUtil.TransferPath("/sitecore/layout", _sourceDatabase, _targetDatabase, null);
        }

        [Test]

        [TestCase(Database.Mongo, "fast:/sitecore/NotReal", Result = 0)]
        [TestCase(Database.SqlServer, "fast:/sitecore/NotReal", Result = 0)]

        [TestCase(Database.Mongo, "fast:/sitecore/Layout", Result = 1)]
        [TestCase(Database.SqlServer, "fast:/sitecore/Layout", Result = 1)]

        [TestCase(Database.Mongo, "fast:/sitecore/Layout/*", Result = 8)]
        [TestCase(Database.SqlServer, "fast:/sitecore/Layout/*", Result = 8)]

        [TestCase(
            Database.Mongo,
            "fast:/sitecore/Layout//*[@@id='{46D2F427-4CE5-4E1F-BA10-EF3636F43534}']",
            Result = 1)]
        [TestCase(
            Database.SqlServer,
            "fast:/sitecore/Layout//*[@@id='{46D2F427-4CE5-4E1F-BA10-EF3636F43534}']",
            Result = 1)]

        [TestCase(
                Database.Mongo,
                "fast:/sitecore/Layout//*[@@id='46D2F427-4CE5-4E1F-BA10-EF3636F43534']",
                Result = 1)]
        [TestCase(
            Database.SqlServer,
            "fast:/sitecore/Layout//*[@@id='46D2F427-4CE5-4E1F-BA10-EF3636F43534']",
            Result = 1)]

        [TestCase(
            Database.Mongo,
            "fast:/sitecore/Layout//*[@@name='Print']",
            Result = 1)]
        [TestCase(
            Database.SqlServer,
            "fast:/sitecore/Layout//*[@@name='Print']",
            Result = 1)]

        [TestCase(
            Database.Mongo,
            "fast:/sitecore/Layout//*[@@key='print']",
            Result = 1)]
        [TestCase(
            Database.SqlServer,
            "fast:/sitecore/Layout//*[@@key='print']",
            Result = 1)]

        [TestCase(
            Database.Mongo,
            "fast:/sitecore/Layout/Sublayouts//*[@@templatename='Sublayout']",
            Result = 2)]
        [TestCase(
            Database.SqlServer,
            "fast:/sitecore/Layout/Sublayouts//*[@@templatename='Sublayout']",
            Result = 2)]

        [TestCase(
            Database.Mongo,
            "fast:/sitecore/Layout//*[@@templateid='B6F7EEB4-E8D7-476F-8936-5ACE6A76F20B']",
            Result = 3)]
        [TestCase(
            Database.SqlServer,
            "fast:/sitecore/Layout//*[@@templateid='B6F7EEB4-E8D7-476F-8936-5ACE6A76F20B']",
            Result = 3)]

        [TestCase(
            Database.Mongo,
            "fast:/sitecore/Layout//*[@@templateid='{B6F7EEB4-E8D7-476F-8936-5ACE6A76F20B}']",
            Result = 3)]
        [TestCase(
            Database.SqlServer,
            "fast:/sitecore/Layout//*[@@templateid='{B6F7EEB4-E8D7-476F-8936-5ACE6A76F20B}']",
            Result = 3)]

        [TestCase(
            Database.Mongo,
            "fast:/sitecore/Layout/Sublayouts//*[@@templatekey='sublayout']",
            Result = 2)]
        [TestCase(
            Database.SqlServer,
            "fast:/sitecore/Layout/Sublayouts//*[@@templatekey='sublayout']",
            Result = 2)]

        [TestCase(
            Database.Mongo,
            "fast:/sitecore/Layout//*[@@parentid='{E18F4BC6-46A2-4842-898B-B6613733F06F}']",
            Result = 3)]
        [TestCase(
            Database.SqlServer,
            "fast:/sitecore/Layout//*[@@parentid='{E18F4BC6-46A2-4842-898B-B6613733F06F}']",
            Result = 3)]



        public int TestCountOfReturnedItemsForQuery(Database testdb, string query)
        {
            var db = GetTestDatabase(testdb);
            Item[] items = db.SelectItems(query);
            return items.Length;
        }
        //TODO @@masterid, 
        private Sitecore.Data.Database GetTestDatabase(Database testdb)
        {
            switch (testdb)
            {
                case Database.Mongo:
                    return _targetDatabase;
                case Database.SqlServer:
                    return _sourceDatabase;
                default:
                    throw new ArgumentOutOfRangeException("testdb");
            }
        }


    }
}
