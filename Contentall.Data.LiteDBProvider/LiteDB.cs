using Contentall.Data.Provider.Abstractions;
using LiteDB;
using System.Linq.Expressions;
using System.Reflection;

namespace Contentall.Data.LiteDBProvider
{
    public class LiteDB : EntitiesContainer
    {
        private readonly LiteDatabase db;

        public LiteDB(ConnectionString connection)
        {
            db = new LiteDatabase(connection.Filename);
        }

        public IQueryable<T> GetQueryableEntities<T>()
        {
            return db.GetCollection<T>(typeof(T).Name).FindAll().AsQueryable();
        }

        public void Insert<T>(T entity)
        {
            entity.GetType().GetProperty("Id").SetValue(entity, Guid.NewGuid().ToString(), null);
            db.GetCollection<T>(typeof(T).Name).Insert(entity);
        }

        public void EnsureIndex<T>(Expression<Func<T,object>> keySelector)
        {
            db.GetCollection<T>(typeof(T).Name).EnsureIndex(keySelector);
        }

        public void Delete<T>(string id)
        {
            db.GetCollection<T>(typeof(T).Name).Delete(id);
        }

        public void Update<T>(T entity)
        {
            db.GetCollection<T>(typeof(T).Name).Update(entity);
        }
    }
}
