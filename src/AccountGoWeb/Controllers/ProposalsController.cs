﻿using AutoMapper;
using Dto.Sales;
using Microsoft.AspNetCore.Mvc;

namespace AccountGoWeb.Controllers
{
    public class ProposalsController : GoodController
    {
        private readonly ILogger<ProposalsController> _logger;
        private readonly IMapper _mapper;

        public ProposalsController(IConfiguration config, ILogger<ProposalsController> logger, IMapper mapper)
        {
            _configuration = config;
            _logger = logger;
            _mapper = mapper;
        }

        public IActionResult Index()
        {
            return RedirectToAction("proposals");
        }

        public async System.Threading.Tasks.Task<IActionResult> Proposals()
        {
            ViewBag.PageContentHeader = "Sales Proposals";

            using (var client = new HttpClient())
            {
                var baseUri = _configuration!["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();

                var response = await client.GetAsync(baseUri + "sales/GetSalesProposals");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }
            }

            return View();
        }

        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> AddSalesProposal()
        {
            ViewBag.PageContentHeader = "Add Sales Proposal";

            SalesProposalForCreation salesProposalModel = new SalesProposalForCreation();
            salesProposalModel.SalesProposalLines = new List<SalesProposalLineForCreation>
            {
                new SalesProposalLineForCreation
                {
                    Amount = 0,
                    Discount = 0,
                    ItemId = 1,
                    Quantity = 1,
                }
            };
            // TODO: Replace with system generated numbering.
            salesProposalModel.No = new System.Random().Next(1, 99999).ToString();

            @ViewBag.Customers = Models.SelectListItemHelper.Customers();
            @ViewBag.Items = Models.SelectListItemHelper.Items();
            @ViewBag.Measurements = Models.SelectListItemHelper.Measurements();
            @ViewBag.PaymentTerms = Models.SelectListItemHelper.PaymentTerms();

            return View(salesProposalModel);
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> AddSalesProposal(SalesProposalForCreation salesProposalForCreationDto, string? addRowBtn)
        {
            if (!string.IsNullOrEmpty(addRowBtn))
            {
                salesProposalForCreationDto.SalesProposalLines!.Add(new SalesProposalLineForCreation
                {
                    Amount = 0,
                    Quantity = 1,
                    Discount = 0,
                    ItemId = 1,
                    MeasurementId = 1,
                });

                ViewBag.Customers = Models.SelectListItemHelper.Customers();
                ViewBag.Items = Models.SelectListItemHelper.Items();
                ViewBag.Measurements = Models.SelectListItemHelper.Measurements();
                ViewBag.PaymentTerms = Models.SelectListItemHelper.PaymentTerms();
         
                return View(salesProposalForCreationDto);
            }
            else if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(salesProposalForCreationDto);
                var content = new StringContent(serialize);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                _logger.LogInformation("AddSalesProposal: " + await content.ReadAsStringAsync());
                var response = Post("Sales/AddSalesProposal", content);
                _logger.LogInformation("AddSalesProposal response: " + response.ToString());

                if (response.IsSuccessStatusCode)
                    return RedirectToAction("Proposals");
            }

            ViewBag.Customers = Models.SelectListItemHelper.Customers();
            ViewBag.Items = Models.SelectListItemHelper.Items();
            ViewBag.PaymentTerms = Models.SelectListItemHelper.PaymentTerms();
            ViewBag.Measurements = Models.SelectListItemHelper.Measurements();

            return View(salesProposalForCreationDto);
        }

        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> EditSalesProposal(int id)
        {
            ViewBag.PageContentHeader = "Edit Sales Proposal";
            SalesProposal? salesProposalModel = null;

            salesProposalModel = GetAsync<SalesProposal>("Sales/GetSalesProposalById?id=" + id).Result;

            if(salesProposalModel is null)
            {
                // TODO : Alerts and Error Handling
                throw new NotImplementedException();
            }

            @ViewBag.Customers = Models.SelectListItemHelper.Customers();
            @ViewBag.PaymentTerms = Models.SelectListItemHelper.PaymentTerms();
            @ViewBag.Items = Models.SelectListItemHelper.Items();
            @ViewBag.Measurements = Models.SelectListItemHelper.Measurements();

            SalesProposalForUpdate salesProposalForUpdateModel = _mapper.Map<SalesProposalForUpdate>(salesProposalModel);

            return View(salesProposalForUpdateModel);
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> EditSalesProposal(SalesProposalForUpdate salesProposalForUpdate, string? addRowBtn)
        {
            if (!string.IsNullOrEmpty(addRowBtn))
            {
                salesProposalForUpdate.SalesProposalLines!.Add(new SalesProposalLineForUpdate
                {
                    Amount = 0,
                    Quantity = 1,
                    Discount = 0,
                    ItemId = 1,
                    MeasurementId = 1,
                });

                ViewBag.Customers = Models.SelectListItemHelper.Customers();
                ViewBag.Items = Models.SelectListItemHelper.Items();
                ViewBag.Measurements = Models.SelectListItemHelper.Measurements();
                ViewBag.PaymentTerms = Models.SelectListItemHelper.PaymentTerms();

                return View(salesProposalForUpdate);
            }
            else if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(salesProposalForUpdate);
                var content = new StringContent(serialize);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = Post("Sales/UpdateSalesProposal", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Proposals");
                }
                else
                {
                    // TODO : Alerts and Error Handling
                    throw new NotImplementedException();
                }
            }

            ViewBag.Customers = Models.SelectListItemHelper.Customers();
            ViewBag.PaymentTerms = Models.SelectListItemHelper.PaymentTerms();
            ViewBag.Items = Models.SelectListItemHelper.Items();
            ViewBag.Measurements = Models.SelectListItemHelper.Measurements();
            
            return View(salesProposalForUpdate);
        }

        public async System.Threading.Tasks.Task<IActionResult> ViewSalesProposal(int id)
        {
            ViewBag.PageContentHeader = "View Sales Proposal";
            SalesProposal? salesProposalModel = null;

            salesProposalModel = GetAsync<SalesProposal>("Sales/GetSalesProposalById?id=" + id).Result;

            if (salesProposalModel is null)
            {
                // TODO : Alerts and Error Handling
                throw new NotImplementedException();
            }

            var customers = Models.SelectListItemHelper.Customers();
            var paymentTerms = Models.SelectListItemHelper.PaymentTerms();
            var items = Models.SelectListItemHelper.Items();
            var measurements = Models.SelectListItemHelper.Measurements();

            @ViewBag.Customer = customers.FirstOrDefault(c => c.Value == salesProposalModel.CustomerId.ToString()).Text;
            @ViewBag.PaymentTerm = paymentTerms.FirstOrDefault(c => c.Value == salesProposalModel.PaymentTermId.ToString()).Text;
            @ViewBag.Item = items.FirstOrDefault(c => c.Value == salesProposalModel.SalesProposalLines.FirstOrDefault().ItemId.ToString()).Text;
            @ViewBag.Measurement = measurements.FirstOrDefault(c => c.Value == salesProposalModel.SalesProposalLines.FirstOrDefault().MeasurementId.ToString()).Text;

            return View(salesProposalModel);
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> ViewSalesProposal(SalesProposalForUpdate salesProposalForUpdate)
        {
            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(salesProposalForUpdate);
                var content = new StringContent(serialize);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = Post("Sales/UpdateSalesProposal", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Proposals");
                }
                else
                {
                    // TODO : Alerts and Error Handling
                    throw new NotImplementedException();
                }
            }

            ViewBag.Customers = Models.SelectListItemHelper.Customers();
            ViewBag.PaymentTerms = Models.SelectListItemHelper.PaymentTerms();
            ViewBag.Items = Models.SelectListItemHelper.Items();
            ViewBag.Measurements = Models.SelectListItemHelper.Measurements();

            return View(salesProposalForUpdate.Id);
        }

        public async System.Threading.Tasks.Task<IActionResult> DeleteSalesProposal(int id)
        {
            using (var client = new HttpClient())
            {
                var baseUri = _configuration!["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.DeleteAsync(baseUri + "Sales/DeleteSalesProposal?id=" + id);

                if(response.IsSuccessStatusCode)
                    return RedirectToAction("Proposals");
                else
                {
                    // TODO : Alerts and Error Handling
                    throw new NotImplementedException();
                }
            }

            return RedirectToAction("Proposals");
        }
    }
}