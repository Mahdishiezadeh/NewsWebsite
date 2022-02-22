using NewsWebsite.Entities;
using NewsWebsite.ViewModels.News;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsWebsite.Data.Contracts
{
    public interface INewsRepository
    {
        string CheckNewsFileName(string fileName);
        int CountNewsPublished();
        List<NewsViewModel> GetPaginateNews(int offset, int limit, Func<IGrouping<string, NewsViewModel>, Object> orderByAscFunc, Func<IGrouping<string, NewsViewModel>, Object> orderByDescFunc, string searchText, bool? isPublish, bool? isInternal);
        Task<List<NewsViewModel>> MostViewedNews(int offset, int limit, string duration);
        Task<List<NewsViewModel>> MostTalkNews(int offset, int limit, string duration);
        Task<List<NewsViewModel>> MostPopularNews(int offset, int limit);
        Task<NewsViewModel> GetNewsById(string newsId);
        Task<List<Comment>> GetNewsCommentsAsync(string newsId);
        Task BindSubComments(Comment comment);
        Task<List<NewsViewModel>> GetNextAndPreviousNews(DateTime? PublishDateTime);
        Task<List<NewsViewModel>> GetRelatedNews(int number, List<string> tagIdList, string newsId);
    }
}
