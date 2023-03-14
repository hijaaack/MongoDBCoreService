using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDBCoreService.db
{
    public class MongoDBData
    {
        #region DECLARATION
        private readonly MongoClient _mongoClient;
        private readonly IMongoDatabase _db;
        private IMongoCollection<BsonDocument> _collection;

        private BsonDocument[] _pipelineQuery1, _pipelineQuery2, _pipelineQuery3 = null;
        private string _pipelineCollection1, _pipelineCollection2, _pipelineCollection3 = null;

        private List<dynamic> _aggregationOutputList1, _aggregationOutputList2, _aggregationOutputList3;
        private object _aggregationOutputListLock1 = new object();
        private object _aggregationOutputListLock2 = new object();
        private object _aggregationOutputListLock3 = new object();

        private List<dynamic> _collectionList;
        private object _collectionListLock = new object();
        #endregion DECLARATION

        #region PROPERTIES
        public List<dynamic> CollectionList
        {
            get
            {
                lock (_collectionListLock)
                {
                    _collectionList = GetCollectionList();
                    return _collectionList;
                }
            }
        }

        public List<dynamic> AggregationOutputList1
        {
            get
            {
                lock (_aggregationOutputListLock1)
                {
                    GetAggregateCollection(_pipelineQuery1, _pipelineCollection1, 1);
                    return _aggregationOutputList1;
                }
            }
        }

        public List<dynamic> AggregationOutputList2
        {
            get
            {
                lock (_aggregationOutputListLock2)
                {
                    GetAggregateCollection(_pipelineQuery2, _pipelineCollection2, 2);
                    return _aggregationOutputList2;
                }
            }
        }

        public List<dynamic> AggregationOutputList3
        {
            get
            {
                lock (_aggregationOutputListLock3)
                {
                    GetAggregateCollection(_pipelineQuery3, _pipelineCollection3, 3);
                    return _aggregationOutputList3;
                }
            }
        }
        #endregion PROPERTIES

        #region CTOR
        public MongoDBData(string databaseName, string connectionString)
        {
            //Connect
            _mongoClient = new MongoClient(connectionString);
            _db = _mongoClient.GetDatabase(databaseName);
        }
        #endregion

        #region PRIVATE METHODS
        private void GetAggregateCollection(BsonDocument[] pipeline, string collection, int pipelineId)
        {
            if (pipeline == null || string.IsNullOrWhiteSpace(collection))
                return;

            _collection = _db.GetCollection<BsonDocument>(collection);
            List<dynamic> list = new List<dynamic>();
            using (var cursor = _collection.Aggregate<dynamic>(pipeline))
            {
                list = cursor.ToList();
            }

            if (list.Count == 0)
                list = new List<dynamic>();

            switch (pipelineId)
            {
                case 1:
                    _aggregationOutputList1 = list;
                    break;

                case 2:
                    _aggregationOutputList2 = list;
                    break;

                case 3:
                    _aggregationOutputList3 = list;
                    break;
            }

        }

        private List<dynamic> GetAggregateCollection(BsonDocument[] pipeline, string collection)
        {
            if (pipeline == null || string.IsNullOrWhiteSpace(collection))
                return new List<dynamic>();

            _collection = _db.GetCollection<BsonDocument>(collection);
            List<dynamic> list = new List<dynamic>();
            using (var cursor = _collection.Aggregate<dynamic>(pipeline))
            {
                list = cursor.ToList();
            }

            if (list == null)
                return new List<dynamic>();
            else
                return list;
        }

        private List<string> GetCollectionsNames()
        {
            List<string> collections = new List<string>();

            foreach (BsonDocument collection in _db.ListCollectionsAsync().Result.ToListAsync<BsonDocument>().Result)
            {
                string name = collection["name"].AsString;
                collections.Add(name);
            }

            return collections;
        }
        private List<dynamic> GetCollectionList()
        {
            List<dynamic> collections = new List<dynamic>();
            List<string> collectionsName = GetCollectionsNames();
            foreach (var name in collectionsName)
            {
                var collection = _db.GetCollection<dynamic>(name);
                var collectionList = collection.AsQueryable().ToList();
                var newJson = JsonConvert.SerializeObject(collectionList);
                var jsonData = @"{" + name + ':' + newJson + "}";
                var jsonObject = (JObject)JsonConvert.DeserializeObject(jsonData);
                collections.Add(jsonObject);
            }

            return collections;
        }
        #endregion PRIVATE METODS

        #region PUBLIC METHODS
        public void UpdateDocument(string collectionName, string jsonData)
        {
            _collection = _db.GetCollection<BsonDocument>(collectionName);

            //Create JSON Object
            var jsonObject = (JObject)JsonConvert.DeserializeObject(jsonData);

            //Read the _id and parse it to correct type
            var id = ObjectId.Parse(jsonObject["_id"].Value<string>());

            //Remove _id due to its not possible to pass it in when using UpdateOne
            jsonObject.Property("_id").Remove();

            //Serialize the object back to json without the _id property
            var newJson = JsonConvert.SerializeObject(jsonObject);

            //Create update set with correct format
            var updateDefinitionSet = @"{'$set':" + newJson + "}";

            //Update the document by id
            _collection.UpdateOne(
                Builders<BsonDocument>.Filter.Eq("_id", id),
                updateDefinitionSet
            );
        }

        public void CreateDocument(string collectionName, string jsonData)
        {
            _collection = _db.GetCollection<BsonDocument>(collectionName);
            var document = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            _collection.InsertOne(document);
        }

        public void CreateCollection(string collectionName)
        {
            _db.CreateCollection(collectionName);
        }

        public void RemoveDocument(string collectionName, string id)
        {
            _collection = _db.GetCollection<BsonDocument>(collectionName);
            var parseId = ObjectId.Parse(id);
            _collection.DeleteOne(
                Builders<BsonDocument>.Filter.Eq("_id", parseId));
        }

        public void SetAggregationPipeline1(string collectionName, string pipelineData)
        {
            _pipelineCollection1 = null;
            _pipelineQuery1 = null;

            _pipelineCollection1 = collectionName;
            _pipelineQuery1 = BsonSerializer.Deserialize<BsonDocument[]>(pipelineData);
        }

        public void SetAggregationPipeline2(string collectionName, string pipelineData)
        {
            _pipelineCollection2 = null;
            _pipelineQuery2 = null;

            _pipelineCollection2 = collectionName;
            _pipelineQuery2 = BsonSerializer.Deserialize<BsonDocument[]>(pipelineData);
        }

        public void SetAggregationPipeline3(string collectionName, string pipelineData)
        {
            _pipelineCollection3 = null;
            _pipelineQuery3 = null;

            _pipelineCollection3 = collectionName;
            _pipelineQuery3 = BsonSerializer.Deserialize<BsonDocument[]>(pipelineData);
        }

        public List<dynamic> GetAggregationResult(string collectionName, string pipelineData)
        {
            var query = BsonSerializer.Deserialize<BsonDocument[]>(pipelineData);
            var list = GetAggregateCollection(query, collectionName);
            return list;
        }

        #endregion PUBLIC METODS
    }
}