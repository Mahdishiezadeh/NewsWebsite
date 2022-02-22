using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NewsWebsite.Common;
using NewsWebsite.Data.Contracts;
using NewsWebsite.Entities;
using NewsWebsite.ViewModels.News;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsWebsite.Data.Repositories
{
    public class NewsRepository : INewsRepository
    {
        private readonly NewsDBContext _context;
        private readonly IMapper _mapper;
        public NewsRepository(NewsDBContext context, IMapper mapper)
        {
            _context = context;
            _context.CheckArgumentIsNull(nameof(_context));

            _mapper = mapper;
            _mapper.CheckArgumentIsNull(nameof(_mapper));
        }


        ///این متد تعداد خبرهای منتشر شده را به ما میدهد ///
        public int CountNewsPublished() => _context.News.Where(n => n.IsPublish == true && n.PublishDateTime <= DateTime.Now).Count();

        /// </summary>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// استرینگ کلید گروه است و ویو مدل لیست
        /// <param name="orderByAscFunc"></param>
        /// استرینگ کلید گروه است و ویو مدل لیست
        /// <param name="orderByDescFunc"></param>
        /// <param name="searchText"></param>
        /// <param name="isPublish"></param>
        /// <returns></returns>
        public List<NewsViewModel> GetPaginateNews(int offset, int limit, Func<IGrouping<string, NewsViewModel>,
           Object> orderByAscFunc, Func<IGrouping<string, NewsViewModel>, Object> orderByDescFunc, string searchText, bool? isPublish,bool? isInternal)
        {
            string NameOfCategories = "";
            string NameOfTags = "";
            ///یک لیست از نوع ویومدل نیوز تعرق کردیم
            List<NewsViewModel> newsViewModel = new List<NewsViewModel>();
            ///در این کوئری کلاس های 
            ///News و NewsCategories و Categories و NewsTags و Tags
            ///با یکدیگر جوین کردیم 
            ///و در خط پایین گفتیم علاوه بر جوین ها اطلاعات بازدید و لایک و کاربر را نیز نمایش بده
            var newsGroup =  (from n in _context.News.Include(v => v.Visits).Include(l => l.Likes).Include(u=>u.User).Include(c=>c.Comments)
                                   join e in _context.NewsCategories on n.NewsId equals e.NewsId into bc
                                   from bct in bc.DefaultIfEmpty()
                                   join c in _context.Categories on bct.CategoryId equals c.CategoryId into cg
                                   from cog in cg.DefaultIfEmpty()
                                   join a in _context.NewsTags on n.NewsId equals a.NewsId into ac
                                   from act in ac.DefaultIfEmpty()
                                   join t in _context.Tags on act.TagId equals t.TagId into tg
                                   from tog in tg.DefaultIfEmpty()
                                       ///اگر ایز پابلیش نال بود تمام خبرها را اعم از ایز پابلیش ترو یا فالس نمایش بده 
                                       ///درغیر اینصورت اگر ایزپابلیش مقدار ترو داشت خبرهای منتشر شده و زمان انتشار آنها از زمان فعلی کمتر است را نمایش بده
                                       /// اگر مقدار ایزپابلیش فالس بود خبرهایی را نمایش بده که در حالت انتشار هستند 
                              where (n.Title.Contains(searchText) && isPublish == null ? (n.IsPublish == true || n.IsPublish == false) : (isPublish == true ? n.IsPublish == true && n.PublishDateTime <= DateTime.Now : n.IsPublish == false)
                              ///اگر مقدار ایز اینترنال نال باشد یعنی هم خبر داخلی و هم خارجی را میخواهیم
                              ///اکر ایز اینترنال ترو بود یعنی خبرهایی را میخواهیم که مقدار ایز اینترنال آنها ترو 
                              ///و در غیر اینصورت خبرهایی که مقدار ایزاینترنال فالس باشد را میخواهیم
                              && isInternal == null ? n.IsInternal == true || n.IsInternal == false : (isInternal == true ? n.IsInternal == true : n.IsInternal == false))
                              select (new NewsViewModel
                                   { ///در این بخش اطلاعات مورد نیاز را انتخاب کرده ایم
                                       NewsId = n.NewsId,
                                       Title = n.Title,
                                       Abstract = n.Abstract,
                                       ShortTitle = n.Title.Length > 50 ? n.Title.Substring(0, 50) + "..." : n.Title,
                                       Url = n.Url,
                                       ImageName = n.ImageName,
                                       Description = n.Description,
                                       NumberOfVisit = n.Visits.Select(v => v.NumberOfVisit).Sum(),
                                       NumberOfLike = n.Likes.Where(l => l.IsLiked == true).Count(),
                                       NumberOfDisLike = n.Likes.Where(l => l.IsLiked == false).Count(),
                                       NumberOfComments = n.Comments.Count(),
                                       NameOfCategories = cog != null ? cog.CategoryName : "",
                                       NameOfTags = tog != null ? tog.TagName : "",
                                       AuthorName = n.User.FirstName + " " + n.User.LastName,
                                       IsPublish = n.IsPublish,
                                       NewsType = n.IsInternal == true ? "داخلی" : "خارجی",
                                       PublishDateTime = n.PublishDateTime == null ? new DateTime(01, 01, 01) : n.PublishDateTime,
                                       PersianPublishDate = n.PublishDateTime == null ? "-" : n.PublishDateTime.ConvertMiladiToShamsi("yyyy/MM/dd ساعت HH:mm:ss"),
                                       ///در این خط نیز گروه بندی انجام میدهیم
                                       ///خبر به ازا هر دسته بندی و هر تگ یک رکورد برایش ثبت میشود پس گروه بندی جلوگیری میکند از ثبت رکورد های اضافه
                                       ///در اینجا به ازای آفست و لیمیت گروه بندی انجام میدهیم
                                       ///بعد از گروپ بای ، اردر بای اضافه شده است برای سورت کردن خبر در تمام پنل مدیریت 
                                       ///نه فقط سورت در صفحه اول که بعد از اردر بای از اردر بای دیسندینگ استفاده شده 
                                       ///و هیچ چیزی به آیتم پاس داده نشده تا مشخص کنیم قصد انجام سورت نزولی را نداریم
                                   })).GroupBy(b => b.NewsId).OrderBy(item=>item.First().Title).OrderByDescending(item=>"").Select(g => new { NewsId = g.Key, NewsGroup = g }).Skip(offset).Take(limit).ToList();
            ///حال میبایست گروه ها را پیمایش کنیم 
            foreach (var item in newsGroup)
            {
                NameOfCategories = "";
                NameOfTags = "";
                ///در این حلقه کتگوری نیم ها را استخراج و با متد دیستینک موارد تکراری را حذف میکنیم
                foreach (var a in item.NewsGroup.Select(a => a.NameOfCategories).Distinct())
                {
                    ///در این ساختار اگر نام کتگوری کاراکتر خالی باشد آن را داخل متغییر آ میریزیم
                    if (NameOfCategories == "")
                        NameOfCategories = a;
                    else
                        ///واگر نام داشته باشد اول نام و سپس یک خط فاصله و در نهایت نام کتگوری دیگر را اضافه میکنیم
                        ///به زبان ساده اگر کتگوری پدر داشت آن را با خط فاصله به فرزند اضافه میکنیم
                        NameOfCategories = NameOfCategories + " - " + a;
                }
                ///دقیقا عملیات توضیح داده شده در حلقه بالا برای تگ ها نیز تکرار شده است
                foreach (var a in item.NewsGroup.Select(a => a.NameOfTags).Distinct())
                {
                    if (NameOfTags == "")
                        NameOfTags = a;
                    else
                        NameOfTags = NameOfTags + " - " + a;
                }
                ///حال یک ویو مدل از نیوز ایجاد و آن را مقدار دهی میکنیم 
                NewsViewModel news = new NewsViewModel()
                {
                    NewsId = item.NewsId,
                    Title = item.NewsGroup.First().Title,
                    ShortTitle = item.NewsGroup.First().ShortTitle,
                    Url = item.NewsGroup.First().Url,
                    Description = item.NewsGroup.First().Description,
                    NumberOfVisit = item.NewsGroup.First().NumberOfVisit,
                    NumberOfDisLike = item.NewsGroup.First().NumberOfDisLike,
                    NumberOfLike = item.NewsGroup.First().NumberOfLike,
                    NumberOfComments=item.NewsGroup.First().NumberOfComments,
                    PublishDateTime=item.NewsGroup.First().PublishDateTime,
                    Abstract=item.NewsGroup.First().Abstract,
                    PersianPublishDate = item.NewsGroup.First().PersianPublishDate,
                    NewsType = item.NewsGroup.First().NewsType,
                    ///وضعیت انتشار را با این شرط که اگر زمان خبر بزرگتر از رمان حال باشد انتشار در آینده است در غیر این صورت منتشر شده 
                    Status = item.NewsGroup.First().IsPublish==false?"پیش نویس": (item.NewsGroup.First().PublishDateTime > DateTime.Now ? "انتشار در آینده" : "منتشر شده"),
                    NameOfCategories = NameOfCategories,
                    NameOfTags = NameOfTags,
                    ImageName=item.NewsGroup.First().ImageName,
                    AuthorName = item.NewsGroup.First().AuthorName,
                };
                ///در این کد اطلاعات خبر را به ویومدل اضافه کردیم 
                newsViewModel.Add(news);
            }
           
            foreach (var item in newsViewModel)
                item.Row = ++offset;

            return newsViewModel;

        }

        public string CheckNewsFileName(string fileName)
        {
            string fileExtension = Path.GetExtension(fileName);
            int fileNameCount = _context.News.Where(f => f.ImageName == fileName).Count();
            int j = 1;
            while (fileNameCount != 0)
            {
                fileName = fileName.Replace(fileExtension, "") + j + fileExtension;
                fileNameCount = _context.Videos.Where(f => f.Poster == fileName).Count();
                j++;
            }

            return fileName;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>آفست و لیمیت برای مشخص کردن آنست که ما چه تعداد خبر را میخواهیم 
        /// <param name="limit"></param> "  " "   "  " " " " " " " " "
        /// <param name="duration"></param>  میتواند یکی از سه مورد Week Or Day Or Month  باشد             
        /// <returns></returns>
        public async Task<List<NewsViewModel>> MostViewedNews(int offset, int limit, string duration)
        {
            string NameOfCategories = "";
            List<NewsViewModel> newsViewModel = new List<NewsViewModel>();
            ///باید تاریخ اولین روز مد نظر کاربر را به دست بیاوریم
            ///تا با کسر کردن از تاریخ آخرین روز حدفاصل آن را حساب کنیم 
            DateTime StartMiladiDate;
            DateTime EndMiladiDate = DateTime.Now;

            if (duration == "week")///اگر تاریخ انتخابی هفته بود
            {
                ///تاریخ امروز را اخذ نمودیم و 4 تا دی گذاشتیم که به صورت روز نمایش دهد 
                ///سپس از متد نام آو ویک کمک میگیریم که عدد آن را بازگشت دهد 
                int NumOfWeek = ConvertDateTime.ConvertMiladiToShamsi(DateTime.Now, "dddd").GetNumOfWeek();
                /// در این خط ما عدد نام آو ویک  را در منهای یک ضرب میکنیم تا از عدد تاریخ امروز کسر شود
                /// تاریخ شنبه را نیز به صورت (0،0،0) بابت ساعت و دقیقه و روز در نظر گرفتیم و به تایم شنبه اضافه میکنیم
                StartMiladiDate = DateTime.Now.AddDays((-1) * NumOfWeek).Date + new TimeSpan(0, 0, 0);
            }

            else if (duration == "day")
                ///اگر مبنا روز بود تاریخ روز را اخذ میکنیم و به اضافه تاریخ صفر (شنبه) میکنیم 
                StartMiladiDate = DateTime.Now.Date + new TimeSpan(0, 0, 0);

            else
            {
                ///اگر مبنا ماه بود بایست تاریخ اولین روز ماه را به دست بیاوریم 
                /// دی دی برای اخذ روز ماه است و از متد فا تو ان برای تبدیل عدد به انگلیسی استفاده کنیم 
                string DayOfMonth = ConvertDateTime.ConvertMiladiToShamsi(DateTime.Now, "dd").Fa2En();
                ///حال از عدد ماه یک واحد کم میکنیم و در منهای یک ضرب میکنیم 
                StartMiladiDate = DateTime.Now.AddDays((-1) * (int.Parse(DayOfMonth) - 1)).Date + new TimeSpan(0, 0, 0);
            }
            ///اطلاعات مورد نیاز خبر را از دیتابیس اخذ میکنیم 
            var newsGroup = await (from n in _context.News.Include(v => v.Visits).Include(l => l.Likes).Include(c => c.Comments)
                                   join e in _context.NewsCategories on n.NewsId equals e.NewsId into bc
                                   from bct in bc.DefaultIfEmpty()
                                   join c in _context.Categories on bct.CategoryId equals c.CategoryId into cg
                                   from cog in cg.DefaultIfEmpty()
                                   ///تاریخ بین شروع و پایانی که کاربر وارد کرده در اتخاذ خبر به عنوان شرط قرار میدهیم
                                   where (n.PublishDateTime <= EndMiladiDate && StartMiladiDate <= n.PublishDateTime)
                                   ///حالا اخبار رو سلکت میکنیم 
                                   select (new
                                   {   
                                       n.NewsId,
                                       ///عنوان خبر را نیز محدود میکنیم
                                       ShortTitle = n.Title.Length > 60 ? n.Title.Substring(0, 60) + "..." : n.Title,
                                       n.Url,
                                       NumberOfVisit = n.Visits.Select(v => v.NumberOfVisit).Sum(),
                                       NumberOfLike = n.Likes.Where(l => l.IsLiked == true).Count(),
                                       NumberOfDisLike = n.Likes.Where(l => l.IsLiked == false).Count(),
                                       NumberOfComments = n.Comments.Count(),
                                       n.ImageName,
                                       CategoryName = cog != null ? cog.CategoryName : "",
                                       PublishDateTime = n.PublishDateTime == null ? new DateTime(01, 01, 01) : n.PublishDateTime,
                                       ///گروه بندی و سورت کردن در این خط کد انجام شده است 
                                   })).GroupBy(b => b.NewsId).Select(g => new { NewsId = g.Key, NewsGroup = g })
                                   ///خبرهایی که از همه پربازدیدترن سورت میکنیم
                                   .OrderByDescending(g => g.NewsGroup.First().NumberOfVisit).Skip(offset).Take(limit).AsNoTracking().ToListAsync();
            ///نیم آو کتگوریز را در این حلقه مقدار دهی میکنیم 
            foreach (var item in newsGroup)
            {
                NameOfCategories = "";
                foreach (var a in item.NewsGroup.Select(a => a.CategoryName).Distinct())
                {
                    if (NameOfCategories == "")
                        NameOfCategories = a;
                    else
                        NameOfCategories = NameOfCategories + " - " + a;
                }
                ///اطلاعات خبر رو برای ویو مدل ارسال میکنیم
                NewsViewModel news = new NewsViewModel()
                {
                    NewsId = item.NewsId,
                    ShortTitle = item.NewsGroup.First().ShortTitle,
                    Url = item.NewsGroup.First().Url,
                    NumberOfVisit = item.NewsGroup.First().NumberOfVisit,
                    NumberOfDisLike = item.NewsGroup.First().NumberOfDisLike,
                    NumberOfLike = item.NewsGroup.First().NumberOfLike,
                    NameOfCategories = NameOfCategories,
                    PublishDateTime = item.NewsGroup.First().PublishDateTime,
                    ImageName = item.NewsGroup.First().ImageName,
                };
                newsViewModel.Add(news);
            }

            return newsViewModel;
        }

        public async Task<List<NewsViewModel>> MostTalkNews(int offset, int limit, string duration)
        {
            DateTime StartMiladiDate;
            DateTime EndMiladiDate = DateTime.Now;

            if (duration == "week")
            {
                int NumOfWeek = ConvertDateTime.ConvertMiladiToShamsi(DateTime.Now, "dddd").GetNumOfWeek();
                StartMiladiDate = DateTime.Now.AddDays((-1) * NumOfWeek).Date + new TimeSpan(0, 0, 0);
            }

            else if (duration == "day")
                StartMiladiDate = DateTime.Now.Date + new TimeSpan(0, 0, 0);

            else
            {
                string DayOfMonth = ConvertDateTime.ConvertMiladiToShamsi(DateTime.Now, "dd").Fa2En();
                StartMiladiDate = DateTime.Now.AddDays((-1) * (int.Parse(DayOfMonth) - 1)).Date + new TimeSpan(0, 0, 0);
            }

            return await (from n in _context.News.Include(v => v.Visits).Include(l => l.Likes).Include(c => c.Comments)
                          where (n.PublishDateTime <= EndMiladiDate && StartMiladiDate <= n.PublishDateTime)
                          select (new NewsViewModel
                          {
                              NewsId = n.NewsId,
                              ShortTitle = n.Title.Length > 50 ? n.Title.Substring(0, 50) + "..." : n.Title,
                              Url = n.Url,
                              NumberOfVisit = n.Visits.Select(v => v.NumberOfVisit).Sum(),
                              NumberOfLike = n.Likes.Where(l => l.IsLiked == true).Count(),
                              NumberOfDisLike = n.Likes.Where(l => l.IsLiked == false).Count(),
                              NumberOfComments = n.Comments.Count(),
                              ImageName = n.ImageName,
                              PublishDateTime = n.PublishDateTime == null ? new DateTime(01, 01, 01) : n.PublishDateTime,
                          })).OrderByDescending(o => o.NumberOfComments).Skip(offset).Take(limit).AsNoTracking().ToListAsync();
        }

        public async Task<List<NewsViewModel>> MostPopularNews(int offset, int limit)
        {
            return await (from n in _context.News.Include(v => v.Visits).Include(l => l.Likes).Include(c => c.Comments)
                          where (n.IsPublish == true && n.PublishDateTime <= DateTime.Now)
                          select (new NewsViewModel
                          {
                              NewsId = n.NewsId,
                              ShortTitle = n.Title.Length > 50 ? n.Title.Substring(0, 50) + "..." : n.Title,
                              Url = n.Url,
                              Title = n.Title,
                              NumberOfVisit = n.Visits.Select(v => v.NumberOfVisit).Sum(),
                              NumberOfLike = n.Likes.Where(l => l.IsLiked == true).Count(),
                              NumberOfDisLike = n.Likes.Where(l => l.IsLiked == false).Count(),
                              NumberOfComments = n.Comments.Count(),
                              ImageName = n.ImageName,
                              PublishDateTime = n.PublishDateTime == null ? new DateTime(01, 01, 01) : n.PublishDateTime,
                          })).OrderByDescending(o => o.NumberOfLike).Skip(offset).Take(limit).AsNoTracking().ToListAsync();

        }

        public async Task<NewsViewModel> GetNewsById(string newsId)
        {
            ///این متغییر تعریف شده است که دسته بندی های خبر را درون آن بریزیم
            string NameOfCategories = "";
            var newsGroup = await (from n in _context.News.Include(v => v.Visits).Include(l => l.Likes).Include(u => u.User).Include(c => c.Comments)
                                   join e in _context.NewsCategories on n.NewsId equals e.NewsId into bc
                                   from bct in bc.DefaultIfEmpty()
                                   join c in _context.Categories on bct.CategoryId equals c.CategoryId into cg
                                   from cog in cg.DefaultIfEmpty()
                                   join a in _context.NewsTags on n.NewsId equals a.NewsId into ac
                                   from act in ac.DefaultIfEmpty()
                                   join t in _context.Tags on act.TagId equals t.TagId into tg
                                   from tog in tg.DefaultIfEmpty()
                                   where (n.NewsId == newsId)
                                   select (new NewsViewModel
                                   {
                                       NewsId = n.NewsId,
                                       Title = n.Title,
                                       Abstract = n.Abstract,
                                       ShortTitle = n.Title.Length > 50 ? n.Title.Substring(0, 50) + "..." : n.Title,
                                       Url = n.Url,
                                       ImageName = n.ImageName,
                                       Description = n.Description,
                                       NumberOfVisit = n.Visits.Select(v => v.NumberOfVisit).Sum(),
                                       NumberOfLike = n.Likes.Where(l => l.IsLiked == true).Count(),
                                       NumberOfDisLike = n.Likes.Where(l => l.IsLiked == false).Count(),
                                       ///فقط تعداد نظرات تایید شده 
                                       NumberOfComments = n.Comments.Where(c => c.IsConfirm == true).Count(),
                                       NameOfCategories = cog != null ? cog.CategoryName : "",
                                       NameOfTags = tog != null ? tog.TagName : "",
                                       IdOfTags = tog != null ? tog.TagId : "",
                                       AuthorInfo = n.User,
                                       IsPublish = n.IsPublish,
                                       NewsType = n.IsInternal == true ? "داخلی" : "خارجی",
                                       PublishDateTime = n.PublishDateTime == null ? new DateTime(01, 01, 01) : n.PublishDateTime,
                                       PersianPublishDate = n.PublishDateTime == null ? "-" : n.PublishDateTime.ConvertMiladiToShamsi("yyyy/MM/dd ساعت HH:mm:ss"),
                                       ///گروپ بای برای جلوگیری از تکرار خبر میباشد
                                   })).GroupBy(b => b.NewsId).Select(g => new { NewsId = g.Key, NewsGroup = g }).AsNoTracking().ToListAsync();

            ///دسته های خبر رو داخل این متغییر قرار داده ایم
            foreach (var a in newsGroup.First().NewsGroup.Select(a => a.NameOfCategories).Distinct())
            {
                if (NameOfCategories == "")
                    NameOfCategories = a;
                else
                    NameOfCategories = NameOfCategories + " - " + a;
            }

            var news = new NewsViewModel()
            {
                ///در اینجا فقط یک گروه بندی داریم پس از متد فرست استفاده کردیم
                NewsId = newsGroup.First().NewsGroup.First().NewsId,
                Title = newsGroup.First().NewsGroup.First().Title,
                ShortTitle = newsGroup.First().NewsGroup.First().ShortTitle,
                Abstract = newsGroup.First().NewsGroup.First().Abstract,
                Url = newsGroup.First().NewsGroup.First().Url,
                Description = newsGroup.First().NewsGroup.First().Description,
                NumberOfVisit = newsGroup.First().NewsGroup.First().NumberOfVisit,
                NumberOfDisLike = newsGroup.First().NewsGroup.First().NumberOfDisLike,
                NumberOfLike = newsGroup.First().NewsGroup.First().NumberOfLike,
                PersianPublishDate = newsGroup.First().NewsGroup.First().PersianPublishDate,
                NewsType = newsGroup.First().NewsGroup.First().NewsType,
                Status = newsGroup.First().NewsGroup.First().IsPublish == false ? "پیش نویس" : (newsGroup.First().NewsGroup.First().PublishDateTime > DateTime.Now ? "انتشار در آینده" : "منتشر شده"),
                NameOfCategories = NameOfCategories,
                TagNamesList = newsGroup.First().NewsGroup.Select(a => a.NameOfTags).Distinct().ToList(),
                TagIdsList = newsGroup.First().NewsGroup.Select(a => a.IdOfTags).Distinct().ToList(),
                ImageName = newsGroup.First().NewsGroup.First().ImageName,
                AuthorInfo = newsGroup.First().NewsGroup.First().AuthorInfo,
                NumberOfComments = newsGroup.First().NewsGroup.First().NumberOfComments,
                PublishDateTime = newsGroup.First().NewsGroup.First().PublishDateTime,
            };

            return news;
        }
        /// <summary>
        /// در این متد تاریخ انتشار خبر را برای سورت نزولی و سعودی دربافت میکنیم 
        /// </summary>
        /// <param name="PublishDateTime"></param>
        /// <returns></returns>
        /// خبرهایی که تاریخ انتشارشون قبل از پابلیش دیت تایم هست رو گرفتیم
        public async Task<List<NewsViewModel>> GetNextAndPreviousNews(DateTime? PublishDateTime)
        {
            var newsList = new List<NewsViewModel>();
             
            ///خبر قبلی (قبل از خبر فعلی) را گرفته و درون نیوز لیست قرار میدهیم
            newsList.Add(await (from n in _context.News.Include(v => v.Visits).Include(l => l.Likes).Include(c => c.Comments)
                                where (n.IsPublish == true && n.PublishDateTime <= DateTime.Now && n.PublishDateTime < PublishDateTime)
                                select (new NewsViewModel
                                {
                                    NewsId = n.NewsId,
                                    ShortTitle = n.Title.Length > 50 ? n.Title.Substring(0, 50) + "..." : n.Title,
                                    Url = n.Url,
                                    Title = n.Title,
                                    NumberOfVisit = n.Visits.Select(v => v.NumberOfVisit).Sum(),
                                    NumberOfLike = n.Likes.Where(l => l.IsLiked == true).Count(),
                                    NumberOfDisLike = n.Likes.Where(l => l.IsLiked == false).Count(),
                                    NumberOfComments = n.Comments.Count(),
                                    ImageName = n.ImageName,
                                    PublishDateTime = n.PublishDateTime == null ? new DateTime(01, 01, 01) : n.PublishDateTime,
                                })).OrderByDescending(o => o.PublishDateTime).AsNoTracking().FirstOrDefaultAsync());
            ///خبر بعد از خبر فعلی را دریافت میکنیم 
            newsList.Add(await (from n in _context.News.Include(v => v.Visits).Include(l => l.Likes).Include(c => c.Comments)
                                where (n.IsPublish == true && n.PublishDateTime <= DateTime.Now && n.PublishDateTime > PublishDateTime)
                                select (new NewsViewModel
                                {
                                    NewsId = n.NewsId,
                                    ShortTitle = n.Title.Length > 50 ? n.Title.Substring(0, 50) + "..." : n.Title,
                                    Url = n.Url,
                                    Title = n.Title,
                                    NumberOfVisit = n.Visits.Select(v => v.NumberOfVisit).Sum(),
                                    NumberOfLike = n.Likes.Where(l => l.IsLiked == true).Count(),
                                    NumberOfDisLike = n.Likes.Where(l => l.IsLiked == false).Count(),
                                    NumberOfComments = n.Comments.Count(),
                                    ImageName = n.ImageName,
                                    PublishDateTime = n.PublishDateTime == null ? new DateTime(01, 01, 01) : n.PublishDateTime,
                                    ///سعودی مرتب میکنیم و اولین خبر را اخذ میکنیم 
                                })).OrderBy(o => o.PublishDateTime).AsNoTracking().FirstOrDefaultAsync());
            ///در این لیست دوخبر داریم خبر قبل و خبر بعد از خبر فعلی
            return newsList;
        }

        public async Task<List<Comment>> GetNewsCommentsAsync(string newsId)
        {
            var comments = await (from c in _context.Comments
                                  where (c.ParentCommentId == null && c.NewsId == newsId && c.IsConfirm == true)
                                  select new Comment { CommentId = c.CommentId, Desription = c.Desription, Email = c.Email, PostageDateTime = c.PostageDateTime, Name = c.Name, IsConfirm = c.IsConfirm }).ToListAsync();
            foreach (var item in comments)
                await BindSubComments(item);

            return comments;
        }

        public async Task BindSubComments(Comment comment)
        {
            var subComments = await (from c in _context.Comments
                                     where (c.ParentCommentId == comment.CommentId && c.IsConfirm == true)
                                     select new Comment { CommentId = c.CommentId, Desription = c.Desription, Email = c.Email, PostageDateTime = c.PostageDateTime, Name = c.Name, IsConfirm = c.IsConfirm }).ToListAsync();

            foreach (var item in subComments)
            {
                await BindSubComments(item);
                comment.comments.Add(item);
            }
        }
        /// <summary>
        /// متد دریافت اخبار مرتبط
        /// </summary>
        /// <param name="number"></param> چند خبر مرتبط با خبر فعلی نیاز داریم ؟
        /// <param name="tagIdList"></param> تگ اخبار مرتبط چیست ؟
        /// <param name="newsId"></param> آیدی خبر 
        /// <returns></returns>
        public async Task<List<NewsViewModel>> GetRelatedNews(int number, List<string> tagIdList, string newsId)
        {
            var newsList = new List<NewsViewModel>();
            int randomRow;
            ///تعداد خبرهای مشابه با خبر فعلی
            ///تگ آیدی شون در لیست آیدی های تگ باشه 
            int newsCount = _context.News.Include(t => t.NewsTags).Where(n => n.IsPublish == true && n.PublishDateTime <= DateTime.Now && tagIdList.Any(y => n.NewsTags.Select(x => x.TagId).Contains(y)) && n.NewsId != newsId).Count();
            ///این حلقه فور به تعداد عدد نامبر تکرار میشه و خبر رندوم توسط حلقه فور سلکت میشه 
            for (int i = 0; i < number && i < newsCount; i++)
            {
                randomRow = CustomMethods.RandomNumber(1, newsCount + 1);
                var news = await _context.News.Include(t => t.NewsTags).Include(c => c.Comments).Include(l => l.Likes).Include(l => l.Visits).Where(n => n.IsPublish == true && n.PublishDateTime <= DateTime.Now && tagIdList.Any(y => n.NewsTags.Select(x => x.TagId).Contains(y)) && n.NewsId != newsId)
                    .Select(n => new NewsViewModel
                    {
                        Title = n.Title,
                        Url = n.Url,
                        NewsId = n.NewsId,
                        ImageName = n.ImageName,
                        PublishDateTime = n.PublishDateTime,
                        NumberOfVisit = n.Visits.Select(v => v.NumberOfVisit).Sum(),
                        NumberOfLike = n.Likes.Where(l => l.IsLiked == true).Count(),
                        NumberOfDisLike = n.Likes.Where(l => l.IsLiked == false).Count(),
                        NumberOfComments = n.Comments.Count(),
                    })
                    .Skip(randomRow - 1).Take(1).FirstOrDefaultAsync();

                newsList.Add(news);
            }

            return newsList;
        }
    }
}
