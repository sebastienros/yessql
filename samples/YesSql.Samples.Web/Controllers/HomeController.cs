using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Filters.Query;
using YesSql.Samples.Web.ModelBinding;
using YesSql.Samples.Web.Models;
using YesSql.Samples.Web.Services;
using YesSql.Samples.Web.ViewModels;

namespace YesSql.Samples.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStore _store;

        public HomeController(IStore store)
        {
            _store = store;
        }

        [Route("/")]
        public async Task<IActionResult> Index([ModelBinder(BinderType = typeof(QueryFilterEngineModelBinder<BlogPost>), Name = "q")] QueryFilterResult<BlogPost> filterResult)
        {
            IEnumerable<BlogPost> posts;

            using (var session = _store.CreateSession())
            {
                var query = session.Query<BlogPost>();

                await filterResult.ExecuteAsync(new WebQueryExecutionContext<BlogPost>(HttpContext.RequestServices, query));

                var currentSearchText = filterResult.ToString();

                posts = await query.ListAsync();

                // Map termList to model.
                // i.e. SelectedFilter needs to be filled with
                // the selected filter value from the term.
                var search = new Filter
                {
                    SearchText = currentSearchText,
                    OriginalSearchText = currentSearchText
                };

                filterResult.MapTo(search);


                search.Statuses = new List<SelectListItem>()
                {
                    new SelectListItem("Select...", "", search.SelectedStatus == BlogPostStatus.Default),
                    new SelectListItem("Published", BlogPostStatus.Published.ToString(), search.SelectedStatus == BlogPostStatus.Published),
                    new SelectListItem("Draft", BlogPostStatus.Draft.ToString(), search.SelectedStatus == BlogPostStatus.Draft)
                };

                search.Sorts = new List<SelectListItem>()
                {
                    new SelectListItem("Newest", BlogPostSort.Newest.ToString(), search.SelectedSort == BlogPostSort.Newest),
                    new SelectListItem("Oldest", BlogPostSort.Oldest.ToString(), search.SelectedSort == BlogPostSort.Oldest)
                };

                var vm = new BlogPostViewModel
                {
                    BlogPosts = posts,
                    Search = search
                };

                return View(vm);
            }
        }

        [HttpPost("/")]
        public IActionResult IndexPost(Filter search)
        {
            // When the user has typed something into the search input no evaluation is required.
            // But we might normalize it for them.
            if (!string.Equals(search.SearchText, search.OriginalSearchText, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", new RouteValueDictionary { { "q", search.SearchText } });
            }

            search.FilterResult.MapFrom(search);

            return RedirectToAction("Index", new RouteValueDictionary { { "q", search.FilterResult.ToString() } });
        }
    }
}
