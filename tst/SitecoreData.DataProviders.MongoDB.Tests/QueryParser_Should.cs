using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SitecoreData.DataProviders.MongoDB.Tests
{
    [TestFixture]
    class QueryElementParser_Should
    {
        [Test]
        public void Extract_String_Surround_By_Single_Quotes()
        {
            string input = @"abcd'efgh'ijkl";
            Assert.That(QueryElementParser.GetName(input), Is.EqualTo("efgh"));
        }
        
        [Test]
        public void Extract_String_Surround_By_Double_Quotes()
        {
            string input = @"abcd""efgh""ijkl";
            Assert.That(QueryElementParser.GetName(input), Is.EqualTo("efgh"));
        }


        [Test]
        public void Return_Empty_String_If_No_Quotes()
        {
            string input = @"abcdefghijkl";
            Assert.That(QueryElementParser.GetName(input), Is.EqualTo(string.Empty));
        }

        [Test]
        public void Disregard_Escaped_SingleQuote()
        {
            string input = @"abcd'ef\'gh'ijkl";
            Assert.That(QueryElementParser.GetName(input), Is.EqualTo(@"ef\'gh"));
        }


        [Test]
        public void Disregard_SingleQuotes_In_Double_Quote_String()
        {
            string input = @"abcd""e'f'gh""ijkl";
            Assert.That(QueryElementParser.GetName(input), Is.EqualTo(@"e'f'gh"));
        }
    }
}
