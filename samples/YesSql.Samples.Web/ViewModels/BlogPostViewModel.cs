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
        public ContentsStatus SelectedFilter { get; set; }

        [ModelBinder(BinderType = typeof(QueryFilterEngineModelBinder<BlogPost>), Name = "SearchText")]
        public QueryFilterResult<BlogPost> FilterResult { get; set; }

        [BindNever]
        public List<SelectListItem> Filters { get; set; } = new();


    }
    public enum ContentsStatus
    {
        Default,
        Draft,
        Published
    }
}
