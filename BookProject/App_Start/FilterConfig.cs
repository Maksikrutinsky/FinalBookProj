using System.Web;
using System.Web.Mvc;
using BookProject.Filters;

namespace BookProject
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new CartCountAttribute());
        }
    }
}
