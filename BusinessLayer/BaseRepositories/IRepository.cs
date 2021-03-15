using System;
using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace BusinessLayer.BaseRepositories
{
    public interface IRepository<TEntity>
    {
        int Count();

        DeleteResult DeleteDocument(FilterDefinition<TEntity> filterDefinition);

        TEntity GetById(string id);

        TEntity InsertDocument(TEntity entity);

        IMongoQueryable<TEntity> Paging(int skip, int take);

        IMongoQueryable<TEntity> Select();

        ReplaceOneResult ReplaceOneDocument(TEntity entity);

        IMongoQueryable<TEntity> Where(Expression<Func<TEntity, bool>> expression);
    }
}
