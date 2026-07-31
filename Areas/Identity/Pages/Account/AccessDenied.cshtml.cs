using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace aspnet_core_tutorial.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Static page shown when an authenticated user is signed in but lacks the role required
    /// for the resource they requested, matching the cookie configuration's AccessDeniedPath.
    /// </summary>
    /// <remarks>
    /// Created by edgar.muhamyangabo on 7/31/26
    /// Author : edgar.muhamyangabo
    /// Date : 7/31/26
    /// Project : aspnet_core_tutorial
    /// </remarks>
    [AllowAnonymous]
    public class AccessDeniedModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
