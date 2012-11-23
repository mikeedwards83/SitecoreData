using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Globalization;

namespace SitecoreData.DataProviders
{
    static public class TransferUtil
    {
        public static void TransferPath(string itemPath, Database sourceDatabase, Database targetDatabase)
        {
            var item = sourceDatabase.GetItem(itemPath);
            var dataProvider = targetDatabase.GetDataProviders().First() as DataProviderWrapper;
            TransferAncestors(item.Parent, dataProvider);
            TransferItemAndDescendants(item, dataProvider);
        }

        private static void TransferItemAndDescendants(Item item, DataProviderWrapper provider)
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

        private static void TransferAncestors(Item item, DataProviderWrapper provider)
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