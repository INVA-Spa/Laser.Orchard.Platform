﻿using Laser.Orchard.Braintree.Models;
using Laser.Orchard.Braintree.Services;
using Laser.Orchard.Braintree.ViewModels;
using Laser.Orchard.PaymentGateway;
using Laser.Orchard.PaymentGateway.Models;
using Newtonsoft;
using Newtonsoft.Json;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.Logging;
using Orchard.Themes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Laser.Orchard.Braintree.Controllers {
    public class BraintreeController : Controller {
        private readonly IOrchardServices _orchardServices;
        private readonly BraintreePosService _posService;
        private readonly IBraintreeService _braintreeService;
        private readonly IEnumerable<IBraintreePaymentInformationProvider> _paymentInfoProviders;

        public BraintreeController(
            IOrchardServices orchardServices, 
            IRepository<PaymentRecord> repository, 
            IPaymentEventHandler paymentEventHandler, 
            IBraintreeService braintreeService,
            IEnumerable<IBraintreePaymentInformationProvider> paymentInfoProviders) {

            _orchardServices = orchardServices;
            _posService = new BraintreePosService(orchardServices, repository, paymentEventHandler);
            _braintreeService = braintreeService;
            _paymentInfoProviders = paymentInfoProviders;

            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pid">Payment ID</param>
        /// <returns></returns>
        [Themed]
        public ActionResult Index(int pid = 0, string guid = "") {
            PaymentRecord payment;
            if (pid > 0) {
                payment = _posService.GetPaymentInfo(pid);
            } else {
                payment = _posService.GetPaymentInfo(guid);
            }
            pid = payment.Id;
            var settings = _orchardServices.WorkContext.CurrentSite.As<BraintreeSiteSettingsPart>();
            if (settings.CurrencyCode != payment.Currency) {
                //throw new Exception(string.Format("Invalid currency code. Valid currency is {0}.", settings.CurrencyCode));
                string error = string.Format("Invalid currency code. Valid currency is {0}.", settings.CurrencyCode);
                _posService.EndPayment(payment.Id, false, error, error);
                return Redirect(_posService.GetPaymentInfoUrl(payment.Id));
            }
            PaymentVM model = new PaymentVM();
            model.Record = payment;
            model.TenantBaseUrl = Url.Action("Index").Replace("/Laser.Orchard.Braintree/Braintree", "");
            // Allow manipulation of PaymentVM by services to add information into it
            // This has become required to make sure out integration complies with 3DS2
            // because we need to forward more info to the shape, and not all possible 
            // information is always available (besides it not being available here)
            // https://developers.braintreepayments.com/guides/3d-secure/client-side/javascript/v3
            foreach(var pip in _paymentInfoProviders) {
                model = pip.AddInformation(model);
            }
            return View("Index", model);
        }

        /// <summary>
        /// Entry point per dispositivi mobile per iniziare il pagamento tramite Braintree.
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0)]
        public ActionResult GetTokenAndPid(string reason, decimal amount, string currency, int itemId = 0) {
            var clientToken = _braintreeService.GetClientToken();
            var payment = new PaymentRecord {
                Reason = reason,
                Amount = amount,
                Currency = currency,
                ContentItemId = itemId
            };
            payment = _posService.StartPayment(payment);
            var result = new { Token = clientToken, Pid = payment.Id };
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        private PaymentResult FinalizePayment(string nonce, string sPid) {
            int pid = int.Parse(sPid);
            var paymentInfo = _posService.GetPaymentInfo(pid);
            var payResult = _braintreeService.Pay(new PaymentContext(nonce, paymentInfo));
            string error = "";
            string transactionId = "";
            string info = JsonConvert.SerializeObject(payResult);
            if (payResult.Success == false) {
                error = payResult.ResponseText;
                Logger.Error(string.Format(@"Error on payment for order {0}: Error {1}. Details: {2}",
                    paymentInfo.ContentItemId,
                    error,
                    info));
            }
            // even failed transactions may be recorded
            transactionId = payResult?.TransactionId ?? "";
            _posService.EndPayment(pid, payResult.Success, error, info, transactionId);
            return new PaymentResult { Pid = pid, Success = payResult.Success, Error = error, TransactionId = transactionId};
        }
        [HttpPost]
        public ActionResult PayMobile(MobilePay model) {
            string nonce = model.payment_method_nonce;
            string sPid = model.pid;
            var outcome = FinalizePayment(nonce, sPid);
            return Json(outcome);
        }
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0)]
        public ActionResult GetToken() {
            var clientToken = _braintreeService.GetClientToken();
            return Content(clientToken, "text/plain", Encoding.UTF8);
        }

        [Themed]
        [HttpPost]
        public ActionResult Pay() {
            string nonce = Request["payment_method_nonce"];
            string sPid = Request["pid"];
            var outcome = FinalizePayment(nonce, sPid);
            return Redirect(_posService.GetPaymentInfoUrl(outcome.Pid));
        }

        public class MobilePay {
            public string payment_method_nonce { get; set; }
            public string pid { get; set; }
        }

        private class PaymentResult {
            public int Pid { get; set; }
            public bool Success { get; set; }
            public string Error { get; set; }
            public string TransactionId { get; set; }
        }
    }
}