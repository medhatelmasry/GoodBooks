﻿using System;
using System.Linq;
using Core.Data;
using Core.Domain;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Api.Data
{
    public class EfRepository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly ApiDbContext _context;
        private readonly ILogger<T> _logger;
        private DbSet<T>? _entities = null;

        public EfRepository(ILogger<T> logger, ApiDbContext context)
        {
            _logger = logger;
            _context = context;
        }
         
        #region Properties

        /// <summary>
        /// Gets a table
        /// </summary>
        public virtual IQueryable<T> Table
        {
            get
            {
                return this.Entities;
            }
        }

        /// <summary>
        /// Gets a table with "no tracking" enabled (EF feature) Use it only when you load record(s) only for read-only operations
        /// </summary>
        public virtual IQueryable<T> TableNoTracking
        {
            get
            {
                return this.Entities.AsNoTracking();
            }
        }

        /// <summary>
        /// Entities
        /// </summary>
        protected virtual DbSet<T> Entities
        {
            get
            {
                if (_entities == null)
                    _entities = _context.Set<T>();
                return _entities;
            }
        }

        #endregion
        #region Ctor


        #endregion
        #region Methods

        /// <summary>
        /// Get entity by identifier
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <returns>Entity</returns>
        public virtual T GetById(object id)
        {
            return this.Entities.FirstOrDefault(x => x.Id == (int)id)!;
        }

        /// <summary>
        /// Insert entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual void Insert(T entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                this.Entities.Add(entity);

                this._context.SaveChanges();

            }
            //catch (DbEntityValidationException dbEx)
            //{
            //    var msg = string.Empty;

            //    foreach (var validationErrors in dbEx.EntityValidationErrors)
            //        foreach (var validationError in validationErrors.ValidationErrors)
            //            msg += string.Format("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage) + Environment.NewLine;

            //    var fail = new Exception(msg, dbEx);
            //    //Debug.WriteLine(fail.Message, fail);
            //    throw fail;
            //}
            catch (Exception ex) {
                _logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Insert entities
        /// </summary>
        /// <param name="entities">Entities</param>
        public virtual void Insert(IEnumerable<T> entities)
        {
            try
            {
                if (entities == null)
                    throw new ArgumentNullException("entities");

                foreach (var entity in entities)
                    this.Entities.Add(entity);

                this._context.SaveChanges();
            }
            //catch (DbEntityValidationException dbEx)
            //{
            //    var msg = string.Empty;

            //    foreach (var validationErrors in dbEx.EntityValidationErrors)
            //        foreach (var validationError in validationErrors.ValidationErrors)
            //            msg += string.Format("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage) + Environment.NewLine;

            //    var fail = new Exception(msg, dbEx);
            //    //Debug.WriteLine(fail.Message, fail);
            //    throw fail;
            //}
            catch (Exception ex) {
                _logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Insert entity asynchronously
        /// </summary>
        /// <param name="entities">Entity</param>
        public async virtual Task InsertAsync(T entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                this.Entities.Add(entity);

                await this._context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Update entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual void Update(T entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                _context.SaveChanges();
            }
            //catch (DbEntityValidationException dbEx)
            //{
            //    var msg = string.Empty;

            //    foreach (var validationErrors in dbEx.EntityValidationErrors)
            //        foreach (var validationError in validationErrors.ValidationErrors)
            //            msg += Environment.NewLine + string.Format("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);

            //    var fail = new Exception(msg, dbEx);
            //    //Debug.WriteLine(fail.Message, fail);
            //    throw fail;
            //}
            catch (Exception ex) {
                _logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Update entity asynchronously
        /// </summary>
        /// <param name="entity">Entity</param>
        public async virtual Task UpdateAsync(T entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Delete entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual void Delete(T entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                this.Entities.Remove(entity);

                this._context.SaveChanges();
            }
            //catch (DbEntityValidationException dbEx)
            //{
            //    var msg = string.Empty;

            //    foreach (var validationErrors in dbEx.EntityValidationErrors)
            //        foreach (var validationError in validationErrors.ValidationErrors)
            //            msg += Environment.NewLine + string.Format("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);

            //    var fail = new Exception(msg, dbEx);
            //    //Debug.WriteLine(fail.Message, fail);
            //    throw fail;
            //}
            catch (Exception ex) {
                _logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Delete entity asynchronously
        /// </summary>
        /// <param name="entity">Entity</param>
        public async virtual Task DeleteAsync(T entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                this.Entities.Remove(entity);

                await this._context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Delete entities
        /// </summary>
        /// <param name="entities">Entities</param>
        public virtual void Delete(IEnumerable<T> entities)
        {
            try
            {
                if (entities == null)
                    throw new ArgumentNullException("entities");

                foreach (var entity in entities)
                    this.Entities.Remove(entity);

                this._context.SaveChanges();
            }
            //catch (DbEntityValidationException dbEx)
            //{
            //    var msg = string.Empty;

            //    foreach (var validationErrors in dbEx.EntityValidationErrors)
            //        foreach (var validationError in validationErrors.ValidationErrors)
            //            msg += Environment.NewLine + string.Format("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);

            //    var fail = new Exception(msg, dbEx);
            //    //Debug.WriteLine(fail.Message, fail);
            //    throw fail;
            //}
            catch (Exception ex) {
                _logger.LogError(ex.Message);
            }
        }

        public IQueryable<T> GetAllIncluding(params System.Linq.Expressions.Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> queryable = Entities;
            foreach (System.Linq.Expressions.Expression<Func<T, object>> includeProperty in includeProperties)
            {
                queryable = queryable.Include<T, object>(includeProperty);
            }

            return queryable;
        }

        #endregion
    }
}
