using Microsoft.AspNetCore.Mvc;
using YesSql.Services;
using YesSql.Samples.Web.Models;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Index()
        {
            BlogPost post;
            using (var session = _store.CreateSession())
            {
                post = await session.Query<BlogPost>().FirstOrDefaultAsync();
            }

            return View(post);
        }
    }
}
