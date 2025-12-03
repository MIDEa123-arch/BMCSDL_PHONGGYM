using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UserApp.Controllers
{
    public class AdminBaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["Admin"] == null)
            {
                // Chuyển hướng về Login
                filterContext.Result = RedirectToAction("Login", "Staff");
                return;
            }
            base.OnActionExecuting(filterContext);
        }
    }

}