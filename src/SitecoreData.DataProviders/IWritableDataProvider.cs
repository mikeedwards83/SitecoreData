using System;
using System.Collections.Generic;
using Sitecore.Collections;
using Sitecore.Data.DataProviders;

namespace SitecoreData.DataProviders
{
    public interface IWritableDataProvider
    {
        bool CreateItem(Guid id, string name, Guid templateId, Guid parentId);

        bool DeleteItem(Guid id);

        void Store(ItemDto itemDto);

        void AddToPublishQueue(PublishItem item);

        void CleanUpPublishQueue(DateTime to);

        IEnumerable<PublishItem> GetPublishQueue(DateTime from, DateTime to);

        IDList SelectIds(string query, CallContext callContext);
    }
}