using BusinessLayer.DbEntities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BusinessLayer.BaseRepositories
{
    public interface IBaseRepository<TSource> : IEventSubscriber where TSource : EntityBase
    {
        void CreateIndex(IndexKeysDefinitionBuilder<TSource> definitionBuilder);

        // IMongoDatabase MongoDatabase { get; }  

        IMongoCollection<TSource> Collection { get; }

        IndexKeysDefinitionBuilder<TSource> IndexKeys { get; }

        FilterDefinitionBuilder<TSource> Filter { get; }

        ProjectionDefinitionBuilder<TSource> Projection { get; }

        SortDefinitionBuilder<TSource> Sort { get; }

        UpdateDefinitionBuilder<TSource> Update { get; }
        /// <summary>
        /// 
        /// </summary>
        // IMongoCollection<TSource> MongoCollection { get; }

        /// <summary>
        /// This is custom aggregate with lookup for all navigation and nested navigation properties.
        /// There should be property configured on entity model with name Lookup for all 
        /// navigation and nested navigation properties.
        /// </summary>
        /// <returns></returns>
        IAggregateFluent<TSource> Aggregate();

        /// <summary>   
        /// 
        /// </summary>
        /// <param name="pipelineDefinition"></param>
        /// <returns></returns>
        IAsyncCursor<TSource> Aggregate(PipelineDefinition<TSource, TSource> pipelineDefinition);

        /// <summary>
        /// Aggregate stages defined from BsonDocument.
        /// </summary>
        /// <param name="bsonDocuments"></param>
        /// <returns>IAsyncCursor&lt;TSource&gt;</returns>
        IAggregateFluent<TSource> Aggregate(params BsonDocument[] bsonDocuments);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupByField"></param>
        /// <returns>IAggregateFluent&lt;TSource&gt;</returns>
        IAggregateFluent<TSource> GroupByField(string groupByField);

        /// <summary>
        ///     Returns the IEnumerable of TSource which is Generic input type
        /// </summary>
        /// <returns></returns>
        IEnumerable<TSource> GetAllItems();

        /// <summary>
        ///     Returns the IEnumerable of TSource which is Generic input type
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> GetAllItemsOf<T>(Expression<Func<T, bool>> expression) where T : EntityBase;

        /// <summary>
        ///     Returns the IEnumerable of TSource which is Generic input type
        /// </summary>
        /// <returns></returns>
        List<TSource> GetAllListItems();

        List<TSource> GetAllListItems(Expression<Func<TSource, bool>> expression);

        /// <summary>
        ///     Returns the int
        /// </summary>
        /// <param name="itemSource"></param>
        /// <returns></returns>
        Task<TSource> AddDocument(TSource itemSource);

        /// <summary>
        /// </summary>
        /// <param name="itemSource"></param>
        Task<TSource> UpdateDocument(TSource itemSource);

        /// <summary>
        /// Returns int
        /// </summary>
        /// <param name="listOfEntities"></param>
        /// <returns></returns>
        Task<int> BulkInsert(List<TSource> listOfEntities);

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<DeleteResult> DeleteDocument(string id);

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        T DeleteDocument<T>(string id);

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        TSource GetDocument(string id);

        /// <summary>
        /// IFindFluent with lookup stage. Using aggregation pipline $lookup internally...
        /// </summary>
        /// <param name="filterDefinition"></param>
        /// <returns>List</returns>
        /// <summary>
        /// Please do not use anywhere <code>System.Linq.Exressions.Expression</code> anywhere in FilterDefinition
        /// </summary>
        IAggregateFluent<TSource> FindWithLookup(FilterDefinition<TSource> filterDefinition);

        /// <summary>
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        TSource GetDocument(Expression<Func<TSource, bool>> expression);

        IQueryable<TSource> AsQueryable();

        /// <summary>
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        T GetDocument<T>(Expression<Func<T, bool>> expression) where T : EntityBase;

        /// <summary>
        /// </summary>
        /// <param name="updateSource"></param>
        int UpdateDocuments(IEnumerable<TSource> updateSource);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        TSource FindDocument(Expression<Func<TSource, bool>> expression);

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        void DeleteDocument<T>(Expression<Func<T, bool>> expression) where T : class, new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        void DeleteDocument<T>(T item) where T : class, new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void DeleteAll<T>() where T : class, new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        T Single<T>(Expression<Func<T, bool>> expression) where T : class, new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IQueryable<T> All<T>() where T : class, new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        IQueryable<T> All<T>(int pageNumber, int pageSize) where T : class, new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        void Add<T>(T item) where T : class, new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        void Add<T>(IEnumerable<T> items) where T : class, new();
    }
}
