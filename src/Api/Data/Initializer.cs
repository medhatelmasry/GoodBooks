using Core.Domain.Financials;
using Services.Administration;
using Services.Financial;
using Services.Inventory;
using Services.Purchasing;
using Services.Sales;
using Services.Security;
using System.Reflection;
using System.Text;

namespace Api.Data
{
    public class Initializer
    {
        private readonly IAdministrationService _adminService;
        private readonly IFinancialService _financialService;
        private readonly ISalesService _salesService;
        private readonly IPurchasingService _purchasingService;
        private readonly IInventoryService _inventoryService;
        private readonly ISecurityService _securityService;

        public Initializer(IAdministrationService adminService,
            IFinancialService financialService,
            ISalesService salesService,
            IPurchasingService purchasingService,
            IInventoryService inventoryService,
            ISecurityService securityService)
        {
            _adminService = adminService;
            _financialService = financialService;
            _salesService = salesService;
            _purchasingService = purchasingService;
            _inventoryService = inventoryService;
            _securityService = securityService;
        }
        public bool Setup()
        {
            /*
             * 1.Company
             * 2.Chart of accounts/account classes
             * 3.Financial year
             * 4.Payment terms
             * 5.GL setting
             * 6.Tax
             * 7.Vendor
             * 8.Customer
             * 9.Items
             * 10.Banks
             * 11.Security Roles
             */

            try
            {
                // 1.Company
                var company = SetupCompany();
                Console.WriteLine("SetupCompany() - Completed!");

                // 2.Chart of accounts/ account classes
                SetupChartOfAccountsAndAccountClasses();
                Console.WriteLine("SetupChartOfAccountsAndAccountClasses() - Completed!");

                //3.Financial year
                SetupFinancialYear();
                Console.WriteLine("SetupFinancialYear() - Completed!");

                //4.Payment terms
                SetupPaymentTerms();
                Console.WriteLine("SetupPaymentTerms() - Completed!");

                //5.gl setting
                SetupLedgerSetting(company);
                Console.WriteLine("setupledgersetting() - completed!");

                //6.Tax
                SetupTax();
                Console.WriteLine("SetupTax() - Completed!");

                // 7. Vendor
                SetupVendor();
                Console.WriteLine("SetupVendor() - Completed!");

                //8.Customer
                SetupCustomer();
                Console.WriteLine("SetupCustomer() - Completed!");

                // 9.Items
                SetupItems();
                Console.WriteLine("SetupItems() - Completed!");

                //10.Banks
                SetupBanks();
                Console.WriteLine("SetupBanks() - Completed!");

                //11.Security Roles
                SetupSecurityRoles();
                Console.WriteLine("SetupSecurityRoles() - Completed!");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Setup() - An error occurred during initialization of database. Clear() method will be called to rollback any changes. Error Message = " + ex.Message);
                return false;
            }
        }

        public bool Clear()
        {
            // Clear the database in this order
            try
            {
                // Security Roles
                if (_securityService.GetRole("SystemAdministrators").Id > 0)
                    _securityService.DeleteRole(_securityService.GetRole("SystemAdministrators").Id);
                if (_securityService.GetRole("GeneralUsers").Id > 0)
                    _securityService.DeleteRole(_securityService.GetRole("SystemAdministrators").Id);

                // Banks

                // Items

                // Customers

                // Vendor

                // Tax

                // GL setting

                // Payment terms

                // Financial year

                // Chart of accounts / account classes

                // Company
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Clear() - An error occurred during clearing database. You may need to re-create the database and run Setup() again. Error Message = " + ex.Message);
                return false;
            }
        }

        private Core.Domain.Company SetupCompany()
        {
            // Console.WriteLine("Checking if there is already a default company.");
            // var defaultCompany = _adminService.GetDefaultCompany();
            // Console.WriteLine(defaultCompany == null ? "There is none." : "There is one.");
            // if (defaultCompany == null)
            // {
            //     Console.WriteLine("Default company found. Returning it.");
            //     defaultCompany = new Core.Domain.Company
            //     {
            //         Id = 1,
            //         Name = "Financial Solutions Inc.",
            //         CompanyCode = "100",
            //         ShortName = "FSI",
            //     };
            //     Console.WriteLine("Default company returned.");
            //     _adminService.SaveCompany(defaultCompany);
            //     Console.WriteLine("Default company saved.");
            // }
            // Console.WriteLine("Default company not found. Creating one.");
            // return defaultCompany;

            Console.WriteLine("Checking if there is already a default company.");
            var defaultCompany = _adminService.GetDefaultCompany();
            if (defaultCompany != null)
            {
                Console.WriteLine("Default company found. Returning it.");
                return defaultCompany;
            }
            else
            {
                Console.WriteLine("Default company not found. Creating it.");
                defaultCompany = new Core.Domain.Company
                {
                    //Id = 1,
                    Name = "Financial Solutions Inc.",
                    CompanyCode = "100",
                    ShortName = "FSI",
                    CRA = "012345678"
                };

                _adminService.SaveCompany(defaultCompany);
//                Console.WriteLine($"Default company returned = {defaultCompany.Id}");
                return defaultCompany;
            }
        }

        public static IList<AccountClass> GetAccountClassesFromCsv()
        {
            Console.WriteLine("SetupChartOfAccountsAndAccountClasses() starting.");
            IList<Core.Domain.Financials.AccountClass> accountClasses = new List<Core.Domain.Financials.AccountClass>();
            try
            {
                // If no accounts found just return.
                //if (_financialService.GetAccounts().Any()) return accountClasses;
                string[,] values;
                var assembly = Assembly.GetEntryAssembly();
                var resourceStream = assembly!.GetManifestResourceStream("Api.Data.coa.csv");
                using (var reader = new StreamReader(resourceStream!, Encoding.UTF8))
                {
                    var coa = reader.ReadToEndAsync().Result;
                    values = LoadCsv(coa);
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
                        account!.ParentAccount = parentAccount;
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
                return accountClasses;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("SetupChartOfAccountsAndAccountClasses() encounterd an Exception - " + ex.StackTrace);
                throw;
            }
        }

        private void SetupChartOfAccountsAndAccountClasses()
        {
            Console.WriteLine("SetupChartOfAccountsAndAccountClasses() starting.");
            try
            {
                IList<Core.Domain.Financials.AccountClass> accountClasses = new List<Core.Domain.Financials.AccountClass>();
                // If no accounts found just return.
                if (_financialService.GetAccounts().Any()) return;
                string[,] values;
                var assembly = Assembly.GetEntryAssembly();
                var resourceStream = assembly!.GetManifestResourceStream("Api.Data.coa.csv");
                using (var reader = new StreamReader(resourceStream!, Encoding.UTF8))
                {
                    var coa = reader.ReadToEndAsync().Result;
                    values = LoadCsv(coa);
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
                        account!.ParentAccount = parentAccount;
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
                _financialService.SaveAccountClasses(accountClasses);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("SetupChartOfAccountsAndAccountClasses() encountered an Exception - " + ex.StackTrace);
                throw;
            }

        }

        private void SetupFinancialYear()
        {
            if (_adminService.GetFinancialYears().Count >= 1) return;
            var financialYear = new Core.Domain.Financials.FinancialYear
            {
                FiscalYearCode = "FY1819",
                FiscalYearName = "FY 2018/2019",
                StartDate = new DateTime(2018, 01, 01),
                EndDate = new DateTime(2018, 12, 31),
                IsActive = true
            };
            _financialService.SaveFinancialYear(financialYear);
        }

        private void SetupPaymentTerms()
        {
            IList<Core.Domain.PaymentTerm> paymentTerms = new List<Core.Domain.PaymentTerm>();
            if (_adminService.GetPaymentTerms().Count >= 1) return;

            paymentTerms.Add(new Core.Domain.PaymentTerm
            {
                Description = "Payment due within 10 days",
                PaymentType = Core.Domain.PaymentTypes.AfterNoOfDays,
                DueAfterDays = 10,
                IsActive = true,
            });
            paymentTerms.Add(new Core.Domain.PaymentTerm
            {
                Description = "Due 15th Of the Following Month 	",
                PaymentType = Core.Domain.PaymentTypes.DayInTheFollowingMonth,
                DueAfterDays = 15,
                IsActive = true,
            });
            paymentTerms.Add(new Core.Domain.PaymentTerm
            {
                Description = "Cash Only",
                PaymentType = Core.Domain.PaymentTypes.Cash,
                IsActive = true,
            });

            foreach (var p in paymentTerms)
            {
                _financialService.SavePaymentTerm(p);
            }
        }

        private void SetupLedgerSetting(Core.Domain.Company company)
        {
            Core.Domain.Financials.GeneralLedgerSetting? glSetting = null;
            if (_financialService.GetGeneralLedgerSetting() == null)
            {
                glSetting = new Core.Domain.Financials.GeneralLedgerSetting
                {
                    Company = company,
                    GoodsReceiptNoteClearingAccount = _financialService.GetAccountByAccountCode("10810"),
                    ShippingChargeAccount = _financialService.GetAccountByAccountCode("40500"),
                    SalesDiscountAccount = _financialService.GetAccountByAccountCode("40400"),
                };
            }
            _financialService.SaveGeneralLedgerSetting(glSetting);
        }

        private void SetupTax()
        {
            IList<Core.Domain.TaxSystem.Tax> taxes = new List<Core.Domain.TaxSystem.Tax>();
            if (_financialService.GetTaxes().Any()) return;

            var salesTaxAccount = _financialService.GetAccountByAccountCode("20300");
            var purchaseTaxAccount = _financialService.GetAccountByAccountCode("50700");

            var vat5 = new Core.Domain.TaxSystem.Tax()
            {
                TaxCode = "VAT5%",
                TaxName = "VAT 5%",
                Rate = 5,
                IsActive = true,
                SalesAccountId = salesTaxAccount.Id,
                PurchasingAccountId = purchaseTaxAccount.Id,
            };

            var vat10 = new Core.Domain.TaxSystem.Tax()
            {
                TaxCode = "VAT10%",
                TaxName = "VAT 10%",
                Rate = 10,
                IsActive = true,
                SalesAccountId = salesTaxAccount.Id,
                PurchasingAccountId = purchaseTaxAccount.Id,
            };

            var evat12 = new Core.Domain.TaxSystem.Tax()
            {
                TaxCode = "VAT12%",
                TaxName = "VAT 12%",
                Rate = 12,
                IsActive = true,
                SalesAccountId = salesTaxAccount.Id,
                PurchasingAccountId = purchaseTaxAccount.Id,
            };

            var exportTax1 = new Core.Domain.TaxSystem.Tax()
            {
                TaxCode = "exportTax1%",
                TaxName = "Export Tax 1%",
                Rate = 1,
                IsActive = true,
                SalesAccountId = salesTaxAccount.Id,
                PurchasingAccountId = purchaseTaxAccount.Id,
            };

            var taxGroupVat = new Core.Domain.TaxSystem.TaxGroup()
            {
                Description = "VAT",
                TaxAppliedToShipping = false,
                IsActive = true,
            };

            var taxGroupExport = new Core.Domain.TaxSystem.TaxGroup()
            {
                Description = "Export",
                TaxAppliedToShipping = false,
                IsActive = true,
            };

            IList<Core.Domain.TaxSystem.TaxGroup> taxGroups = new List<Core.Domain.TaxSystem.TaxGroup>();
            taxGroups.Add(taxGroupVat);
            taxGroups.Add(taxGroupExport);

            var itemTaxGroupRegular = new Core.Domain.TaxSystem.ItemTaxGroup()
            {
                Name = "Regular",
                IsFullyExempt = false,
            };

            var itemTaxGroupRegularPreferenced = new Core.Domain.TaxSystem.ItemTaxGroup()
            {
                Name = "Preferenced",
                IsFullyExempt = false,
            };

            IList<Core.Domain.TaxSystem.ItemTaxGroup> itemtaxGroups = new List<Core.Domain.TaxSystem.ItemTaxGroup>();
            itemtaxGroups.Add(itemTaxGroupRegular);
            itemtaxGroups.Add(itemTaxGroupRegularPreferenced);

            vat5.TaxGroupTaxes.Add(new Core.Domain.TaxSystem.TaxGroupTax()
            {
                TaxGroup = taxGroupVat,
            });

            evat12.TaxGroupTaxes.Add(new Core.Domain.TaxSystem.TaxGroupTax()
            {
                TaxGroup = taxGroupVat,
            });

            exportTax1.TaxGroupTaxes.Add(new Core.Domain.TaxSystem.TaxGroupTax()
            {
                TaxGroup = taxGroupExport,
            });

            vat5.ItemTaxGroupTaxes.Add(new Core.Domain.TaxSystem.ItemTaxGroupTax()
            {
                ItemTaxGroup = itemTaxGroupRegularPreferenced,
                IsExempt = false,
            });

            evat12.ItemTaxGroupTaxes.Add(new Core.Domain.TaxSystem.ItemTaxGroupTax()
            {
                ItemTaxGroup = itemTaxGroupRegular,
                IsExempt = false,
            });

            taxes.Add(vat5);
            taxes.Add(vat10);
            taxes.Add(evat12);
            taxes.Add(exportTax1);

            foreach (var tax in taxes)
            {
                _adminService.AddNewTax(tax);
            }
        }

        private void SetupVendor()
        {
            Core.Domain.Purchases.Vendor vendor = new Core.Domain.Purchases.Vendor();
            if (_purchasingService.GetVendors().Any()) return;
            Core.Domain.Party vendorParty = new Core.Domain.Party
            {
                Name = "ABC Sample Supplier",
                PartyType = Core.Domain.PartyTypes.Vendor,
                IsActive = true
            };

            vendor.AccountsPayableAccountId = _financialService.GetAccountByAccountCode("20110").Id;
            vendor.PurchaseAccountId = _financialService.GetAccountByAccountCode("50200").Id;
            vendor.PurchaseDiscountAccountId = _financialService.GetAccountByAccountCode("50400").Id;
            vendor.Party = vendorParty;

            Core.Domain.Contact primaryContact = new Core.Domain.Contact
            {
                ContactType = Core.Domain.ContactTypes.Vendor,
                FirstName = "Mary",
                LastName = "Walter",
                Party = new Core.Domain.Party
                {
                    Name = "Mary Walter",
                    PartyType = Core.Domain.PartyTypes.Contact
                }
            };
            vendor.PrimaryContact = primaryContact;
            _purchasingService.AddVendor(vendor);
        }

        private void SetupCustomer()
        {
            Core.Domain.Sales.Customer customer = new Core.Domain.Sales.Customer();
            if (_salesService.GetCustomers().Any()) return;
            var accountAr = _financialService.GetAccountByAccountCode("10120");
            var accountSales = _financialService.GetAccountByAccountCode("40100");
            var accountAdvances = _financialService.GetAccountByAccountCode("20120");
            var accountSalesDiscount = _financialService.GetAccountByAccountCode("40400");

            Core.Domain.Party customerParty = new Core.Domain.Party();
            customerParty.Name = "ABC Customer";
            customerParty.PartyType = Core.Domain.PartyTypes.Customer;
            customerParty.IsActive = true;

            customer.AccountsReceivableAccountId = accountAr != null ? (int?)accountAr.Id : null;
            customer.SalesAccountId = accountSales != null ? (int?)accountSales.Id : null;
            customer.CustomerAdvancesAccountId = accountAdvances != null ? (int?)accountAdvances.Id : null;
            customer.SalesDiscountAccountId = accountSalesDiscount != null ? (int?)accountSalesDiscount.Id : null;
            customer.TaxGroupId = _financialService.GetTaxGroups().FirstOrDefault(tg => tg.Description == "VAT")!.Id;
            customer.Party = customerParty;

            Core.Domain.Contact primaryContact = new Core.Domain.Contact
            {
                ContactType = Core.Domain.ContactTypes.Customer,
                FirstName = "John",
                LastName = "Doe",
                Party = new Core.Domain.Party
                {
                    Name = "John Doe",
                    PartyType = Core.Domain.PartyTypes.Contact
                }
            };

            customer.PrimaryContact = primaryContact;

            _salesService.AddCustomer(customer);
        }

        private void SetupItems()
        {
            IList<Core.Domain.Items.Measurement> measurements = new List<Core.Domain.Items.Measurement>();
            IList<Core.Domain.Items.ItemCategory> categories = new List<Core.Domain.Items.ItemCategory>();

            if (_inventoryService.GetAllItems().Any()) return;

            measurements.Add(new Core.Domain.Items.Measurement
            {
                Code = "EA",
                Description = "Each"
            });
            measurements.Add(new Core.Domain.Items.Measurement
            {
                Code = "PK",
                Description = "Pack"
            });
            measurements.Add(new Core.Domain.Items.Measurement
            {
                Code = "MO",
                Description = "Monthly"
            });
            measurements.Add(new Core.Domain.Items.Measurement
            {
                Code = "HR",
                Description = "Hour"
            });

            foreach (var m in measurements)
            {
                _inventoryService.SaveMeasurement(m);
            }

            // Accounts = Sales A/C (40100), Inventory (10800), COGS (50300), Inv Adjustment (50500), Item Assm Cost (10900)
            var sales = _financialService.GetAccountByAccountCode("40100");
            var inventory = _financialService.GetAccountByAccountCode("10800");
            var invAdjusment = _financialService.GetAccountByAccountCode("50500");
            var cogs = _financialService.GetAccountByAccountCode("50300");
            var assemblyCost = _financialService.GetAccountByAccountCode("10900");

            var chargesCategory = new Core.Domain.Items.ItemCategory()
            {
                Name = "Charges",
                Measurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "EA"),
                ItemType = Core.Domain.ItemTypes.Charge,
                SalesAccount = sales,
                InventoryAccount = inventory,
                AdjustmentAccount = invAdjusment,
                CostOfGoodsSoldAccount = cogs,
                AssemblyAccount = assemblyCost,
            };

            chargesCategory.Items.Add(new Core.Domain.Items.Item()
            {
                Description = "HOA Dues",
                SellDescription = "HOA Dues",
                PurchaseDescription = "HOA Dues",
                Price = 350,
                SmallestMeasurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "MO"),
                SellMeasurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "MO"),
                SalesAccount = _financialService.GetAccountByAccountCode("40200"),
                No = "0001",
                Code = "1111"
            });

            categories.Add(chargesCategory);

            var componentCategory = new Core.Domain.Items.ItemCategory()
            {
                Name = "Components",
                Measurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "EA"),
                ItemType = Core.Domain.ItemTypes.Purchased,
                SalesAccount = sales,
                InventoryAccount = inventory,
                AdjustmentAccount = invAdjusment,
                CostOfGoodsSoldAccount = cogs,
                AssemblyAccount = assemblyCost,
            };

            var carStickerItem = new Core.Domain.Items.Item()
            {
                Description = "Car Sticker",
                SellDescription = "Car Sticker",
                PurchaseDescription = "Car Sticker",
                Price = 100,
                Cost = 40,
                SmallestMeasurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "EA"),
                SellMeasurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "EA"),
                PurchaseMeasurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "EA"),
                SalesAccount = sales,
                InventoryAccount = inventory,
                CostOfGoodsSoldAccount = cogs,
                InventoryAdjustmentAccount = invAdjusment,
                ItemTaxGroup = _financialService.GetItemTaxGroups().FirstOrDefault(m => m.Name == "Regular"),
                No = "0002",
                Code = "2222"
            };

            var otherItem = new Core.Domain.Items.Item()
            {
                Description = "Optical Mouse",
                SellDescription = "Optical Mouse",
                PurchaseDescription = "Optical Mouse",
                Price = 80,
                Cost = 30,
                SmallestMeasurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "EA"),
                SellMeasurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "EA"),
                PurchaseMeasurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "EA"),
                SalesAccount = sales,
                InventoryAccount = inventory,
                CostOfGoodsSoldAccount = cogs,
                InventoryAdjustmentAccount = invAdjusment,
                ItemTaxGroup = _financialService.GetItemTaxGroups().FirstOrDefault(m => m.Name == "Regular"),
                No = "0003",
                Code = "3333"
            };

            componentCategory.Items.Add(carStickerItem);
            componentCategory.Items.Add(otherItem);

            categories.Add(componentCategory);

            categories.Add(new Core.Domain.Items.ItemCategory()
            {
                Name = "Services",
                Measurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "HR"),
                ItemType = Core.Domain.ItemTypes.Service,
                SalesAccount = sales,
                InventoryAccount = inventory,
                AdjustmentAccount = invAdjusment,
                CostOfGoodsSoldAccount = cogs,
                AssemblyAccount = assemblyCost,
            });

            categories.Add(new Core.Domain.Items.ItemCategory()
            {
                Name = "Systems",
                Measurement = _inventoryService.GetMeasurements().FirstOrDefault(m => m.Code == "EA"),
                ItemType = Core.Domain.ItemTypes.Manufactured,
                SalesAccount = sales,
                InventoryAccount = inventory,
                AdjustmentAccount = invAdjusment,
                CostOfGoodsSoldAccount = cogs,
                AssemblyAccount = assemblyCost,
            });

            foreach (var c in categories)
            {
                _inventoryService.SaveItemCategory(c);
            }
        }

        private void SetupBanks() // Dependency: chart of account
        {
            IList<Core.Domain.Financials.Bank> banks = new List<Core.Domain.Financials.Bank>();

            var bank1 = new Core.Domain.Financials.Bank
            {
                AccountId = _financialService.GetAccountByAccountCode("10111").Id,
                Name = "General Fund",
                Type = Core.Domain.BankTypes.CheckingAccount,
                BankName = "GFB",
                Number = "1234567890",
                Address = "123 Main St.",
                IsDefault = true,
                IsActive = true,
            };
            var bank2 = new Core.Domain.Financials.Bank
            {
                AccountId = _financialService.GetAccountByAccountCode("10113").Id,
                Name = "Petty Cash Account",
                Type = Core.Domain.BankTypes.CashAccount,
                IsDefault = false,
                IsActive = true,
            };

            banks.Add(bank1);
            banks.Add(bank2);

            foreach (var b in banks)
                _financialService.SaveBank(b);
        }

        private void SetupSecurityRoles()
        {
            Console.WriteLine("SetupSecurityRoles() - Started");
            try
            {
                var existingSecurityRoles = _securityService.GetAllSecurityRole();
                // Console.WriteLine(existingSecurityRoles.Count());
                if (existingSecurityRoles.FirstOrDefault(r => r.Name == "SystemAdministrators") == null)
                    _securityService.AddRole("SystemAdministrators");
                if (existingSecurityRoles.FirstOrDefault(r => r.Name == "GeneralUsers") == null)
                    _securityService.AddRole("GeneralUsers");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message + " - " + ex.StackTrace);
                throw;
            }
        }

        private static string[,] LoadCsv(string filename)
        {
            // Get the file's text.
            //string whole_file = System.IO.File.ReadAllText(filename);

            // Split into lines.
            string whole_file = filename.Replace('\n', '\r');
            string[] lines = whole_file.Split(new char[] { '\r' },
                StringSplitOptions.RemoveEmptyEntries);

            // See how many rows and columns there are.
            int num_rows = lines.Length;
            int num_cols = lines[0].Split(',').Length;

            // Allocate the data array.
            string[,] values = new string[num_rows, num_cols];

            // Load the array.
            for (int r = 0; r < num_rows; r++)
            {
                string[] line_r = lines[r].Split(',');
                for (int c = 0; c < num_cols; c++)
                {
                    values[r, c] = line_r[c];
                }
            }

            // Return the values.
            return values;
        }
    }
}