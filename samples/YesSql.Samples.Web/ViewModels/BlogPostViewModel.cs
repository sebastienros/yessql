using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using YesSql.Samples.Web.Models;
using YesSql.Samples.Web.ModelBinding;
using YesSql.Filters.Query;

namespace YesSql.Samples.Web.ViewModels
{
    public class BlogPostViewModel
    {
        public IEnumerable<BlogPost> BlogPosts { get; set; }
        public Filter Search { get; set; }
    }

    public class Filter
    {
        public string Author { get; set; }
        public string SearchText { get; set; }
        public string OriginalSearchText { get; set; }
        public BlogPostStatus SelectedStatus { get; set; }
        public BlogPostSort SelectedSort { get; set; }

        [ModelBinder(BinderType = typeof(QueryFilterEngineModelBinder<BlogPost>), Name = nameof(SearchText))]
        public QueryFilterResult<BlogPost> FilterResult { get; set; }

        [BindNever]
        public List<SelectListItem> Statuses { get; set; } = new();

        [BindNever]
        public List<SelectListItem> Sorts { get; set; } = new();
    }

    public enum BlogPostStatus
    {
        Default,
        Draft,
        Published
    }

    public enum BlogPostSort
    {
        Newest,
        Oldest,
    }
}
