using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewsWebsite.Data.Contracts;
using NewsWebsite.Entities;
using NewsWebsite.ViewModels.Home;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewsWebsite.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// میبایست با این سازنده یک نمونه از رابط یونیت آو ورک را دریفات کنیم 
        /// تا دسترسی به کلاس های ریپازیتوری داشته باشیم 
        /// </summary>
        private readonly IUnitOfWork _uw;
        private readonly IHttpContextAccessor _accessor;
        public HomeController(IUnitOfWork uw, IHttpContextAccessor accessor)
        {
            _uw = uw;
            _accessor = accessor;
        }
        public async Task< IActionResult> Index(string duration ,string TypeOfNews)
        {

            ///در این خط کد ایجکس ما بررسی میکنیم که پیام دریافتی از توع ایجکس میباشد یا خیر ؟
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            ///اگر ماستویود نیوز بود 
            if (isAjax && TypeOfNews == "MostViewedNews")
                ///پارشیال ویو مربوطه به اضافه اطلاعات جدید پر بازدید ترین اخبار را به پارشیال ویو باز میگردانیم
                return PartialView("_MostViewedNews", await _uw.NewsRepository.MostViewedNews(0, 3, duration));

            else if (isAjax && TypeOfNews == "MostTalkNews")
                return PartialView("_MostTalkNews", await _uw.NewsRepository.MostTalkNews(0, 5, duration));


            ///اگر درخواست از توع ایجکس نبود
            else
            {
                ///قصد داریم از متد GetPagginatenewsAsync 
                ///استفاده کنیم پس یک متغییر تعریف میکنیم  
                ///چون ده عدد خبر را میخواهیم بگیریم 
                ///آفست 0 لیمیت 10 و سورت بر اساس تاریخ انتشار و نزولی میخواهیم انجام دهیم پس
                ///مقدار سرچ رو کاراکتر خالی و فقط خبرهای منتشر شده را مدنظر داریم پس ترو را ارسال میکنیم
                ///پارامتر آخر را نال وارد کردیم چون هم خبرهای داخلی و هم خارجی را نیاز داریم
                var news = _uw.NewsRepository.GetPaginateNews(0, 10, item => "", item => item.First().PersianPublishDate, "", true,null);
                var mostViewedNews = await _uw.NewsRepository.MostViewedNews(0, 3, "day");
                ///چون پنج عدد خبر را میخواهیم صفر و پنج قرار دادیم
                var mostTalkNews = await _uw.NewsRepository.MostTalkNews(0, 5, "day");
                var mostPopularNews = await _uw.NewsRepository.MostPopularNews(0, 5);
                ///ترتیب وارد کردن پارامترها به این صورت است که 10 عدد از خبرهای داخلی با سورت نزولی
                ///و براساس تارخ انتشار و و خبرهای منتشر شده و خبرهای داخلی
                var internalNews = _uw.NewsRepository.GetPaginateNews(0, 10, item => "", item => item.First().PersianPublishDate, "", true, true);
                ///در اینجا 10 خبر با سورت نزولی بر اساس تاریخ انتشار و منتشر شده و خبرهای خارجی مد نظر میباشد
                var foreignNews = _uw.NewsRepository.GetPaginateNews(0, 10, item => "", item => item.First().PersianPublishDate, "", true, false);
                ///در اینجا 10 ویدئو اول را میخواهیم ،نال یعنی قصد نداریم بر اساس تایتل سورت کنیم ،
                ///فالس یعنی سورت نزولی، دابل کوتیشن هم برای پارامتر سرچ است  
                var videos =await _uw.VideoRepository.GetPaginateVideosAsync(0,10,null,false,"");
                ///قصد داریم یک نمونه از هوم پیج ویومدل را مقدار دهی کنیم 
                var homePageViewModel = new HomePageViewModel(news, mostViewedNews, mostTalkNews, mostPopularNews, internalNews, foreignNews, videos);
                return View(homePageViewModel);
            }
        }

        [Route("News/{newsId}/{url}")]
        public async Task<IActionResult> NewsDetails(string newsId, string url)
        {
            ///در اینجا آدرس آیپی کاربر را گرفته و چک میکنیم آیا این خبر بازدید شده است یا خیر ؟
            string ipAddress = _accessor.HttpContext?.Connection?.RemoteIpAddress.ToString();
            Visit visit = _uw.BaseRepository<Visit>().FindByConditionAsync(n => n.NewsId == newsId && n.IpAddress == ipAddress).Result.FirstOrDefault();
            if (visit != null && visit.LastVisitDateTime.Date != DateTime.Now.Date)
            {
                visit.NumberOfVisit = visit.NumberOfVisit + 1;
                visit.LastVisitDateTime = DateTime.Now;
                await _uw.Commit();
            }
            else if (visit == null)
            {
                visit = new Visit { IpAddress = ipAddress, LastVisitDateTime = DateTime.Now, NewsId = newsId, NumberOfVisit = 1 };
                await _uw.BaseRepository<Visit>().CreateAsync(visit);
                await _uw.Commit();
            }

            var news = await _uw.NewsRepository.GetNewsById(newsId);
            var newsComments = await _uw.NewsRepository.GetNewsCommentsAsync(newsId);
            var nextAndPreviousNews = await _uw.NewsRepository.GetNextAndPreviousNews(news.PublishDateTime);
            var newsRelated = await _uw.NewsRepository.GetRelatedNews(2, news.TagIdsList, newsId);
            var newsDetailsViewModel = new NewsDetailsViewModel(news, newsComments, newsRelated, nextAndPreviousNews);
            return View(newsDetailsViewModel);
        }

    }
}
