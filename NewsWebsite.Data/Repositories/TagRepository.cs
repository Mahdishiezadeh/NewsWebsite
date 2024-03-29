﻿using Microsoft.EntityFrameworkCore;
using NewsWebsite.Common;
using NewsWebsite.Data.Contracts;
using NewsWebsite.Entities;
using NewsWebsite.ViewModels.Tag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsWebsite.Data.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly NewsDBContext _context;
        public TagRepository(NewsDBContext context)
        {
            _context = context;
        }


        public async Task<List<TagViewModel>> GetPaginateTagsAsync(int offset, int limit, bool? tagNameSortAsc, string searchText)
        {
            List<TagViewModel> tags = await _context.Tags.Where(c => c.TagName.Contains(searchText))
                                   .Select(t => new TagViewModel { TagId = t.TagId, TagName = t.TagName }).Skip(offset).Take(limit).AsNoTracking().ToListAsync();

            if (tagNameSortAsc != null)
                tags = tags.OrderBy(c => (tagNameSortAsc == true && tagNameSortAsc != null) ? c.TagName : "").OrderByDescending(c => (tagNameSortAsc == false && tagNameSortAsc != null) ? c.TagName : "").ToList();

            foreach (var item in tags)
                item.Row = ++offset;

            return tags;
        }

        public bool IsExistTag(string tagName, string recentTagId = null)
        {
            if (!recentTagId.HasValue())
                return _context.Tags.Any(c => c.TagName.Trim().Replace(" ", "") == tagName.Trim().Replace(" ", ""));
            else
            {
                var tag = _context.Tags.Where(c => c.TagName.Trim().Replace(" ", "") == tagName.Trim().Replace(" ", "")).FirstOrDefault();
                if (tag == null)
                    return false;
                else
                {
                    if (tag.TagId != recentTagId)
                        return true;
                    else
                        return false;
                }
            }
        }

        /// <summary>
        /// در این متد در پارامتر اول ما اسم تگ ها را در قالب یک آرایه دریافت  و
        /// در پارامتر دوم آیدی خبر را نیز توسط این اکشن متد دریافت میکنیم
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="newsId"></param>
        /// <returns></returns>
        public async Task<List<NewsTag>> InsertNewsTags(string[] tags, string newsId = null)
        {
            string tagId;
            List<NewsTag> newsTags = new List<NewsTag>();
            ///در اینجا تمام تگ های دیتابیس را اخذ کرده ایم
            var allTags = _context.Tags.ToList();
           
            newsTags.AddRange(allTags.Where(n => tags.Contains(n.TagName)).Select(c => new NewsTag { TagId = c.TagId, NewsId = newsId }).ToList());
            ///در این خط علاوه بر تگ های دیتابیس تگ های نوشته شده توسط کاربر در همان لحظه را نیز اخذ کرده ایم
            var newTags = tags.Where(n => !allTags.Select(t => t.TagName).Contains(n)).ToList();
            ///در اینجا تگ هایی که داخل نیوزتگ نیست را بررسی و به این جدول اضافه میکنیم
            foreach (var item in newTags)
            {
                tagId = StringExtensions.GenerateId(10);
                _context.Tags.Add(new Tag { TagName = item, TagId = tagId });
                newsTags.Add(new NewsTag { TagId = tagId, NewsId = newsId });
            }
            await _context.SaveChangesAsync();
            return newsTags;
        }
    }
}
