using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsWebsite.Data.Contracts;
using NewsWebsite.ViewModels.News;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewsWebsite.ViewComponents
{
    /// <summary>
    /// بایست از کلاس ویوکامپوننت ارث بری کند 
    /// </summary>
    public class LatestNewsTitleList : ViewComponent
    {
        private readonly IUnitOfWork _uw;
        public LatestNewsTitleList(IUnitOfWork uw)
        {
            _uw = uw;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        
        {
            ///شرط قرار میدیم که حتما خبر منتشر شده باشه
            ///شرط رو از نیوز ریپازیتوری و متد گت پیجینیت ایسینک کپی میشه کرد 
            ///و یکسری اصلاحات روی کد کپی شده اعمال کرد
            ///برای مرتب کردن اخبار در نمایش جدیدترین ها از متد اردر بای کمک گرفتیم
            ///و از متد سلکت برای گرفتن عنوان خبر و یو آر ال خبر کمک میگیریم
            ///از نیوزویومدل برای انتخاب پراپرتی های مدنظر کمک میگیریم
            ///متد تیک هم برای گرفتن 10 خبر اول است
            var newsTitles = await _uw._Context.News.Where(n => n.IsPublish == true && n.PublishDateTime <= DateTime.Now)
                .OrderByDescending(n => n.PublishDateTime).Select(n => new NewsViewModel {Title=n.Title,Url=n.Url,NewsId=n.NewsId}).Take(10).ToListAsync();
            return View(newsTitles);
        }
    }
}
