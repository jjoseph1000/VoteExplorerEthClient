using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace VoteExplorer.Models
{
    public class GroupService : IDisposable
    {
        private static bool UpdateDatabase = false;
        private VoteExplorerContext Context;

        public GroupService(VoteExplorerContext context)
        {
            this.Context = context;
        }

        public IList<GroupVM> GetAll()
        {
            IMongoQueryable<Group> groups = Context.groups.AsQueryable();
            IMongoQueryable<Meeting> meetings = Context.meetings.AsQueryable();


            IList<GroupVM> result = (from gps in groups
                                     join mtg in meetings on gps.MeetingId equals mtg._id
                                     select new GroupVM
                                     {
                                         _id = gps._id,
                                         GroupName = gps.GroupName,
                                         MeetingEntity = mtg.Entity
                                     }).ToList();


            return result;
        }

        public IEnumerable<GroupVM> Read()
        {
            return GetAll();
        }

        public void Create(GroupVM group)
        {
            if (!UpdateDatabase)
            {
            }
            else
            {

            }
        }

        public void Update(GroupVM product)
        {
            if (!UpdateDatabase)
            {
                //var target = One(e => e.ProductID == product.ProductID);

                //if (target != null)
                //{
                //    target.ProductName = product.ProductName;
                //    target.UnitPrice = product.UnitPrice;
                //    target.UnitsInStock = product.UnitsInStock;
                //    target.Discontinued = product.Discontinued;
                //    target.CategoryID = product.CategoryID;
                //    target.Category = product.Category;
                //}
            }
            else
            {
                ////var entity = new GroupVM();

                ////entity.ProductID = product.ProductID;
                ////entity.ProductName = product.ProductName;
                ////entity.UnitPrice = product.UnitPrice;
                ////entity.UnitsInStock = (short)product.UnitsInStock;
                ////entity.Discontinued = product.Discontinued;
                ////entity.CategoryID = product.CategoryID;

                ////if (product.Category != null)
                ////{
                ////    entity.CategoryID = product.Category.CategoryID;
                ////}

                ////entities.Products.Attach(entity);
                ////entities.Entry(entity).State = EntityState.Modified;
                ////entities.SaveChanges();
            }
        }

        public void Destroy(GroupVM group)
        {
            if (!UpdateDatabase)
            {
                var target = GetAll().FirstOrDefault(g => g._id == group._id);
                if (target != null)
                {
                    GetAll().Remove(target);
                }
            }
            else
            {
                //var entity = new Product();

                //entity.ProductID = product.ProductID;

                //entities.Products.Attach(entity);

                //entities.Products.Remove(entity);

                //var orderDetails = entities.Order_Details.Where(pd => pd.ProductID == entity.ProductID);

                //foreach (var orderDetail in orderDetails)
                //{
                //    entities.Order_Details.Remove(orderDetail);
                //}

                //entities.SaveChanges();
            }
        }

        public GroupVM One(Func<GroupVM, bool> predicate)
        {
            return GetAll().FirstOrDefault(predicate);
        }

        public void Dispose()
        {
        }
    }
}