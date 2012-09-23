using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SitecoreData.DataProviders
{
    public class PublishItem
    {

        public Guid Id { get; set; }

        public string Language { get; set; }

        public int Version { get; set; }

        public DateTime Date { get; set; }

        public string Action { get; set; }
    }
}
