using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HOA_Management_System.Filters
{
    public class UserAuthAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        // Constructor: Accepts a list of roles allowed to enter (e.g., "Admin", "Encoder")
        public UserAuthAttribute(params string[] roles)
        {
            _allowedRoles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. CHECK LOGIN STATUS
            // If there is no "UserId" in the session, they are not logged in.
            var userId = context.HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                // Force Redirect to Login
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 2. CHECK ROLE ACCESS
            // If the controller requires specific roles, check if the user has them.
            if (_allowedRoles.Length > 0)
            {
                var userRole = context.HttpContext.Session.GetString("Role");

                // If user has no role OR their role is not in the allowed list
                if (string.IsNullOrEmpty(userRole) || !_allowedRoles.Contains(userRole))
                {
                    // Homeowner trying to access Admin page -> Send to their Home
                    if (userRole == "Homeowner")
                    {
                        context.Result = new RedirectToActionResult("Index", "Home", null);
                    }
                    else
                    {
                        // Unknown/Unauthorized -> Back to Login
                        context.Result = new RedirectToActionResult("Login", "Account", null);
                    }
                }
            }

            base.OnActionExecuting(context);
        }
    }
}