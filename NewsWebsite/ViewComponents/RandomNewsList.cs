using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsWebsite.Common;
using NewsWebsite.Data.Contracts;
using NewsWebsite.ViewModels.News;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewsWebsite.ViewComponents
{
    public class RandomNewsList : ViewComponent
    {
        private readonly IUnitOfWork _uw;
        public RandomNewsList(IUnitOfWork uw)
        {
            _uw = uw;
        }
        /// <summary>
        /// نامبر را دراینجا چون دو خبر میخواهیم عدد دو ارسال میکنیم 
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public async Task<IViewComponentResult> InvokeAsync(int number)
        {
            ///دو (2) خبری که به صورت رندوم دریافت میکنیم را درون این لیست قرار میدهیم
            var newsList = new List<NewsViewModel>();
            ///شماره ردیف خبری که قرار است سلکت کنیم را داخل این متغییر قرار میدهیم
            int randomRow;
            for (int i=0;i<number;i++)
            {
                ///متد دات کانت نیوز پابلیشد را در نیوز ریپازیتوری ایجاد کردیم
                ///با استفاده از رندوم نامبر شماره ردیف خبر را به دست میاوریم ، عدد مینیمم یک میباشد و عدد ماکزیمم
                ///تعداد خبرهای که منتشر شده اند + یک را ارسال کرده ایم
                randomRow = CustomMethods.RandomNumber(1, _uw.NewsRepository.CountNewsPublished()+1);
                ///در این بخش برای خبر شرط گذاشتیم خبرهای منتشر شده و مشخص کرده ایم چه پراپرتی های از خبر سلکت شود 
                ///در متد اسکیپ گفته ایم از روی تعداد رندوم منهای یک عدد بپر و در متد تیک گفتیم خبر بعد از آن را دریافت کن 
                ///به همین منظور به متد تیک عدد یک را ارسال نمودیم 
                var news = await _uw._Context.News.Where(n => n.IsPublish == true && n.PublishDateTime <= DateTime.Now).Select(n => new NewsViewModel { Title = n.Title, Url = n.Url, NewsId = n.NewsId, ImageName = n.ImageName }).Skip(randomRow-1).Take(1).FirstOrDefaultAsync();
                ///در اینجا خبر را دریافت و به لیست ارسال نمودیم
                newsList.Add(news);
            }
           
            return View(newsList);
        }
    }
}
