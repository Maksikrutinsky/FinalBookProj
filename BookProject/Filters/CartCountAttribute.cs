using System;
using System.Linq;
using System.Web.Mvc;
using BookProject.Models;

namespace BookProject.Filters
{
    public class CartCountAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controller = filterContext.Controller as Controller;
            if (controller != null && controller.Session["UserId"] != null)
            {
                using (var db = new EBookLibraryEntities())
                {
                    int userId = Convert.ToInt32(controller.Session["UserId"]);
                    string sql = "SELECT COUNT(*) FROM OrderItems WHERE OrderId IN " +
                                 "(SELECT OrderId FROM Orders WHERE UserId = @userId AND Status = 'InCart')";
                    var cartCount = db.Database.SqlQuery<int>(sql,
                        new System.Data.SqlClient.SqlParameter("@userId", userId)
                    ).First();
                    controller.ViewBag.CartItemsCount = cartCount;
                }
            }
            base.OnActionExecuting(filterContext);
        }
    }
}