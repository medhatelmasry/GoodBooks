using Core.Domain;
using LibraryGDB.Models.Financial;
using Microsoft.AspNetCore.Mvc;
using Services.Financial;
using System.Reflection;
using System.Text;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    //[Authorize]
    public class ChartOfAccountsController(IFinancialService financialService) : BaseController
    {
        [HttpGet]
        [Route("accounts")] // api/Financials/CashBanks
        // create a get method for displaying the add accont page
        public IActionResult GetAccounts()
        {
            var accounts = financialService.GetAccounts().ToList();

            var accountTree = BuildAccountGrouping(accounts, null);

            return new ObjectResult(accountTree);

        }

        [Route("AddChartOfAccount")]
        [HttpPost]
        public IActionResult AddAccount(Core.Domain.Financials.Account model)
        {
            string[] errors = null;
            if (ModelState.IsValid)
            {
                // map the view model to the domain model
                var account = new Core.Domain.Financials.Account
                {
                    AccountCode = model.AccountCode,
                    AccountName = model.AccountName,
                    AccountClassId = model.AccountClassId,
                    IsCash = model.IsCash,
                    IsContraAccount = model.IsContraAccount,
                    DrOrCrSide = model.DrOrCrSide,
                    CompanyId = model.CompanyId,
                };

                // save the account
                financialService.AddAccount(account);

                // redirect to the index page
                return new OkObjectResult(Ok());
            }
            else
            {
                errors = new string[ModelState.ErrorCount];
                foreach (var val in ModelState.Values)
                    for (int i = 0; i < ModelState.ErrorCount; i++)
                        errors[i] = val.Errors[i].ErrorMessage;

                return new BadRequestObjectResult(errors);
            }
        }

        private IList<Dto.Financial.Account> BuildAccountGrouping(IList<Core.Domain.Financials.Account> allAccounts,
int? parentAccountId)
        {
            var accountTree = new List<Dto.Financial.Account>();
            var childAccounts = allAccounts.Where(o => o.ParentAccountId == parentAccountId).ToList();

            foreach (var account in childAccounts)
            {
                var accountDto = new Dto.Financial.Account()
                {
                    Id = account.Id,
                    AccountClassId = account.AccountClassId,
                    ParentAccountId = account.ParentAccountId,
                    CompanyId = account.CompanyId,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Description = account.Description,
                    IsCash = account.IsCash,
                    IsContraAccount = account.IsContraAccount,
                    Balance = account.Balance,
                    DebitBalance = account.DebitBalance,
                    CreditBalance = account.CreditBalance
                };
                var children = BuildAccountGrouping(allAccounts, account.Id);
                accountDto.ChildAccounts = children;
                accountTree.Add(accountDto);
            }

            return accountTree;
        }

        private void SetupChartOfAccountsAndAccountClasses()
        {
            Console.WriteLine("SetupChartOfAccountsAndAccountClasses() starting.");
            try
            {
                IList<Core.Domain.Financials.AccountClass> accountClasses = new List<Core.Domain.Financials.AccountClass>();
                // If no accounts found just return.
                if (financialService.GetAccounts().Any()) return;
                string[,] values = new string[10, 8];
                var assembly = Assembly.GetEntryAssembly();
                var resourceStream = assembly.GetManifestResourceStream("Api.Data.coa.csv");
                using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
                {
                    var coa = reader.ReadToEndAsync().Result;
                }
                List<Core.Domain.Financials.Account> accounts = new List<Core.Domain.Financials.Account>();

                for (var i = 1; i < (values.Length / 8); i++)
                {
                    Core.Domain.Financials.Account account =
                        new Core.Domain.Financials.Account
                        {
                            AccountCode = values[i, 0],
                            AccountName = values[i, 1],
                            AccountClassId = int.Parse(values[i, 3]),
                            IsCash = bool.Parse(values[i, 5]),
                            IsContraAccount =
                                bool.Parse(values[i, 4])
                        };

                    switch (values[i, 7])
                    {
                        case "DR":
                            account.DrOrCrSide = Core.Domain.DrOrCrSide.Dr;
                            break;
                        case "CR":
                            account.DrOrCrSide = Core.Domain.DrOrCrSide.Cr;
                            break;
                        default:
                            account.DrOrCrSide = Core.Domain.DrOrCrSide.NA;
                            break;
                    }

                    account.CompanyId = 1;
                    accounts.Add(account);
                }

                for (var i = 1; i < (values.Length / 8); i++)
                {
                    string accountCode = values[i, 0];
                    string parentAccountCode = values[i, 2];

                    var account = accounts.FirstOrDefault(a => a.AccountCode == accountCode);
                    var parentAccount = accounts.FirstOrDefault(a => a.AccountCode == parentAccountCode);
                    if (parentAccount != null)
                        account.ParentAccount = parentAccount;
                }

                var assetClass = new Core.Domain.Financials.AccountClass
                {
                    Name = "Assets",
                    NormalBalance = "Dr"
                };
                var liabilitiesClass = new Core.Domain.Financials.AccountClass
                {
                    Name = "Liabilities",
                    NormalBalance = "Cr"
                };
                var equityClass = new Core.Domain.Financials.AccountClass
                {
                    Name = "Equity",
                    NormalBalance = "Cr"
                };
                var revenueClass = new Core.Domain.Financials.AccountClass
                {
                    Name = "Revenue",
                    NormalBalance = "Cr"
                };
                var expenseClass = new Core.Domain.Financials.AccountClass
                {
                    Name = "Expense",
                    NormalBalance = "Dr"
                };
                var temporaryClass = new Core.Domain.Financials.AccountClass
                {
                    Name = "Temporary",
                    NormalBalance = "NA"
                };

                accountClasses.Add(assetClass);
                accountClasses.Add(liabilitiesClass);
                accountClasses.Add(equityClass);
                accountClasses.Add(revenueClass);
                accountClasses.Add(expenseClass);
                accountClasses.Add(temporaryClass);

                foreach (var account in accounts)
                {
                    switch (account.AccountClassId)
                    {
                        case 1:
                            assetClass.Accounts.Add(account);
                            break;
                        case 2:
                            liabilitiesClass.Accounts.Add(account);
                            break;
                        case 3:
                            equityClass.Accounts.Add(account);
                            break;
                        case 4:
                            revenueClass.Accounts.Add(account);
                            break;
                        case 5:
                            expenseClass.Accounts.Add(account);
                            break;
                        case 6:
                            temporaryClass.Accounts.Add(account);
                            break;
                    }
                }
                financialService.SaveAccountClasses(accountClasses);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("SetupChartOfAccountsAndAccountClasses() encounterd an Exception - " + ex.StackTrace);
                throw;
            }
        }

    }
}
