using System;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Version = Sitecore.Data.Version;

namespace SitecoreData.DataProviders
{
    static public class TransferUtil
    {
        public static void TransferPath(string itemPath, Database sourceDatabase, Database targetDatabase, Action<string> callback)
        {
            var item = sourceDatabase.GetItem(itemPath);
            var dataProvider = targetDatabase.GetDataProviders().First() as DataProviderWrapper;
            TransferAncestors(item.Parent, dataProvider, callback);
            TransferItemAndDescendants(item, dataProvider, callback);
        }

        private static void TransferItemAndDescendants(Item item, DataProviderWrapper provider, Action<string> callback)
        {
            
            TransferSingleItem(item, provider, callback);

            if (!item.HasChildren)
            {
                return;
            }

            foreach (Item child in item.Children)
            {
                TransferItemAndDescendants(child, provider, callback);
            }
        }

        private static void TransferAncestors(Item item, DataProviderWrapper provider, Action<string> callback)
        {
            if (item == null) return;
            TransferAncestors(item.Parent,provider, callback);
            TransferSingleItem(item, provider, callback);
        }

        private static void TransferSingleItem(Item item, DataProviderWrapper provider, Action<string> callback)
        {
            ItemDefinition parentDefinition = null;

            if (item.Parent != null)
            {
                parentDefinition = new ItemDefinition(item.Parent.ID, item.Parent.Name, item.Parent.TemplateID,
                                                      item.Parent.BranchId);
            }

            if (callback != null)
            {
                callback(item.Paths.FullPath);
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
                            provider.AddVersion(itemDefinition, new VersionUri(language, Version.First), null);

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
    }
}