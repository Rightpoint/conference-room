using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories
{
    public abstract class EntityRepository<T> where T : Entity
    {
        protected readonly MongoCollection<T> Collection;

        protected IQueryable<T> Queryable
        {
            get
            {
                return Collection.AsQueryable();
            }
            
        }

        protected EntityRepository(EntityCollectionDefinition<T> collectionDefinition)
        {
            Collection = collectionDefinition.Collection;
        } 

        public virtual void Add(T entity)
        {
            var result = Collection.Insert(entity, WriteConcern.Acknowledged);

            if (result.HasLastErrorMessage)
            {
                throw new Exception(result.LastErrorMessage);
            }
        }

        public virtual void Delete(string id)
        {
            var result = Collection.Remove(Query<T>.EQ(e => e.Id, id), RemoveFlags.None, WriteConcern.Acknowledged);

            if (result.HasLastErrorMessage)
            {
                throw new Exception(result.LastErrorMessage);
            }
        }

        public virtual T GetById(string id)
        {
            return Queryable.SingleOrDefault(e => e.Id == id);
        }

        public virtual IEnumerable<T> GetByIds(IEnumerable<string> ids)
        {
            return Queryable.Where(e => ids.Contains(e.Id));
        }

        public virtual void Update(T entity)
        {
            var result = Collection.Save(entity, WriteConcern.Acknowledged);

            if (result.HasLastErrorMessage)
            {
                throw new Exception(result.LastErrorMessage);
            }
        }

        public virtual IEnumerable<T> GetAll()
        {
            return Queryable.AsEnumerable();
        }

        public virtual string NextIdentity()
        {
            return ObjectId.GenerateNewId(DateTime.Now).ToString();
        }

        protected static void AssertAffected(WriteConcernResult result, int expectedAffected)
        {
            if (result.DocumentsAffected != expectedAffected)
            {
                throw new Exception($"Expected to affect {expectedAffected} documents, but affected {result.DocumentsAffected}");
            }
        }
    }
}