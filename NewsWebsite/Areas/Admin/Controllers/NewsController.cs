using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsWebsite.Common;
using NewsWebsite.Common.Attributes;
using NewsWebsite.Data.Contracts;
using NewsWebsite.Entities;
using NewsWebsite.ViewModels.News;

namespace NewsWebsite.Areas.Admin.Controllers
{
    public class NewsController : BaseController
    {
        private readonly IUnitOfWork _uw;
        private readonly IHostingEnvironment _env;
        private const string NewsNotFound = "خبر یافت نشد.";
        private readonly IMapper _mapper;

        public NewsController(IUnitOfWork uw, IMapper mapper, IHostingEnvironment env)
        {
            _uw = uw;
            _uw.CheckArgumentIsNull(nameof(_uw));

            _env = env;
            _env.CheckArgumentIsNull(nameof(_env));

            _mapper = mapper;
            _mapper.CheckArgumentIsNull(nameof(_mapper));
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public IActionResult GetNews(string search, string order, int offset, int limit, string sort)
        {
            List<NewsViewModel> news;
            ///در این کد تعداد خبرها را دریافت کردیم 
            int total = _uw.BaseRepository<News>().CountEntities();
            if (!search.HasValue())
                search = "";

            if (limit == 0)
                limit = total;
            ///چک کردیم سورت بر اساس شورت تایتل هست ؟
            ///شورت تایتل در کد جی کوئری پارشیال ویو نیوز تایتل نام گذاری شده
            ///که وقتی در ویو کلیک میشود این مقدار به کد سرور و این اکشن متد ارسال میشود
            if (sort == "ShortTitle")
            {
                if (order == "asc")
                    ///این کد در درس دوم فصل سوم تغییر کرد 
                    ///زیرا نوع سورت را در ریپازیتوری نیوز به کلی تغییر داده ایم 
                    ///در این سورت نزولی نداریم و سورت بر اساس عتوان است 
                    news =  _uw.NewsRepository.GetPaginateNews(offset, limit, item=>item.First().Title,item=>"", search,null,null);
                else
                    ///در این سورت سعودی داریم و سورت براساس تایتل (عنوان) است 
                    news = _uw.NewsRepository.GetPaginateNews(offset, limit, item => "", item => item.First().Title, search,null,null);
            }

            else if (sort == "بازدید")
            {
                if (order == "asc")
                    news =  _uw.NewsRepository.GetPaginateNews(offset, limit,item=>item.First().NumberOfVisit,item=>"", search,null,null);
                else
                    news =  _uw.NewsRepository.GetPaginateNews(offset, limit, item => "",item=>item.First().NumberOfVisit, search,null,null);
            }

            else if (sort == "لایک")
            {
                if (order == "asc")
                    news =  _uw.NewsRepository.GetPaginateNews(offset, limit, item=>item.First().NumberOfLike, item => "", search,null,null);
                else
                    news =  _uw.NewsRepository.GetPaginateNews(offset, limit, item => "",item=>item.First().NumberOfLike, search, null,null);
            }

            else if (sort == "دیس لایک")
            {
                if (order == "asc")
                    news = _uw.NewsRepository.GetPaginateNews(offset, limit,item=>item.First().NumberOfDisLike, item => "", search, null,null);
                else
                    news =  _uw.NewsRepository.GetPaginateNews(offset, limit, item => "",item=>item.First().NumberOfDisLike, search, null,null);
            }

            else if (sort == "تاریخ انتشار")
            {
                if (order == "asc")
                    news =  _uw.NewsRepository.GetPaginateNews(offset, limit, item=>item.First().PersianPublishDate, item => "", search, null,null);
                else
                    news =  _uw.NewsRepository.GetPaginateNews(offset, limit, item => "",item=>item.First().PersianPublishDate, search, null,null);
            }

            else
                    news = _uw.NewsRepository.GetPaginateNews(offset, limit, item => "", item => item.First().PersianPublishDate, search, null,null);

            if (search != "")
                total = news.Count();

            return Json(new { total = total, rows = news });
        }
        /// <summary>
        /// توسط این اکشن متد ما ویو مهم 
        /// CreateOrUpdate
        ///  را به کاربر نمایش میدهیم
        /// </summary>
        /// <param name="newsId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> CreateOrUpdate(string newsId)
        {
            ///یک نمونه از ویومدل نیوز ایجاد کردیم
            NewsViewModel newsViewModel = new NewsViewModel();
            ///اطلاعات تمام تگ ها را از دیتابیس اخد و درون این متغییر ریختیم
            ///به خاطر این کد جی کوئری (در ویوکریت اور آپدیت) 
            /// var states = @Html.Raw(Json.Serialize(ViewBag.Tags));
            /// ما ویوبگ تگز را مقداردهی میکنیم
            ViewBag.Tags = _uw._Context.Tags.Select(t => t.TagName).ToList();
            /// در اینجا یک نیوز کتگوری ویومدل ایجاد و کتگوری ها را دیتابیس استخراج و به آن پاس میدهیم
            newsViewModel.NewsCategoriesViewModel = new NewsCategoriesViewModel(await _uw.CategoryRepository.GetAllCategoriesAsync(), null);
            ///چک میکنیم نیوز آیدی مقدار دارد ؟ 
            if (newsId.HasValue())
            {
                ///از ایگر لودینگ وفول آتر جوین برای اخذ تگ ها و کتگوری های خبر استفاده کردیم 
                ///داخل نویگیشن پراپرتی نیوز کتگوریز یک لیست قرار میگیرد 
                ///(c => c.NewsCategories)
                ///از کلاس نیوز کتگوری که دو پراپرتی نیوز آیدی و نیوز کتگوری دارد
                ///پس این دو پراپرتی مقداردهی میشوند و به صورت یک لیست در اینجا قرار میگیرند
                var news = await (from n in _uw._Context.News.Include(c => c.NewsCategories)
                            join w in _uw._Context.NewsTags on n.NewsId equals w.NewsId into bc
                            from bct in bc.DefaultIfEmpty()
                            join t in _uw._Context.Tags on bct.TagId equals t.TagId into cg
                            from cog in cg.DefaultIfEmpty()
                            where (n.NewsId == newsId)
                            select new NewsViewModel
                            {
                                NewsId = n.NewsId,
                                Title = n.Title,
                                Abstract=n.Abstract,
                                Description = n.Description,
                                PublishDateTime = n.PublishDateTime,
                                IsPublish = n.IsPublish,
                                ImageName = n.ImageName,
                                IsInternal = n.IsInternal,
                                NewsCategories = n.NewsCategories,
                                Url = n.Url,
                                NameOfTags = cog!=null? cog.TagName:"",
                            }).ToListAsync();

                if (news != null)
                {
                    ///عمل مپینگ را انجام میدهیم و اولین رکورد مهم است زیرا بقیه اطلاعات تکراری است
                    newsViewModel = _mapper.Map<NewsViewModel>(news.FirstOrDefault());
                    ///چک میکنیم که زمان خبر از زمان فعلی بزرگتر است؟
                    if (news.FirstOrDefault().PublishDateTime > DateTime.Now)
                    {///اگر از زمان فعلی بزرگتر باشد در آینده منتشر میشود
                        newsViewModel.FuturePublish = true;
                        ///روز انتشار خبر
                        newsViewModel.PersianPublishDate = news.FirstOrDefault().PublishDateTime.ConvertMiladiToShamsi("yyyy/MM/dd");
                        ///ساعت انتشار خبر
                        newsViewModel.PersianPublishTime = news.FirstOrDefault().PublishDateTime.Value.TimeOfDay.ToString();
                    }
                    newsViewModel.NewsCategoriesViewModel = new NewsCategoriesViewModel(await _uw.CategoryRepository.GetAllCategoriesAsync(),news.FirstOrDefault().NewsCategories.Select(n=>n.CategoryId).ToArray());
                    ///در اینجا تگ های خبر را اخذ و با کاراکتر داخل پرانتز به هم بچسبانیم
                    ///متد کمباین ویت داخل کلاس استرینگ اکستنشن تعریف شده است
                    newsViewModel.NameOfTags = news.Select(t => t.NameOfTags).ToArray().CombineWith(',');
                }

            }
            
            return View(newsViewModel);
        }

        [HttpPost]
        ///داخل این اکشن متد قصد داریم مقادیر دو تگ اینپوت زیر را که در ویو کریت  اور آپدیت است از کاربر دریافت کنیم 
        //<input type = "submit" value="@(Model.NewsId.HasValue()?"به روزرسانی":"انتشار")"
        //<input type = "submit" value="ذخیره پیش نویس"
        ///ما مقادیر ولیو این تگ ها را با متغییر زیر دریافت میکنیم
        ///string submitButton
        public async Task<IActionResult> CreateOrUpdate(NewsViewModel viewModel,string submitButton)
        {
            ViewBag.Tags = _uw._Context.Tags.Select(t => t.TagName).ToList();
            ///ابتدا تمام کتگوری ها را برای نمایش درختی دریافت و در متغییر دوم میبایست 
            ///که نوع دسته بندی را ارسال کنیم اما
            ///چون هنوز دسته بندی برای ایجاد در نظر گرفته نشده مقدار نال را ارسال میکنیم
            viewModel.NewsCategoriesViewModel = new NewsCategoriesViewModel(await _uw.CategoryRepository.GetAllCategoriesAsync(), viewModel.CategoryIds);
            ///اگر پراپرتی فیوچر پابلیش ترو باشه 
            if (!viewModel.FuturePublish)
            {
                ///این دو پراپرتی را از مدل استیت پاک میکنیم
                ModelState.Remove("PersianPublishTime");
                ModelState.Remove("PersianPublishDate");
            }
            ///اگر نیوز آیدی مقدار داشت ایمیج آن را نیز حذف میکنیم
            if(viewModel.NewsId.HasValue())
                ModelState.Remove("ImageFile");
           
            if (ModelState.IsValid)
            {///به صورت پیش فرض ایز پابلیش فالس است 
                if (submitButton != "ذخیره پیش نویس")
                    viewModel.IsPublish = true;

                if (viewModel.ImageFile != null)
                    viewModel.ImageName = $"news-{StringExtensions.GenerateId(10)}.jpg";
                ///کدهای این بخش مربوط به ویرایش است
                if (viewModel.NewsId.HasValue())
                {
                    ///اطلاعات خبر بر اساس آیدی دریافت کردیم
                    ///به عنوان پارامتر اول شرط مون رو بر اساس آیدی خبر ارسال کردیم
                    ///پارامتر دوم برای سورت هست  که نال قرار دادیم
                    ///پارامتر سوم ایگر لودینگ هست و ما نویگیشن پراپرتی نیوز کتگوریز و نیوز تگ رو ارسال کردیم
                    ///در انتهای کد ریزالت قرار دادیم چون متد به صورت موازی یا ایسینک هست
                    ///زمانی از فایند بای کاندیشن ایسینک استفاده میکنیم که نویگیشن پراپرتی باشد
                    var news = _uw.BaseRepository<News>().FindByConditionAsync(n=>n.NewsId==viewModel.NewsId,null,n => n.NewsCategories,n=>n.NewsTags).Result.FirstOrDefault();
                    if (news == null)
                        ModelState.AddModelError(string.Empty, NewsNotFound);
                    else
                    {
                        if (viewModel.IsPublish && news.IsPublish == false)
                            viewModel.PublishDateTime = DateTime.Now;

                        if (viewModel.IsPublish && news.IsPublish == true)
                        {
                            if (viewModel.PersianPublishDate.HasValue())
                            {
                                var persianTimeArray = viewModel.PersianPublishTime.Split(':');
                                viewModel.PublishDateTime = viewModel.PersianPublishDate.ConvertShamsiToMiladi().Date + new TimeSpan(int.Parse(persianTimeArray[0]), int.Parse(persianTimeArray[1]), 0);
                            }
                            else
                                viewModel.PublishDateTime = news.PublishDateTime;
                        }

                        if (viewModel.ImageFile != null)
                        {
                            viewModel.ImageFile.UploadFileBase64($"{_env.WebRootPath}/newsImage/{viewModel.ImageName}");
                            FileExtensions.DeleteFile($"{_env.WebRootPath}/newsImage/{news.ImageName}");
                        }

                        else
                            viewModel.ImageName = news.ImageName;
                        
                        if (viewModel.NameOfTags.HasValue())
                            ///در اینجا نیوزتگز ویومدل را مقداردهی میکنیم
                            ///در این پراپرتی تگ هایی را مقداردهی میکنیم که قرار است جایگزین تگ های قبلی شوند
                            viewModel.NewsTags = await _uw.TagRepository.InsertNewsTags(viewModel.NameOfTags.Split(','), news.NewsId);
                        
                        else
                            viewModel.NewsTags = news.NewsTags;
                        ///اگر کتگوری آیدی نال بود همان دسته بندی های قبلی خبر را داخل آن قرار میدهیم
                        if (viewModel.CategoryIds == null)
                            viewModel.NewsCategories = news.NewsCategories;
                        else
                            ///در غیر اینصورت آیدی پراپرتی هایی که داخل این متد هست رو سلکت میکنیم و به صورت لیست ذخیره میکنیم
                            viewModel.NewsCategories = viewModel.CategoryIds.Select(c => new NewsCategory { CategoryId = c, NewsId = news.NewsId }).ToList();
                        ///در آخر مقدار یوزر آیدی خبر را به ویو مدل پاس میدهیم
                        viewModel.UserId = news.UserId;
                        ///عمل مپینگ را انجام میدهیم
                        ///ویومدل به متغییر نیوز مپ شود
                        _uw.BaseRepository<News>().Update(_mapper.Map(viewModel, news));
                        await _uw.Commit();
                        ViewBag.Alert = "ذخیره تغییرات با موفقیت انجام شد.";
                        
                    }
                }
                ///در این قسمت درج خبر را انجام میدهیم
                else
                {
                    ///از متد آپلود بیس 64 استفاده کرده ایم و مسیر پوشه ای که قرار است فایل ذخیره شود را داده ایم
                    viewModel.ImageFile.UploadFileBase64($"{_env.WebRootPath}/newsImage/{viewModel.ImageName}");
                    ///برای خبر یک آیدی تولید کرده ایم
                    viewModel.NewsId = StringExtensions.GenerateId(10);
                    ///یوزر آیدی را براساس کاربری که ساین این کرده مقداردهی کردیم
                    ///براساس نوع داده اینتیجر درخواست کردهایم چون نوع کلید اصلی جدول ای پی پی یوز اینتیجر است
                    viewModel.UserId = User.Identity.GetUserId<int>();
                    ///اگر ایز پابلیش ترو بود کاربر قصد دارد خبر را منتشر کند
                    if (viewModel.IsPublish)
                    {
                        ///اگ پابلیش دیت تایم زمان داشت یعنی کاربر قصد انتشار در همین زمان را دارد
                        if (!viewModel.PersianPublishDate.HasValue())
                            viewModel.PublishDateTime = DateTime.Now;
                        ///در غیر این صورت اگر پابلیش دیت تایم مقدار نداشت کاربر در آینده خیر را منتشر میکند 
                        else
                        {
                            ///در اینجا ساعت و دقیقه را از منو سمت چپ بالای ویو کریت اور آپدیت که کاربر وارد میکند
                            ///را اخذ و آن را از هم جدا میکنیم و علامت دو تقطه میگذاریم
                            var persianTimeArray = viewModel.PersianPublishTime.Split(':');
                            ///در اینجا نیز مقدار تاریخ وارد شده توسط کاربر را به تاریخ میلادی تبدیل کرده ایم و با دات دیت تاریخ را جدا کردیم
                            ///سپس با تایم اسپن به عنوان پارامتر اول ساعت را به آن پاس دادیم
                            ///و به عنوان پارامتر دوم هم دقیقه را به تایم اسپن پاس دادیم
                            ///پارامتر سوم ثانیه است که مقدار آن را صفر قرارداده ایم
                            ///از دات پارس هم برای تبدیل نوع استرینگ به اینت کمک گرفته ایم
                            viewModel.PublishDateTime = viewModel.PersianPublishDate.ConvertShamsiToMiladi().Date + new TimeSpan(int.Parse(persianTimeArray[0]), int.Parse(persianTimeArray[1]), 0);
                        }
                    }
                    ///در اینجا چک کردیم مقدار آرایه کتگوری آیدی نال نباشد
                    if (viewModel.CategoryIds != null)
                        ///و اگر نال نباشد مقدار آیدی این دسته بندی ها در متغییر نیوزکتگوریز قرار میگیرد
                        ///که لیستی از نوع نیوز کتگوری عملا داخل نیوز کتگوریز ریخته میشود 
                        viewModel.NewsCategories=viewModel.CategoryIds.Select(c=>new NewsCategory { CategoryId = c }).ToList();
                    else
                        viewModel.NewsCategories = null;
                    ///در اینجا چک میکنیم تگی انتخاب شده است یا خیر و تگها را داخل پراپرتی نیم آو تگ میریزیم
                    ///و در ویو مربوط به این کنترلر به تگ اینپوت زیر متصل هستیم
                    //< input type = "text" id = "tagstype" asp -for= "NameOfTags" class="form-control" style="width:400px;">
                    if (viewModel.NameOfTags.HasValue())
                        ///براساس متد اسپلیت یک آرایه را بازگشت میدهیم و به متد اینزرت نیوز تگ ارسال کردیم
                        ///هر آنچه در TagsRepository
                        ///توسط این متد 
                        /// public async Task<List<NewsTag>> InsertNewsTags(string[] tags, string newsId = null)
                        /// به عنوان تگ شناسایی میشود داخل پراپرتی نیوز تگ میریزیم
                        viewModel.NewsTags = await _uw.TagRepository.InsertNewsTags(viewModel.NameOfTags.Split(","));
                    else
                        viewModel.NewsTags = null;
                    ///از آتومپر استفاده میکنیم و ویومدل به یک نمونه از کلاس نیوز مپ میشه و سپس از متد کریت استفاده کردیم
                    await _uw.BaseRepository<News>().CreateAsync(_mapper.Map<News>(viewModel));
                    await _uw.Commit();
                    ///نام اکشن متد را به نیم آو پاش بدهیم اصولی تر است و سپس ریدایرکت به اکشن متد میشویم
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(viewModel);
        }


        [HttpGet, AjaxOnly]
        ///یک اکشن متد با اتربیوت گت که رویکرد اصلی آن گرفتن آیدی خبر است
        public async Task<IActionResult> Delete(string newsId)
        {///بررسی میکنیم آیدی نال نباشد 
            if (!newsId.HasValue())
                ModelState.AddModelError(string.Empty, NewsNotFound);
            else ///اگر نال نبود
            {///خبر رو بر اساس آیدی دریافت میکنیم
                var news = await _uw.BaseRepository<News>().FindByIdAsync(newsId);
                ///چک میکنیم نیوز نال نباشه 
                if (news == null)
                    ModelState.AddModelError(string.Empty, NewsNotFound);
                else
                    ///متغییر نیوز رو هم به پارشیال ویو ارسال میکنیم
                    return PartialView("_DeleteConfirmation", news);
            }
            return PartialView("_DeleteConfirmation");
        }


        [HttpPost, ActionName("Delete"), AjaxOnly]
        ///اطلاعات خبر رو در قالب یک ویومدل دریافت میکنه
        public async Task<IActionResult> DeleteConfirmed(News model)
        {
            if (model.NewsId == null)
                ModelState.AddModelError(string.Empty, NewsNotFound);
            else
            {
                ///پیدا کردن خبر بر اساس ایدی خبر
                var news = await _uw.BaseRepository<News>().FindByIdAsync(model.NewsId);
                if (news == null)
                    ModelState.AddModelError(string.Empty, NewsNotFound);
                else
                {
                    ///حذ ف خبر
                    _uw.BaseRepository<News>().Delete(news);
                    await _uw.Commit();
                    ///تصویر خبر را نیز حذف میکنیم
                    FileExtensions.DeleteFile($"{_env.WebRootPath}/newsImage/{news.ImageName}");
                    TempData["notification"] = DeleteSuccess;
                    ///در آخر نیوز را به پارشیال ویو دیلیت کانفیرم پاس میدهیم
                    return PartialView("_DeleteConfirmation", news);
                }
            }
            return PartialView("_DeleteConfirmation");
        }


        [HttpPost, ActionName("DeleteGroup"), AjaxOnly]
        ///در حذف گروهی یک آرایه را دریافت میکنیم و چک میکنیم آرایه مقدار داشته باشد
        public async Task<IActionResult> DeleteGroupConfirmed(string[] btSelectItem)
        {
            if (btSelectItem.Count() == 0)
                ModelState.AddModelError(string.Empty, "هیچ خبری برای حذف انتخاب نشده است.");
            else
            {
                ///اگر آرایه عضوی را داشت به کمک حلقه آن را حذف میکنیم
                foreach (var item in btSelectItem)
                {
                    var news = await _uw.BaseRepository<News>().FindByIdAsync(item);
                    _uw.BaseRepository<News>().Delete(news);
                    FileExtensions.DeleteFile($"{_env.WebRootPath}/newsImage/{news.ImageName}");
                }
                await _uw.Commit();
                TempData["notification"] = "حذف گروهی اطلاعات با موفقیت انجام شد.";
            }

            return PartialView("_DeleteGroup");
        }
    }
}