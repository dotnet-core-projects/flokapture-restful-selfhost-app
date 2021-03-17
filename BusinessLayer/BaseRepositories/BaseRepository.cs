using BusinessLayer.DbEntities;
using BusinessLayer.ExtensionLibrary;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable StaticMemberInGenericType

namespace BusinessLayer.BaseRepositories
{
    public class BaseRepository<TSource> : MongoCollectionBase<TSource>, IBaseRepository<TSource>, IRepository<TSource>, IDisposable where TSource : EntityBase
    {
        private IntPtr _nativeResource = Marshal.AllocHGlobal(100);
        public static string ProjectPath = AppDomain.CurrentDomain.BaseDirectory.Split(new[] { @"bin\" }, StringSplitOptions.None).First();
        public static IConfigurationRoot ConfigurationRoot = new ConfigurationBuilder().SetBasePath(ProjectPath).AddJsonFile("appsettings.json").Build();
        private static readonly string MongoDb = ConfigurationRoot.GetSection("Database:Database").Value;
        /*
        private static readonly MongoClientSettings ClientSettings = new MongoClientSettings
        {
            Scheme = ConnectionStringScheme.MongoDB,
            ReadEncoding = new UTF8Encoding(),
            Server = new MongoServerAddress(ConfigurationRoot.GetSection("Database:Host").Value, int.Parse(ConfigurationRoot.GetSection("Database:Port").Value)),
            AllowInsecureTls = true,            
            Credential = MongoCredential.CreateCredential(MongoDb, ConfigurationRoot.GetSection("Database:User").Value, ConfigurationRoot.GetSection("Database:Pwd").Value),
            IPv6 = true,
            UseTls = true,            
            SslSettings = new SslSettings
            {
                CheckCertificateRevocation = false,
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                ClientCertificates = new List<X509Certificate>
                    {
                        new X509Certificate(Path.Combine(ProjectPath, "certificates", "device.pfx"), "yogeshs")
                    }
            }             
        };
        */
        public static string ConnectionString = $"mongodb://{ConfigurationRoot.GetSection("Database:User").Value}:{ConfigurationRoot.GetSection("Database:Pwd").Value}@{ConfigurationRoot.GetSection("Database:Host").Value}:{int.Parse(ConfigurationRoot.GetSection("Database:Port").Value)}/?ssl=true&sslVerifyCertificate=false";
        public static MongoClientSettings ClientSettings = MongoClientSettings.FromUrl(new MongoUrl(ConnectionString));
        public MongoClient MongoClient = new MongoClient(ClientSettings);

        // If need to compulsory override, then make method as abstract
        public virtual void CreateIndex(IndexKeysDefinitionBuilder<TSource> definitionBuilder)
        {
        }

        public IMongoCollection<TSource> MongoCollection => MongoDatabase.GetCollection<TSource>(typeof(TSource).Name);

        public IMongoDatabase MongoDatabase => MongoClient.GetDatabase(MongoDb);

        public IndexKeysDefinitionBuilder<TSource> IndexKeys => Builders<TSource>.IndexKeys;

        public FilterDefinitionBuilder<TSource> Filter => Builders<TSource>.Filter;

        public ProjectionDefinitionBuilder<TSource> Projection => Builders<TSource>.Projection;

        public SortDefinitionBuilder<TSource> Sort => new SortDefinitionBuilder<TSource>(); // Builders<TSource>.Sort;

        public UpdateDefinitionBuilder<TSource> Update => new UpdateDefinitionBuilder<TSource>();

        public virtual IAggregateFluent<TSource> Aggregate()
        {
            var aggregateFluent = MongoCollection.Aggregate();
            if (!typeof(TSource).IsClass) return aggregateFluent;

            var propertyInfo = typeof(TSource).GetProperty("Lookup");
            if (propertyInfo == null) return aggregateFluent;

            var lookups = (Dictionary<string, EntityLookup>)propertyInfo.GetValue("Lookup");

            return aggregateFluent.ApplyLookup(lookups);
        }

        public virtual IAggregateFluent<TSource> Aggregate(List<BsonDocument> pipelineDefinition)
        {
            return Aggregate().AppendStages(pipelineDefinition);
        }

        public virtual IAsyncCursor<TSource> Aggregate(PipelineDefinition<TSource, TSource> pipelineDefinition)
        {
            return MongoCollection.Aggregate(pipelineDefinition);
        }

        public virtual IAggregateFluent<TSource> Aggregate(params BsonDocument[] bsonDocuments)
        {
            var asyncCuror = MongoCollection.Aggregate();
            if (bsonDocuments.Length <= 0) return asyncCuror;

            var lstStages = bsonDocuments.ToList();
            return asyncCuror.AppendStages(lstStages);
        }

        public virtual IAggregateFluent<TSource> GroupByField(string groupByField)
        {
            var projectionDefinition =
                new BsonDocumentProjectionDefinition<TSource, TSource>(new BsonDocument { { "_id", $"${groupByField}" } }
                    .AddRange(new[]
                    {
                        new BsonElement("Document", new BsonDocument {{"$first", "$$ROOT"}})
                    }));
            return MongoCollection.Aggregate().Group(projectionDefinition).As<TSource>(); // .BsonDocLookup();
        }

        public virtual IEnumerable<TSource> AllDocuments()
        {
            return MongoCollection.AsQueryable().AsEnumerable();
        }

        public virtual IEnumerable<T> AllDocumentsOf<T>(Expression<Func<T, bool>> expression) where T : EntityBase
        {
            return MongoDatabase.GetCollection<T>(typeof(T).Name).AsQueryable().Where(expression).AsEnumerable();
        }

        public List<TSource> ListAllDocuments()
        {
            return MongoCollection.AsQueryable().ToList();
        }
        public virtual List<TSource> ListAllDocuments(Expression<Func<TSource, bool>> expression)
        {
            return MongoCollection.AsQueryable().Where(expression).ToList();
        }

        public virtual async Task<TSource> AddDocument(TSource itemSource)
        {
            await MongoCollection.InsertOneAsync(itemSource).ConfigureAwait(false);
            return itemSource;
        }

        public virtual async Task<TSource> UpdateDocument(TSource itemSource)
        {
            var filter = Filter.Eq("_id", itemSource._id);
            await MongoCollection.ReplaceOneAsync(filter, itemSource).ConfigureAwait(false);
            return itemSource;
        }

        public virtual async Task<int> BulkInsert(List<TSource> listOfEntities)
        {
            await MongoCollection.InsertManyAsync(listOfEntities).ConfigureAwait(false);
            return 1;
        }

        public async Task<DeleteResult> DeleteDocument(string tKey)
        {
            var filter = Filter.Eq("_id", tKey);
            return await MongoCollection.DeleteOneAsync(filter).ConfigureAwait(false);
        }

        public virtual T DeleteDocument<T>(string id)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            return MongoDatabase.GetCollection<T>(typeof(T).Name).FindOneAndDelete(filter);
        }

        public virtual TSource GetDocument(string id)
        {
            return MongoCollection.AsQueryable().FirstOrDefault(t => t._id == id);
        }

        public virtual IAggregateFluent<TSource> FindWithLookup(FilterDefinition<TSource> filterDefinition)
        {
            var findFluent = MongoCollection.Find(FilterDefinition<TSource>.Empty);
            var bsonDocuments = findFluent.ApplyLookup(filterDefinition);
            return Aggregate(bsonDocuments); // .ToList();
        }

        public virtual TSource GetDocument(Expression<Func<TSource, bool>> expression)
        {
            return MongoCollection.FindSync(expression).ToList().First();
        }

        public virtual T GetDocument<T>(Expression<Func<T, bool>> expression) where T : EntityBase
        {
            return MongoDatabase.GetCollection<T>(typeof(T).Name).AsQueryable().FirstOrDefault(expression);
        }

        public virtual int UpdateDocuments(IEnumerable<TSource> updateSource)
        {
            int updateCount = 0;
            foreach (var itemSource in updateSource)
            {
                updateCount++;
                var filter = Filter.Eq("_id", itemSource._id);
                MongoCollection.ReplaceOne(filter, itemSource);
            }
            return updateCount;
        }

        public virtual TSource FindDocument(Expression<Func<TSource, bool>> expression)
        {
            return MongoCollection.Find(expression).FirstOrDefault();
        }

        public void DeleteDocument<T>(Expression<Func<T, bool>> expression) where T : class, new()
        {
            var items = All<T>().Where(expression);
            foreach (var item in items) { DeleteDocument(item); }
        }

        public void DeleteDocument<T>(T item) where T : class, new()
        {
            MongoCollection.DeleteOne(a => a.Equals(item));
        }

        public void DeleteAll<T>() where T : class, new()
        {
            MongoDatabase.DropCollection(typeof(T).Name);
        }

        public T Single<T>(Expression<Func<T, bool>> expression) where T : class, new()
        {
            return All<T>().Where(expression).SingleOrDefault();
        }

        public IQueryable<T> All<T>() where T : class, new()
        {
            return MongoDatabase.GetCollection<T>(typeof(T).Name).AsQueryable();
        }

        public IQueryable<T> All<T>(int page, int pageSize) where T : class, new()
        {
            return null;
            // TODO: This is yet to implement
            // return PagingExtensions.Page(All<T>(), page, pageSize);
        }

        public void Add<T>(T item) where T : class, new()
        {
            MongoDatabase.GetCollection<T>(typeof(T).Name).InsertOne(item);
        }

        public void Add<T>(IEnumerable<T> items) where T : class, new()
        {
            foreach (var item in items) { Add(item); }
        }

        public virtual IQueryable<TSource> AsQueryable()
        {
            return MongoDatabase.GetCollection<TSource>(typeof(TSource).Name).AsQueryable();
        }

        public virtual async Task<TSource> DeleteItem(TSource itemSource)
        {
            var filterDefinition = new JsonFilterDefinition<TSource>(itemSource.ToJson());
            await MongoCollection.DeleteOneAsync(filterDefinition).ConfigureAwait(false);
            return itemSource;
        }

        public IMongoCollection<T> MongoCollectionOf<T>() where T : EntityBase
        {
            return MongoDatabase.GetCollection<T>(typeof(T).Name);
        }

        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
        {
            throw new NotImplementedException();
        }

        public TSource InsertDocument(TSource entity)
        {
            MongoCollection.InsertOne(entity);
            return entity;
        }

        public void Insert(TSource entity, Action<TSource> callback)
        {
            MongoCollection.InsertOne(entity);
            callback(entity);
        }

        public ReplaceOneResult ReplaceOneDocument(TSource entity)
        {
            var filter = Filter.Eq(x => x._id, entity._id);
            return MongoCollection.ReplaceOne(filter, entity);
        }

        public ReplaceOneResult ReplaceOne(TSource entity, Action<TSource> callback)
        {
            var filter = Filter.Eq(x => x._id, entity._id);
            var replaceOneResult = MongoCollection.ReplaceOne(filter, entity);
            callback(entity);
            return replaceOneResult;
        }

        public DeleteResult DeleteDocument(FilterDefinition<TSource> filterDefinition)
        {
            return MongoCollection.DeleteOne(filterDefinition);
        }

        public DeleteResult Delete(string id, Action<DeleteResult> callback)
        {
            var deleteResult = MongoCollection.DeleteOne(Filter.Eq(x => x._id, id));
            callback(deleteResult);
            return deleteResult;
        }

        public IMongoQueryable<TSource> Where(Expression<Func<TSource, bool>> expression)
        {
            return MongoCollection.AsQueryable().Where(expression);
        }

        public IMongoQueryable<TSource> Select()
        {
            return MongoCollection.AsQueryable();
        }

        public IMongoQueryable<TSource> Paging(int skip, int take)
        {
            return MongoCollection.AsQueryable().Skip(skip).Take(take);
        }

        public TSource GetById(string id)
        {
            return MongoCollection.Find(Filter.Eq(x => x._id, id)).FirstOrDefault();
        }

        public int Count()
        {
            return MongoCollection.AsQueryable().Count();
        }

        public override void InsertMany(IEnumerable<TSource> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            MongoCollection.InsertMany(documents, options, cancellationToken);
        }

        public override IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<TSource, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return MongoCollection.Aggregate(pipeline, options, cancellationToken);
        }

        public override Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TSource, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.Factory.StartNew(() => Aggregate(pipeline, options), cancellationToken);
        }

        public override async Task<BulkWriteResult<TSource>> BulkWriteAsync(IEnumerable<WriteModel<TSource>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return await Task.Factory.StartNew(() =>
                BulkWrite(null, requests, options), cancellationToken).ConfigureAwait(false);
        }

        public override async Task<long> CountDocumentsAsync(FilterDefinition<TSource> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return await Task.Factory.StartNew(() => MongoCollection.CountDocuments(filter, options), cancellationToken).ConfigureAwait(false);
        }

        [Obsolete]
        public override async Task<long> CountAsync(FilterDefinition<TSource> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return await MongoCollection.Find(filter).CountDocumentsAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TSource, TField> field, FilterDefinition<TSource> filter, DistinctOptions options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return await Task.Factory.StartNew(() => Distinct(field, filter, options), cancellationToken).ConfigureAwait(false);
        }

        public override Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TSource> filter, FindOptions<TSource, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return MongoCollection.FindAsync(session, filter, options, cancellationToken); //.GetAwaiter().GetResult();
        }

        public override IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<TSource> filter, FindOptions<TSource, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            // TODO: Needs to check whether we can implement FindWithLookup method here...
            /*
            var findFluent = MongoCollection.Find(FilterDefinition<TSource>.Empty);
            var bsonDocuments = findFluent.ApplyLookup(filter);
            Console.WriteLine(bsonDocuments);
            */
            return MongoCollection.FindAsync(session, filter, options, cancellationToken).GetAwaiter().GetResult();
        }

        public override IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<TSource> filter, FindOptions<TSource, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // TODO: Needs to check whether we can implement FindWithLookup method here...
            /*
            var findFluent = MongoCollection.Find(FilterDefinition<TSource>.Empty);
            var bsonDocuments = findFluent.ApplyLookup(filter);
            Console.WriteLine(bsonDocuments);
            */
            return MongoCollection.FindAsync(filter, options, cancellationToken).GetAwaiter().GetResult();
        }
        public override Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TSource> filter, FindOptions<TSource, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            var findFluent = MongoCollection.Find(FilterDefinition<TSource>.Empty);
            var bsonDocuments = findFluent.ApplyLookup(filter);
            return Task.Factory.StartNew(() => Aggregate(bsonDocuments).As<TProjection>().Out(typeof(TProjection).Name), cancellationToken);
            // return MongoCollection.Aggregate(filter).ToCursorAsync(cancellationToken);
        }

        public override Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TSource> filter, FindOneAndDeleteOptions<TSource, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TSource> filter, TSource replacement, FindOneAndReplaceOptions<TSource, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TSource> filter, UpdateDefinition<TSource> update, FindOneAndUpdateOptions<TSource, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override async Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TSource, TResult> options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return await MongoCollection.MapReduceAsync(map, reduce, options, cancellationToken).ConfigureAwait(false);
        }

        public override IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>()
        {
            return MongoCollection.OfType<TDerivedDocument>();
        }

        public override IMongoCollection<TSource> WithReadPreference(ReadPreference readPreference)
        {
            return MongoCollection.WithReadPreference(readPreference);
        }

        public override IMongoCollection<TSource> WithWriteConcern(WriteConcern writeConcern)
        {
            return MongoCollection.WithWriteConcern(writeConcern);
        }

        public override CollectionNamespace CollectionNamespace => MongoCollection.CollectionNamespace;

        public override IMongoDatabase Database => MongoDatabase;

        public override IBsonSerializer<TSource> DocumentSerializer => MongoCollection.DocumentSerializer;

        public override IMongoIndexManager<TSource> Indexes => MongoCollection.Indexes;

        public override MongoCollectionSettings Settings => MongoCollection.Settings;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BaseRepository()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_nativeResource == IntPtr.Zero) return;
            Marshal.FreeHGlobal(_nativeResource);
            _nativeResource = IntPtr.Zero;
        }
    }

    public class LogMongoEvents : IEventSubscriber
    {
        private readonly ReflectionEventSubscriber _eventSubscriber;

        public LogMongoEvents()
        {
            _eventSubscriber = new ReflectionEventSubscriber(this);
        }

        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
        {
            return _eventSubscriber.TryGetEventHandler(out handler);
        }

        public void Handle(CommandStartedEvent startedEvent)
        {
            if (startedEvent.CommandName != "aggregate" && startedEvent.CommandName != "find") return;
            Console.WriteLine("============================================");
            Console.WriteLine("Event Name: " + startedEvent.CommandName);
            Console.WriteLine("============================================");
        }

        public void Handle(CommandSucceededEvent startedEvent)
        {
            Console.WriteLine("============================================");
            Console.WriteLine("Event Name: " + startedEvent.CommandName);
            Console.WriteLine("============================================");
        }
    }
}