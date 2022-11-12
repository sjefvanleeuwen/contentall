using System.Linq.Expressions;

namespace Contentall.Data.Provider.Abstractions
{
    public interface EntitiesContainer
    {
        IQueryable<T> GetQueryableEntities<T>();
        void Insert<T>(T entity);
        void Update<T>(T entity);
        void Delete<T>(string id);
        void EnsureIndex<T>(Expression<Func<T, object>> keySelector);
    }
}