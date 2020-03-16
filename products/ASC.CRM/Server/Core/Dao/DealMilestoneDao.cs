/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


#region Import

using ASC.Collections;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.CRM.Core.EF;
using ASC.CRM.Core.Entities;
using ASC.CRM.Core.Enums;
using ASC.CRM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace ASC.CRM.Core.Dao
{
    public class CachedDealMilestoneDao : DealMilestoneDao
    {
        private readonly HttpRequestDictionary<DealMilestone> _dealMilestoneCache =
            new HttpRequestDictionary<DealMilestone>("crm_deal_milestone");

        public CachedDealMilestoneDao(DbContextManager<CRMDbContext> dbContextManager,
                                TenantManager tenantManager,
                                SecurityContext securityContext)
            : base(dbContextManager,
                  tenantManager,
                  securityContext)
        {



        }

        private void ResetCache(int id)
        {
            _dealMilestoneCache.Reset(id.ToString());
        }

        public override int Create(DealMilestone item)
        {
            item.ID = base.Create(item);

            _dealMilestoneCache.Add(item.ID.ToString(), item);

            return item.ID;
        }

        public override void Delete(int id)
        {
            ResetCache(id);

            base.Delete(id);
        }

        public override void Edit(DealMilestone item)
        {
            ResetCache(item.ID);

            base.Edit(item);
        }


        private DealMilestone GetByIDBase(int id)
        {
            return base.GetByID(id);
        }

        public override DealMilestone GetByID(int id)
        {
            return _dealMilestoneCache.Get(id.ToString(), () => GetByIDBase(id));
        }

        public override void Reorder(int[] ids)
        {
            _dealMilestoneCache.Clear();

            base.Reorder(ids);
        }
    }

    public class DealMilestoneDao : AbstractDao
    {

        public DealMilestoneDao(DbContextManager<CRMDbContext> dbContextManager,
                                TenantManager tenantManager,
                                SecurityContext securityContext) :
                                                                    base(dbContextManager,
                                                                         tenantManager,
                                                                         securityContext)
        {




        }

        public virtual void Reorder(int[] ids)
        {
            using var tx = CRMDbContext.Database.BeginTransaction();

            for (int index = 0; index < ids.Length; index++)
            {
                var itemToUpdate = Query(CRMDbContext.DealMilestones).FirstOrDefault(x => x.Id == ids[index]);

                itemToUpdate.SortOrder = index;

                CRMDbContext.Update(itemToUpdate);                
            }

            CRMDbContext.SaveChanges();
            
            tx.Commit();
        }

        public int GetCount()
        {
            return Query(CRMDbContext.DealMilestones).Count();
        }


        public Dictionary<int, int> GetRelativeItemsCount()
        {
            var sqlQuery = Query(CRMDbContext.DealMilestones)
                           .GroupJoin(CRMDbContext.Deals,
                                 x => x.Id,
                                 x => x.DealMilestoneId, 
                                 (x,y) =>
                                 {
                                    
                                 });
                        


            throw new Exception();

            //var sqlQuery = Query("crm_deal_milestone tbl_deal_milestone")
            //              .Select("tbl_deal_milestone.id")
            //              .OrderBy("tbl_deal_milestone.sort_order", true)
            //              .GroupBy("tbl_deal_milestone.id");

            //sqlQuery.LeftOuterJoin("crm_deal tbl_crm_deal",
            //                          Exp.EqColumns("tbl_deal_milestone.id", "tbl_crm_deal.deal_milestone_id"))
            //    .Select("count(tbl_crm_deal.deal_milestone_id)");

            //var queryResult = Db.ExecuteList(sqlQuery);
            //return queryResult.ToDictionary(x => Convert.ToInt32(x[0]), y => Convert.ToInt32(y[1]));
        }

        public int GetRelativeItemsCount(int id)
        {
            return Query(CRMDbContext.Deals)
                  .Count(x => x.DealMilestoneId == id);
        }

        public virtual int Create(DealMilestone item)
        {

            if (String.IsNullOrEmpty(item.Title) || String.IsNullOrEmpty(item.Color))
                throw new ArgumentException();

            int id;

            using var tx = CRMDbContext.Database.BeginTransaction();

            if (item.SortOrder == 0)
                item.SortOrder = Query(CRMDbContext.DealMilestones).Select(x => x.SortOrder).Max() + 1;
                                    
            var itemToAdd = new DbDealMilestone
            {
                Title = item.Title,
                Description = item.Description,
                Color = item.Color,
                Probability = item.Probability,
                Status = item.Status,
                SortOrder = item.SortOrder,
                TenantId = TenantID
            };
            
            CRMDbContext.DealMilestones.Add(itemToAdd);
            CRMDbContext.SaveChanges();
            
            id = itemToAdd.Id;

            tx.Commit();
            
            return id;
        }

        public virtual void ChangeColor(int id, String newColor)
        {
            var item = CRMDbContext.DealMilestones.First(x => x.Id == id);

            item.Color = newColor;

            CRMDbContext.SaveChanges();
        }

        public virtual void Edit(DealMilestone item)
        {
            if (HaveContactLink(item.ID))
                throw new ArgumentException(String.Format("{0}. {1}.", CRMErrorsResource.BasicCannotBeEdited, CRMErrorsResource.DealMilestoneHasRelatedDeals));
            
            var itemToUpdate = Query(CRMDbContext.DealMilestones)
                .FirstOrDefault(x => x.Id == item.ID);

            itemToUpdate.Title = item.Title;
            itemToUpdate.Description = item.Description;
            itemToUpdate.Color = item.Color;
            itemToUpdate.Probability = item.Probability;
            itemToUpdate.Status = item.Status;

            CRMDbContext.DealMilestones.Update(itemToUpdate);
            
            CRMDbContext.SaveChanges();
        }

        public bool HaveContactLink(int dealMilestoneID)
        {
            return Query(CRMDbContext.Deals)
                .Any(x => x.DealMilestoneId == dealMilestoneID);
        }

        public virtual void Delete(int id)
        {
            if (HaveContactLink(id))
                throw new ArgumentException(String.Format("{0}. {1}.", CRMErrorsResource.BasicCannotBeDeleted, CRMErrorsResource.DealMilestoneHasRelatedDeals));

            var item = CRMDbContext.DealMilestones.First(x => x.Id == id);
            
            CRMDbContext.DealMilestones.Remove(item);

            CRMDbContext.SaveChanges();

        }

        public virtual DealMilestone GetByID(int id)
        {
            return ToDealMilestone(Query(CRMDbContext.DealMilestones).FirstOrDefault(x => x.Id == id));
        }

        public Boolean IsExist(int id)
        {
            return Query(CRMDbContext.DealMilestones).Any(x => x.Id == id);
        }

        public List<DealMilestone> GetAll(int[] id)
        {
            return Query(CRMDbContext.DealMilestones)
                .Where(x => id.Contains(x.Id)).ToList().ConvertAll(ToDealMilestone);
        }

        public List<DealMilestone> GetAll()
        {
            return Query(CRMDbContext.DealMilestones)
                    .ToList()
                    .ConvertAll(ToDealMilestone);
        }

        //private SqlQuery GetDealMilestoneQuery(Exp where)
        //{
        //    SqlQuery sqlQuery = Query("crm_deal_milestone")
        //        .Select("id",
        //                "title",
        //                "description",
        //                "color",
        //                "probability",
        //                "status",
        //                "sort_order")
        //                .OrderBy("sort_order", true);

        //    if (where != null)
        //        sqlQuery.Where(where);

        //    return sqlQuery;

        //}

        private static DealMilestone ToDealMilestone(DbDealMilestone dbDealMilestone)
        {
            return new DealMilestone
            {
                 ID = dbDealMilestone.Id,
                 Title = dbDealMilestone.Title,
                 Color = dbDealMilestone.Color,
                 Status = dbDealMilestone.Status,
                 Description = dbDealMilestone.Description,
                 Probability = dbDealMilestone.Probability,
                 SortOrder = dbDealMilestone.SortOrder                 
            };
        }
    }
}