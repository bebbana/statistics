using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Statistics.Data.Interfaces;

namespace Statistics.Data.Repositories
{
    /// <summary>
    /// generic repository class for basic CRUD methods management
    /// </summary>
    /// <typeparam name="TEntity">datatype will be specificated by creating of instance of descendant</typeparam>
    public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : class
    {
        protected ApplicationDbContext dbContext;
        protected DbSet<TEntity> dbSet;

        /// <summary>
        /// BaseRepository constructor
        /// </summary>
        /// <param name="appDbContext">DI service of application db context</param>
        public BaseRepository()
        {
            dbContext = new ApplicationDbContext();
            dbSet = dbContext.Set<TEntity>();
        }

        /// <summary>
        /// Get all objects of descendant data type from db context
        /// </summary>
        /// <returns>List of objects </returns>
        public List<TEntity> GetAll()
        {
            return dbSet.ToList();
        }

        /// <summary>
        /// Return descendant data type object by id
        /// </summary>
        /// <param name="id">Guid id of object</param>
        /// <returns></returns>
        public TEntity FindById(Guid id)
        {
            return dbSet.Find(id);
        }

        /// <summary>
        /// Insert object to DB.
        /// </summary>
        /// <param name="entity">descendant data type object</param>
        public void Insert(TEntity entity)
        {
            dbSet.Add(entity);
            dbContext.SaveChanges();
        }

        /// <summary>
        /// Update object in DB
        /// </summary>
        /// <param name="entity">descendant data type object</param>
        public void Update(TEntity entity)
        {
            dbSet.Update(entity);
            dbContext.SaveChanges();
        }

        /// <summary>
        /// Delete object from DB
        /// </summary>
        /// <param name="id">object id</param>
        public void Delete(Guid id)
        {
            TEntity entity = dbSet.Find(id);
            try
            {
                dbSet.Remove(entity);
                dbContext.SaveChanges();
            }
            catch (Exception)
            {
                dbContext.Entry(entity).State = EntityState.Unchanged;
                throw;
            }
        }
    }
}
