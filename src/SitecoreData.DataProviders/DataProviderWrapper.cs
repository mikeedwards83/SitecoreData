﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Sitecore;
using Sitecore.Caching;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Reflection;
using Version = Sitecore.Data.Version;
using System.Collections;

namespace SitecoreData.DataProviders
{
    public class DataProviderWrapper : DataProvider
    {
        private readonly object _prefetchCacheLock = new object();
        private readonly long _prefetchCacheSize = Settings.Caching.DefaultDataCacheSize;
        private Cache _prefetchCache;
        private DataProviderBase _provider;

        public LanguageCollection Languages { get; private set; }

        public DataProviderWrapper(string connectionStringName, string implementationType)
        {
            if (string.IsNullOrEmpty(connectionStringName))
            {
                throw new ArgumentException("Can not be null or empty", "connectionStringName");
            }

            if (string.IsNullOrEmpty(implementationType))
            {
                throw new ArgumentException("Can not be null or empty", "implementationType");
            }

            ConnectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
            ImplementationType = implementationType;

        }

        protected string ImplementationType { get; set; }
        protected string ConnectionString { get; set; }

        protected DataProviderBase Provider
        {
            get
            {
                if (_provider == null)
                {
                    _provider = (DataProviderBase)ReflectionUtil.CreateObject(ImplementationType, new object[] {ConnectionString});
                }

                if (_provider == null)
                {
                    throw new Exception(string.Format("Could not create a instance of \"{0}\"", ImplementationType));
                }

                return _provider;
            }
        }

        protected Cache PrefetchCache
        {
            get
            {
                if (_prefetchCache != null)
                {
                    return _prefetchCache;
                }

                lock (_prefetchCacheLock)
                {
                    if (_prefetchCache != null)
                    {
                        return _prefetchCache;
                    }

                    var cacheName = Provider.GetType().Name + " - Prefetch data";
                    var instance = Cache.GetNamedInstance(cacheName, _prefetchCacheSize);

                    instance.Enabled = !CacheOptions.DisableAll;

                    _prefetchCache = instance;

                    return _prefetchCache;
                }
            }
        }

	    public override ItemDefinition GetItemDefinition(ID itemId, CallContext context)
        {
            var prefetchData = GetPrefetchData(itemId);

            if (prefetchData == null)
            {
                return null;
            }

            return prefetchData.ItemDefinition;
        }

        private PrefetchData GetPrefetchData(ID itemId)
        {
            var data = PrefetchCache[itemId] as PrefetchData;

            if (data != null)
            {
                if (!data.ItemDefinition.IsEmpty)
                {
                    return data;
                }

                return null;
            }

            var itemDto = Provider.GetItem(itemId.ToGuid());

            if (itemDto != null)
            {
                data = new PrefetchData(new ItemDefinition(itemId, itemDto.Name, new ID(itemDto.TemplateId), new ID(itemDto.BranchId)), new ID(itemDto.ParentId));

                PrefetchCache.Add(itemId, data, data.GetDataLength());

                return data;
            }

            return null;
        }

        public override VersionUriList GetItemVersions(ItemDefinition itemDefinition, CallContext context)
        {
            // TODO: Move to provider!
            // TODO: Use prefecthing like the old one?
            var result = Provider.GetItem(itemDefinition.ID.ToGuid());

            if (result != null && result.FieldValues != null)
            {
                var versions = new VersionUriList();
                var versionsList = new List<VersionUri>();

                foreach (var fieldKey in result.FieldValues.Where(field => field.Version.HasValue && field.Language != null))
                {
                    if (versionsList.Any(ver => fieldKey.Matches(ver)))
                    {
                        continue;
                    }

                    var newVersionUri = new VersionUri(LanguageManager.GetLanguage(fieldKey.Language), new Version(fieldKey.Version.Value));

                    versionsList.Add(newVersionUri);
                }

                foreach (var version in versionsList)
                {
                    versions.Add(version);
                }

                return versions;
            }

            return null;
        }

        public override FieldList GetItemFields(ItemDefinition itemDefinition, VersionUri versionUri, CallContext context)
        {
            // TODO: Use prefecthing like the old one?
            var result = Provider.GetItem(itemDefinition.ID.ToGuid());

            if (result != null && result.FieldValues != null)
            {
                var fields = new FieldList();

                foreach (var fieldValue in result.FieldValues.Where(field => field.Matches(versionUri)))
                {
                    fields.Add(new ID(fieldValue.Id), fieldValue.Value);
                }

                return fields;
            }

            return null;
        }

        public override IDList GetChildIDs(ItemDefinition itemDefinition, CallContext context)
        {
            // TODO: Use prefecthing like the old one?
            var childIds = Provider.GetChildIds(itemDefinition.ID.ToGuid());

            return IDList.Build(childIds.Select(guid => new ID(guid)).ToArray());
        }

        public override ID GetParentID(ItemDefinition itemDefinition, CallContext context)
        {
            // TODO: Use prefecthing like the old one?
            var guid = Provider.GetParentId(itemDefinition.ID.ToGuid());

            if (guid == Guid.Empty)
            {
                return ID.Null;
            }

            return ID.Parse(guid);
        }

        public override IdCollection GetTemplateItemIds(CallContext context)
        {
            var guids = Provider.GetTemplateIds(TemplateIDs.Template.ToGuid());
            var list = new IdCollection();

            foreach (var guid in guids)
            {
                list.Add(ID.Parse(guid));
            }

            return list;
        }

        public override bool DeleteItem(ItemDefinition itemDefinition, CallContext context)
        {
            var provider = Provider.WritableProvider;

            return provider.DeleteItem(itemDefinition.ID.ToGuid());
        }

        public override bool CreateItem(ID itemId, string itemName, ID templateId, ItemDefinition parent, CallContext context)
        {
            var current = Provider.GetItem(itemId.ToGuid());

            if (current != null)
            {
                return false;
            }

            if (parent != null)
            {
                var parentItem = Provider.GetItem(parent.ID.ToGuid());

                if (parentItem == null)
                {
                    return false;
                }
            }

            var provider = Provider.WritableProvider;

            Guid parentId;

            if (parent == null)
            {
                parentId = Guid.Empty;
            }
            else
            {
                parentId = parent.ID.ToGuid();
            }

            provider.CreateItem(itemId.ToGuid(), itemName, templateId.ToGuid(), parentId);

            return true;
        }

        public override bool SaveItem(ItemDefinition itemDefinition, ItemChanges changes, CallContext context)
        {
            var current = Provider.GetItem(itemDefinition.ID.ToGuid());

            if (current == null)
            {
                return false;
            }


            //nothing has changed
            if (!changes.HasFieldsChanged && !changes.HasPropertiesChanged)
                return false;


            if (changes.HasPropertiesChanged)
            {
                current.Name = StringUtil.GetString(changes.GetPropertyValue("name"), itemDefinition.Name);
                current.Key = current.Name.ToLowerInvariant();
                var templateId = MainUtil.GetObject(changes.GetPropertyValue("templateid"), itemDefinition.TemplateID) as ID;
                current.TemplateId = templateId != ID.Null ? templateId.ToGuid() : Guid.Empty;

                var branchId = MainUtil.GetObject(changes.GetPropertyValue("branchid"), itemDefinition.BranchId) as ID;
                current.BranchId = branchId != ID.Null ? branchId.ToGuid() : Guid.Empty;
            }

            if (changes.HasFieldsChanged)
            {
                foreach (FieldChange change in changes.FieldChanges)
                {
                    var fieldVersionUri = new VersionUri(
                        change.Definition == null || change.Definition.IsShared ? null : change.Language,
                        change.Definition == null || change.Definition.IsUnversioned ? null : change.Version);

                    var matchingFields = current.FieldValues.Where(fv => fv.Matches(fieldVersionUri) && fv.Id.Equals(change.FieldID.ToGuid())).ToList();

                    if (change.RemoveField)
                    {
                        if (matchingFields.Any())
                        {
                            current.FieldValues.Remove(matchingFields.First());
                        }
                    }
                    else
                    {
                        if (matchingFields.Any())
                        {
                            current.FieldValues.Find(fv => fv.Matches(fieldVersionUri) && fv.Id.Equals(change.FieldID.ToGuid())).Value = change.Value;
                        }
                        else
                        {
                            current.FieldValues.Add(new FieldDto
                                                        {
                                                            Id = change.FieldID.ToGuid(),
                                                            Language = fieldVersionUri.Language != null ? fieldVersionUri.Language.Name : null,
                                                            Version = fieldVersionUri.Version != null ? fieldVersionUri.Version.Number : null as int?,
                                                            Value = change.Value
                                                        });
                        }


                        if (change.FieldID == FieldIDs.WorkflowState)
                        {
                            Guid workflowStateId = Guid.Empty;
                            Guid.TryParse(change.Value, out workflowStateId);

                            current.WorkflowStateId = workflowStateId;
                        }
                    }

                   
                }

               

            }
            Provider.WritableProvider.Store(current);

            return true;
        }

        public override IDList SelectIDs(string query, CallContext context)
        {
            if (_provider is IWritableDataProvider)
            {
                return ((IWritableDataProvider) _provider).SelectIds(query, context);
            }
            return base.SelectIDs(query, context);
        }

        public override int AddVersion(ItemDefinition itemDefinition, VersionUri baseVersion, CallContext context)
        {
            var current = Provider.GetItem(itemDefinition.ID.ToGuid());

            if (current == null)
            {
                return -1;
            }

            var versionNumber = -1;

            if (baseVersion.Version != null && baseVersion.Version.Number > 0)
            {
                // copy version
                var currentFieldValues = current.FieldValues.Where(fv => fv.Matches(baseVersion)).ToList();
                var maxVersionNumber = currentFieldValues.Max(fv => fv.Version);

                versionNumber = maxVersionNumber.HasValue && maxVersionNumber > 0 ? maxVersionNumber.Value + 1 : -1;

                if (versionNumber > 0)
                {
                    foreach (var fieldValue in currentFieldValues)
                    {
                        current.FieldValues.Add(new FieldDto
                                                    {
                                                        Id = fieldValue.Id,
                                                        Language = fieldValue.Language,
                                                        Version = versionNumber,
                                                        Value = fieldValue.Value
                                                    });
                    }
                }
            }

            if (versionNumber == -1)
            {
                versionNumber = 1;

                // add blank version
                current.FieldValues.Add(new FieldDto
                                            {
                                                Id = FieldIDs.Created.ToGuid(),
                                                Language = baseVersion.Language.Name,
                                                Version = versionNumber,
                                                Value = string.Empty
                                            });
            }

            Provider.WritableProvider.Store(current);

            return versionNumber;
        }

        public override DataUri[] GetItemsInWorkflowState(Sitecore.Workflows.WorkflowInfo info, CallContext context)
        {
            Guid workflowStateId = Guid.Empty;
            if(Guid.TryParse(info.StateID, out workflowStateId)){
               var items = Provider.GetItemsInWorkflowState(workflowStateId);
               var result = items.Select(
                        x => x.FieldValues.Where(y => y.Id == FieldIDs.WorkflowState.Guid)
                       .Select(y=> new DataUri(new ID(x.Id), LanguageManager.GetLanguage(y.Language), new Version( y.Version??1)))
                       
                       );
                if(result.Any())
                    return result.Aggregate((x,y)=> (x??new DataUri[]{}).Concat(y ?? new DataUri[]{})).ToArray();
                else
                    return new DataUri[] { };
            }
            else return new DataUri[]{};
        }

        public override bool AddToPublishQueue(ID itemID, string action, DateTime date, CallContext context)
        {
            // this.Api.Execute(" INSERT INTO {0}PublishQueue{1} (     {0}ItemID{1}, {0}Language{1}, {0}Version{1}, {0}Date{1}, 
            // {0}Action{1}   )   VALUES(     {2}itemID{3}, {2}language{3}, {2}version{3}, {2}date{3}, {2}action{3}   )", (object)"itemID", 
            // (object)itemID, (object)"language", (object)"*", (object)"version", (object)0, (object)"date", (object)date, (object)"action",
            // (object)action);

            PublishItem item = new PublishItem();
            item.Id = itemID.Guid;
            item.Language = "*";
            item.Version = 0;
            item.Date = date;
            item.Action = action;

            Provider.WritableProvider.AddToPublishQueue(item);

            return true;
        }

        public override bool CleanupPublishQueue(DateTime to, CallContext context)
        {
            Provider.WritableProvider.CleanUpPublishQueue(to);
            return true;
        }

        public override IDList GetPublishQueue(DateTime from, DateTime to, CallContext context)
        {
            var items = Provider.WritableProvider.GetPublishQueue(from, to);

            Hashtable hashtable = new Hashtable();
            IDList list = new IDList();

            foreach (var item in items)
            {
                if (hashtable.ContainsKey(item.Id))
                    continue;

                hashtable[item.Id] = string.Empty;

                list.Add(new ID(item.Id));
            }


            return list;
        }
        public override LanguageCollection GetLanguages(CallContext context)
        {
            if (this.Languages == null) this.Languages = LoadLanguages();

            return Languages;
        }

        public LanguageCollection LoadLanguages()
        {
            var languages = Provider.GetItemsByTemplate(TemplateIDs.Language.Guid);

            LanguageCollection languageCollection = new LanguageCollection();

            foreach (var language in languages.Where(x=>!string.IsNullOrEmpty(x.Name)))
            {
                Language result;
                if (Language.TryParse(language.Name, out result) && !languageCollection.Contains(result))
                {
                    result.Origin.ItemId = new ID(language.Id);
                    languageCollection.Add(result);
                }
            }
            return languageCollection;
        }
      

        
    }
}