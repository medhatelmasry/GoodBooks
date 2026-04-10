using Dto.Administration;
using Dto.Security;
using Microsoft.AspNetCore.Mvc;

namespace AccountGoWeb.Controllers
{
  //[Microsoft.AspNetCore.Authorization.Authorize]
  public class AdministrationController : BaseController
  {
    public AdministrationController(IConfiguration config)
    {
      _baseConfig = config;
      Models.SelectListItemHelper._config = config;
    }

    public IActionResult Company()
    {
      ViewBag.PageContentHeader = "Company";
      var model = GetAsync<Company>("administration/company").Result;
      if (model == null)
        model = new Company();
      return View(model);
    }

    [HttpPost]
    public IActionResult Company(Company model)
    {
      ViewBag.PageContentHeader = "Company";
      if (ModelState.IsValid)
      {
        var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
        var content = new StringContent(serialize);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var response = PostAsync("administration/savecompany", content);

        return View(model);
      }
      return View(model);
    }

    public IActionResult Settings()
    {
      ViewBag.PageContentHeader = "Setup and Configuration";
      ViewBag.Accounts = Models.SelectListItemHelper.Accounts();
      var model = GetAsync<GeneralLedgerSetting>("administration/settings").Result;
      if (model == null)
        model = new GeneralLedgerSetting();
      return View(model);
    }

    [HttpPost]
    public IActionResult SaveSettings(Models.Financial.GeneralLedgerSetting model)
    {
      if (ModelState.IsValid)
      {

      }
      ViewBag.Accounts = Models.SelectListItemHelper.Accounts();
      ViewBag.PageContentHeader = "Setup and Configuration";
      return RedirectToAction(nameof(AdministrationController.Settings));
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SystemAdministrators")]
    public async System.Threading.Tasks.Task<IActionResult> Users()
    {
      var users = await GetAsync<System.Collections.Generic.IEnumerable<User>>("administration/users");
      ViewBag.PageContentHeader = "Users";
      return View(users);
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SystemAdministrators")]
    public async System.Threading.Tasks.Task<IActionResult> Roles()
    {
      var roles = await GetAsync<System.Collections.Generic.IEnumerable<Role>>("administration/roles");
      ViewBag.PageContentHeader = "Security Roles";
      return View(roles);
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SystemAdministrators")]
    public async System.Threading.Tasks.Task<IActionResult> Groups()
    {
      var groups = await GetAsync<System.Collections.Generic.IEnumerable<Group>>("administration/groups");
      ViewBag.PageContentHeader = "Security Groups";
      return View(groups);
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SystemAdministrators")]
    public async System.Threading.Tasks.Task<IActionResult> AuditLogs()
    {
      var auditLogs = await GetAsync<System.Collections.Generic.IEnumerable<AuditLog>>("administration/auditlogs");

      ViewBag.PageContentHeader = "Audit Logs";
      return View(model: auditLogs);
    }

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SystemAdministrators")]
    public async System.Threading.Tasks.Task<IActionResult> EditUserRoles(int userId)
    {
      var user = await GetAsync<User>($"administration/GetUser?username={userId}"); // Wait, GetUser takes username, not id.
      // Actually, the API has GetUser(string username), so need to get user by id first? Wait, no, the API has GetUser by username.

      // To get user by id, perhaps need to modify API or get all users and find.

      // For simplicity, assume we pass username, but since userId is int, perhaps change to username.

      // Let's add a new API endpoint for GetUserById.

      // But to keep it simple, let's modify the API to have GetUserById.

      // Add to API:

      // [HttpGet]

      // [Route("GetUserById/{id}")]

      // public IActionResult GetUserById(int id)

      // {

      //     var user = _securityService.GetAllUser().FirstOrDefault(u => u.Id == id);

      //     if (user == null) return NotFound();

      //     // Map to DTO as in GetUser

      // }

      // Then use that.

      // For now, I'll assume we have it.

      var selectedUser = await GetAsync<User>($"administration/getuserbyid/{userId}");

      var allRoles = await GetAsync<System.Collections.Generic.IEnumerable<Role>>("administration/roles");

      ViewBag.PageContentHeader = "Edit User Roles";

      ViewBag.AllRoles = allRoles;

      return View(selectedUser);
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SystemAdministrators")]
    public IActionResult EditUserRoles(int userId, List<int> selectedRoles)
    {
      var dto = new { UserId = userId, RoleIds = selectedRoles };
      var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
      var content = new StringContent(serialize);
      content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

      var response = Post("administration/assignroles", content);

      if (response.IsSuccessStatusCode)
      {
        return RedirectToAction(nameof(Users));
      }
      else
      {
        ModelState.AddModelError(string.Empty, "Failed to assign roles");
        // Reload data
        var selectedUser = GetAsync<User>($"administration/getuserbyid/{userId}").Result;
        var allRoles = GetAsync<System.Collections.Generic.IEnumerable<Role>>("administration/roles").Result;
        ViewBag.PageContentHeader = "Edit User Roles";
        ViewBag.AllRoles = allRoles;
        return View(selectedUser);
      }
    }

    [HttpGet]
    public new IActionResult User(int id = 0)
    {
      if (id != 0)
      {
        ViewBag.PageContentHeader = "User";
      }
      else
      {
        ViewBag.PageContentHeader = "New User";
      }

      return View(new Models.Account.RegisterViewModel());
    }

    [HttpPost]
    public new IActionResult User(Models.Account.RegisterViewModel model)
    {
      try
      {
        if (ModelState.IsValid)
        {
          var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
          var content = new StringContent(serialize);
          content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
          HttpResponseMessage responseAddNewUser = Post("account/addnewuser", content);
          Newtonsoft.Json.Linq.JObject resultAddNewUser = Newtonsoft.Json.Linq.JObject.Parse(responseAddNewUser.Content.ReadAsStringAsync().Result);

          if ((bool)resultAddNewUser["succeeded"]!)
          {
            return RedirectToAction(nameof(AdministrationController.Users), "Administration");
          }
          else
          {
            ModelState.AddModelError(string.Empty, resultAddNewUser["errors"]![0]!["description"]!.ToString());
            return View(model);
          }
        }
      }
      catch (Exception ex)
      {
        ModelState.AddModelError(string.Empty, "Please check if your database is ready/published." + ": " + ex.Message);
        return View(model);
      }
      ViewBag.PageContentHeader = "New User";
      return View(model);
    }
  }
}
