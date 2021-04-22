using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using YesSql.Filters.Query;
using YesSql.Samples.Web.Models;
using YesSql.Samples.Web.ModelBinding;
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
        public async Task<IActionResult> Index([ModelBinder(BinderType = typeof(QueryFilterEngineModelBinder<BlogPost>), Name = "q")] QueryFilterResult<BlogPost> termList)
        {
            IEnumerable<BlogPost> posts;

            string currentSearchText = String.Empty;

            using (var session = _store.CreateSession())
            {
                var query = session.Query<BlogPost>();

                await termList.ExecuteAsync(query, HttpContext.RequestServices);

                currentSearchText = termList.ToString();

                posts = await query.ListAsync();

                // Map termList to model.
                // i.e. SelectedFilter needs to be filled with
                // the selected filter value from the term.
                var search = new Filter
                {
                    SearchText = currentSearchText,
                    OriginalSearchText = currentSearchText,
                    Filters = new List<SelectListItem>()
                    {
                        new SelectListItem("Select...", ""),
                        new SelectListItem("Published", ContentsStatus.Published.ToString()),
                        new SelectListItem("Draft", ContentsStatus.Draft.ToString())
                    }
                };

                termList.MapTo(search);

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
            if (!String.Equals(search.SearchText, search.OriginalSearchText, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", new RouteValueDictionary { { "q", search.SearchText } });
            }

            search.TermList.MapFrom(search);

            return RedirectToAction("Index", new RouteValueDictionary { { "q", search.TermList.ToString() } });  
        }
    }
}
