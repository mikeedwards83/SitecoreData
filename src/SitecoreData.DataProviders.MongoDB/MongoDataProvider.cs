using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Builders;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Workflows;

namespace SitecoreData.DataProviders.MongoDB
{
    public class MongoDataProvider : DataProviderBase, IWritableDataProvider, IDisposable
    {
        public MongoDataProvider(string connectionString) : base(connectionString)
        {
            // TODO: SubClass Item*Dto to decorate with BSON attributes?
            SafeMode = SafeMode.True;
            JoinParentId = ID.Null;
            
            var databaseName = MongoUrl.Create(connectionString).DatabaseName;

            Server = MongoServer.Create(connectionString);
            Db = Server.GetDatabase(databaseName);

            Items = Db.GetCollection<ItemDto>("items", SafeMode);
            Items.EnsureIndex(IndexKeys.Ascending(new[] {"ParentId"}));
            Items.EnsureIndex(IndexKeys.Ascending(new[] {"TemplateId"}));
            Items.EnsureIndex(IndexKeys.Ascending(new[] { "WorkflowStateId" }));
            Items.EnsureIndex(IndexKeys.Ascending(new[] { "Key" }));

            PublishQueue = Db.GetCollection<PublishItem>("publishQueue", SafeMode);
            PublishQueue.EnsureIndex(IndexKeys.Ascending(new[] { "Date" }));


        }

        private ID JoinParentId { get; set; }

        private MongoServer Server { get; set; }

        private MongoDatabase Db { get; set; }

        private MongoCollection<ItemDto> Items { get; set; }

        private MongoCollection<PublishItem> PublishQueue { get; set; }

        private SafeMode SafeMode { get; set; }

        public void Dispose()
        {
        }

        public bool CreateItem(Guid id, string name, Guid templateId, Guid parentId)
        {
            var exists = GetItem(id);

            if (exists != null)
            {
                return true;
            }

            var item = new ItemDto
                           {
                               Id = id,
                               Name = name,
                               Key = name.ToLowerInvariant(),
                               TemplateId = templateId,
                               ParentId = parentId
                           };

            Store(item);

            return true;
        }

        public override IEnumerable<ItemDto> GetItemsInWorkflowState(Guid workflowStateId){
            //    SELECT F.{0}ItemId{1}, F.{0}Language{1}, F.{0}Version{1}\r\n FROM {0}Items{1} I, {0}VersionedFields{1} F\r\n
            //    WHERE F.{0}ItemId{1} = I.{0}ID{1}\r\n AND F.{0}FieldId{1} = {2}fieldID{3}\r\n AND F.{0}Value{1} = {2}fieldValue{3}\r\n 
            //    ORDER BY I.{0}Name{1}, F.{0}Language{1}, F.{0}Version{1}", (object) "fieldID", (object) FieldIDs.WorkflowState,
            //    (object) "fieldValue", (object) info.StateID)

            var query =    Query.EQ("WorkflowStateId", workflowStateId);
            return Items.Find(query);
                  
        }

        public bool DeleteItem(Guid id)
        {
            var result = Items.Remove(Query.EQ("_id", id), RemoveFlags.Single, SafeMode);

            return result != null && result.Ok;
        }

        public void Store(ItemDto item)
        {
            Items.Save(item, SafeMode);
        }

        public override ItemDto GetItem(Guid id)
        {
            return Items.FindOneByIdAs<ItemDto>(id);
        }

        public override IEnumerable<Guid> GetChildIds(Guid parentId)
        {
            var query = Query.EQ("ParentId",
                                 parentId == JoinParentId.ToGuid()
                                     ? Guid.Empty
                                     : parentId);

            return Items.FindAs<ItemDto>(query).Select(it => it.Id).ToArray();
        }

        public override Guid GetParentId(Guid id)
        {
            var result = Items.FindOneByIdAs<ItemDto>(id);
            
            return result != null ? (result.ParentId != Guid.Empty ? result.ParentId : JoinParentId.ToGuid()) : Guid.Empty;
        }

        public override IEnumerable<Guid> GetTemplateIds(Guid templateId)
        {
            var query = Query.EQ("TemplateId", TemplateIDs.Template.ToGuid());
            var ids = new List<Guid>();

            foreach (var id in Items.FindAs<ItemDto>(query).Select(it => it.Id))
            {
                ids.Add(id);
            }

            return ids;
        }

       

        public void AddToPublishQueue(PublishItem item)
        {
            PublishQueue.Save(item);
        }


        public void CleanUpPublishQueue(DateTime to)
        {
            var query = Query.LT("Date", to);
            PublishQueue.Remove(query);
        }

        public IEnumerable<PublishItem> GetPublishQueue(DateTime from, DateTime to)
        {
            var query = Query.And(
                Query.LT("Date", to),
                Query.GT("Date", from));
            return PublishQueue.Find(query).ToArray();
        }



        public override IEnumerable<ItemDto> GetItemsByTemplate(Guid templateId)
        {
            var query = Query.EQ("TemplateId", templateId);
            return Items.Find(query).ToArray();
        }



        public IDList SelectIds(string query, Sitecore.Data.DataProviders.CallContext callContext)
        {
            IDList returnList = new IDList();

            //fast query 

            if (query.StartsWith("fast:"))
			{
                query = query.Substring(5);
                if (!query.Contains("*"))
                {
                    var id = GetItemFromSimplePath(query);
                    if (id != Guid.Empty)
                        returnList.Add(new ID(id));
                }
                else if(query.Contains("["))
                {
                    List<Guid> guids = GetItemsMatchingPredicate(query);
                    guids.ForEach(g => returnList.Add(new ID(g)));
                }
                else if (query.EndsWith(@"//*"))
                {
                    List<Guid> guids = GetMatchingItemsForDescendantPath(query);
                    guids.ForEach(g => returnList.Add(new ID(g)));
                }
                else if (query.EndsWith(@"/*") && !query.Contains(@"//")) 
                {
                    List<Guid> guids = GetMatchingItemsForWildcardPath(query);
                    guids.ForEach(g => returnList.Add(new ID(g)));
                }

                return returnList;
            }

            return null;
        }

        private List<Guid> GetItemsMatchingPredicate(string query)
        {
            string firstPart = query.Substring(0, query.IndexOf("["));
            List<Guid> baseList = new List<Guid>();
            if (firstPart.EndsWith(@"//*"))
            {
                baseList = GetMatchingItemsForDescendantPath(firstPart);
            }
            else if (firstPart.EndsWith(@"/*"))
            {
                baseList = GetMatchingItemsForWildcardPath(firstPart);
            }
            else
            {
                var id = GetItemFromSimplePath(firstPart);
                if (id != Guid.Empty)
                {
                    baseList.Add(id);
                }
            }

            var predicate = QueryElementParser.GetPredicate(query);

            if (predicate.Contains("@@id"))
            {
                Guid guid = QueryElementParser.GetGuidFromPredicate(predicate);
                baseList = baseList.Where(i => i == guid).ToList();
            } else if (predicate.Contains("@@templateid"))
            {
                Guid guid = QueryElementParser.GetGuidFromPredicate(predicate);
                baseList = baseList.Select(GetItem).Where(i => i.TemplateId == guid).Select(i => i.Id).ToList();
            }
            else if (predicate.Contains("@@masterid"))
            {
                Guid guid = QueryElementParser.GetGuidFromPredicate(predicate);
                baseList = baseList.Select(GetItem).Where(i => i.BranchId == guid).Select(i => i.Id).ToList();
            } else if (predicate.Contains("@@parentid"))
            {
                Guid guid = QueryElementParser.GetGuidFromPredicate(predicate);
                baseList = baseList.Select(GetItem).Where(i => i.ParentId == guid).Select(i => i.Id).ToList();
            }
            else if (predicate.Contains("@@key"))
            {
                String name = QueryElementParser.GetName(predicate);
                baseList = baseList.Select(GetItem).Where(i => i.Key == name).Select(i => i.Id).ToList();
            }
            else if (predicate.Contains("@@name"))
            {
                String name = QueryElementParser.GetName(predicate);
                baseList = baseList.Select(GetItem).Where(i => i.Name == name).Select(i => i.Id).ToList();
            }
			else if (predicate.Contains("@@templatename"))
			{
				String name = QueryElementParser.GetName(predicate);
				IEnumerable<ItemDto> templates = GetTemplates();  //TODO Find efficient and reliable way to cache this.
				IEnumerable<Guid> matchingTemplates = templates.Where(i => i.Name == name).Select(i => i.Id).ToList();
				baseList = baseList.Select(GetItem).Where(i => matchingTemplates.Contains(i.TemplateId)).Select(i => i.Id).ToList();
			}
            return baseList;
        }

	    private IEnumerable<ItemDto> GetTemplates()
	    {
		    return GetTemplatesRecursive(Sitecore.ItemIDs.TemplateRoot.Guid, new List<Guid>()).Select(GetItem).ToList();
	    }

	    private IEnumerable<Guid> GetTemplatesRecursive(Guid id, IList<Guid> list)
	    {
		    ItemDto item = GetItem(id);
			if (item.TemplateId == Sitecore.TemplateIDs.Template.Guid)
			{
				list.Add(item.Id);
			}
		    IEnumerable<Guid> children = GetChildrenOfItem(item.Id);
			foreach (var child in children)
			{
				GetTemplatesRecursive(child, list);
			}
		    return list;
	    }


	    private List<Guid> GetMatchingItemsForWildcardPath(string query)
        {
            string parentPath = query.Substring(0, query.IndexOf(@"/*"));
            Debug.Assert(query.Length-2 == parentPath.Length, @"Path must end with ""/*""");
            Guid parentId = GetItemFromSimplePath(parentPath);
            return GetChildrenOfItem(parentId);
        }

        private List<Guid> GetMatchingItemsForDescendantPath(string query)
        {
            string parentPath = query.Substring(0, query.IndexOf(@"//*"));
            Debug.Assert(query.Length - 3 == parentPath.Length, @"Path must end with ""//*""");
            Guid parentId = GetItemFromSimplePath(parentPath);
            return GetDescendantsOfItem(parentId);
        }

        private List<Guid> GetChildrenOfItem(Guid parentId)
        {
            var itemQuery =
               from item in Items.AsQueryable()
               where item.ParentId == parentId 
                     
               select item.Id;
            return itemQuery.ToList();
        }
        
        private List<Guid> GetDescendantsOfItem(Guid parentId)
        {
            List<Guid> children = GetChildrenOfItem(parentId);
            List<Guid> descendants = new List<Guid>(children);
            foreach (var child in children)
            {
                descendants.AddRange(GetDescendantsOfItem(child));
            }
            return descendants;
        }

        private Guid GetItemFromSimplePath(string query)
        {
//implement basic path look up (e.g. /sitecore/Layout)

            var parts = query.Split('/').Select(s => s.ToLower()).Skip(1);

            //Note: Sitecore FastQuery paths always begin with the root.

            var parentId = Guid.Empty;
            foreach (var pathElement in parts)
            {
                var pathElementWithoutHashes = StripOffHashes(pathElement);
                Guid itemId = GetChildItemWithMatchingName(parentId, pathElementWithoutHashes);
                parentId = itemId;
            }
            return parentId;
        }

        private string StripOffHashes(string pathElement)
        {
            if (pathElement.StartsWith("#") && pathElement.EndsWith("#"))
                return pathElement.Substring(1, pathElement.Length - 2);
            return pathElement;
        }

        private Guid GetChildItemWithMatchingName(Guid parentId, string pathElement)
        {
            var itemQuery =
                from item in Items.AsQueryable()
                where item.ParentId == parentId &&
                      (item.Key == pathElement || item.Name == pathElement)
                select item;
                //TODO Fix data transfer to populate key consistently, and remove "|| item.Name == pathElement" above.

            var itemDto = itemQuery.FirstOrDefault();
            return itemDto == null ? Guid.Empty : itemDto.Id;
        }

        /// <summary>
        /// checks a series of parent items
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parentKeys"></param>
        /// <returns></returns>
        public bool CheckParent(ItemDto item, IEnumerable<string> parentKeys)
        {
            //catch the sitecore root item
            if (item.ParentId == Guid.Empty) return true;
            
            var parent = GetItem(item.ParentId);

            if (parent.Key == CleanKey(parentKeys.First()))
            {
               return  CheckParent(parent, parentKeys.Skip(1));
            }
            else return false;
        }

        /// <summary>
        /// Will query for an item based on the key
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IDList QuerySimple(string key)
        {

            key = CleanKey(key);

            var mongoDb = Query.EQ("Key", key);

            var results = Items.Find(mongoDb).Select(x => new ID(x.Id));

            return IDList.Build(results.ToArray());

        }

        public static string CleanKey(string key)
        {
            if (key.StartsWith("#") && key.EndsWith("#"))
            {
                key = key.Substring(1, key.Length - 2);
            }
            return key;
        }
    }
}