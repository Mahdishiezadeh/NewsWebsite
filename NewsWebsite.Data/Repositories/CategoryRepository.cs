﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NewsWebsite.Data.Contracts;
using NewsWebsite.Entities;
using NewsWebsite.ViewModels.Category;
using NewsWebsite.Common;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace NewsWebsite.Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly NewsDBContext _context;
        private readonly IMapper _mapper;
        public CategoryRepository(NewsDBContext context, IMapper mapper)
        {
            _context = context;
            _context.CheckArgumentIsNull(nameof(_context));

            _mapper = mapper;
            _mapper.CheckArgumentIsNull(nameof(_mapper));
        }

        public async Task<List<CategoryViewModel>> GetPaginateCategoriesAsync(int offset, int limit, bool? categoryNameSortAsc,bool? parentCategoryNameSortAsc, string searchText)
        {
            List<CategoryViewModel> categories= await _context.Categories.Include(c => c.Parent)
                                    .Where(c => c.CategoryName.Contains(searchText) || c.Parent.CategoryName.Contains(searchText))
                                    .ProjectTo<CategoryViewModel>(_mapper.ConfigurationProvider)
                                    .Skip(offset).Take(limit).AsNoTracking().ToListAsync();

            if (categoryNameSortAsc != null)
                categories = categories.OrderBy(c => (categoryNameSortAsc == true && categoryNameSortAsc != null) ? c.CategoryName : "")
                                     .OrderByDescending(c => (categoryNameSortAsc == false && categoryNameSortAsc != null) ? c.CategoryName : "").ToList();

            else if (parentCategoryNameSortAsc != null)
            {
                categories = categories.OrderBy(c => (parentCategoryNameSortAsc == true && parentCategoryNameSortAsc != null) ? c.ParentCategoryName : "")
                                   .OrderByDescending(c => (parentCategoryNameSortAsc == false && parentCategoryNameSortAsc != null) ? c.ParentCategoryName : "").ToList();

            }

            foreach (var item in categories)
                item.Row = ++offset;

            return categories;
        }

        /// <summary>
        /// برای اخذ یو آر ال در اکشن متد گت آل کتگوری
        /// به ویو مدل تری ویو کتگوری رفته و پراپرتی یو آر ال را ایجاد کردیم
        /// سپس در اینجا مقداردهی کردیم و اکشن متد گت آل کتگوری را به نوع تسک تغییر دادیم 
        /// و در خطوط برنامه از اویت برای برنامه نویسی موازی کمک گرفتیم
        /// </summary>
        /// <returns></returns>
        public async Task<List<TreeViewCategory>> GetAllCategoriesAsync()
        {
            var Categories =await (from c in _context.Categories
                              where (c.ParentCategoryId == null)
                              select new TreeViewCategory { id = c.CategoryId, title = c.CategoryName ,url=c.Url }).ToListAsync();
            foreach (var item in Categories)
            {
                BindSubCategories(item);
            }

            return Categories;
        }

        public void BindSubCategories(TreeViewCategory category)
        {
            var SubCategories = (from c in _context.Categories
                                 where (c.ParentCategoryId == category.id)
                                 select new TreeViewCategory { id = c.CategoryId, title = c.CategoryName, url=c.Url }).ToList();
            foreach (var item in SubCategories)
            {
                BindSubCategories(item);
                category.subs.Add(item);
            }
        }

        public Category FindByCategoryName(string categoryName)
        {
           return  _context.Categories.Where(c => c.CategoryName == categoryName.TrimStart().TrimEnd()).FirstOrDefault();
        }


        public bool IsExistCategory(string categoryName, string recentCategoryId = null)
        {
            if (!recentCategoryId.HasValue())
                return _context.Categories.Any(c => c.CategoryName.Trim().Replace(" ", "") == categoryName.Trim().Replace(" ", ""));
            else
            {
                var category = _context.Categories.Where(c => c.CategoryName.Trim().Replace(" ", "") == categoryName.Trim().Replace(" ", "")).FirstOrDefault();
                if (category == null)
                    return false;
                else
                {
                    if (category.CategoryId != recentCategoryId)
                        return true;
                    else
                        return false;
                }
            }
        }


    }
}
