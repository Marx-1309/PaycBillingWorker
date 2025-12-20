using System;
using System.Collections.Generic;
using Newtonsoft.Json; 

namespace PaycBillingWorker.Models
{
    // DB Row Model
    public class InvoiceSourceRow
    {
        public string ReferenceKey { get; set; }
        public string ReferenceType { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime InvoiceFromDate { get; set; }
        public DateTime InvoiceToDate { get; set; }
        public decimal? OpeningReading { get; set; }
        public decimal? ClosingReading { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ThisPeriodCharges { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal MinimumPayable { get; set; }
        public DateTime PayByDate { get; set; }
        public string Comment { get; set; }
        public string SpecialComment { get; set; }
        public string SerialNo { get; set; }
        public string Description { get; set; }
        public decimal UnitRate { get; set; }
        public decimal Quantity { get; set; }
        public decimal Nett { get; set; }
        public decimal Vat { get; set; }
        public decimal Amount { get; set; }
    }

    // API Payload Model
    public class InvoicePayload
    {
        [JsonProperty("reference_key")]
        public string ReferenceKey { get; set; }

        [JsonProperty("reference_type")]
        public string ReferenceType { get; set; }

        [JsonProperty("invoice_no")]
        public string InvoiceNo { get; set; }

        [JsonProperty("invoice_date")]
        public string InvoiceDate { get; set; }

        [JsonProperty("invoice_from_date")]
        public string InvoiceFromDate { get; set; }

        [JsonProperty("invoice_to_date")]
        public string InvoiceToDate { get; set; }

        [JsonProperty("opening_reading")]
        public decimal? OpeningReading { get; set; }

        [JsonProperty("closing_reading")]
        public decimal? ClosingReading { get; set; }

        [JsonProperty("opening_balance")]
        public decimal OpeningBalance { get; set; }

        [JsonProperty("this_period_charges")]
        public decimal ThisPeriodCharges { get; set; }

        [JsonProperty("total_payable")]
        public decimal TotalPayable { get; set; }

        [JsonProperty("minimum_payable")]
        public decimal MinimumPayable { get; set; }

        [JsonProperty("pay_by_date")]
        public string PayByDate { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; } = string.Empty;

        [JsonProperty("special_comment")]
        public string SpecialComment { get; set; } = string.Empty;

        [JsonProperty("line_items")]
        public List<InvoiceLineItem> LineItems { get; set; } = new();
    }

    public class InvoiceLineItem
    {
        [JsonProperty("serial_no")]
        public string SerialNo { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("unit_rate")]
        public decimal UnitRate { get; set; }

        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("nett")]
        public decimal Nett { get; set; }

        [JsonProperty("vat")]
        public decimal Vat { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }
    }
}