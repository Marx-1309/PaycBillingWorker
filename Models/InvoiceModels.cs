using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PaycBillingWorker.Models
{
    // Represents a single row from the SQL View
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

        // Line Item columns
        public string SerialNo { get; set; }
        public string Description { get; set; }
        public decimal UnitRate { get; set; }
        public decimal Quantity { get; set; }
        public decimal Nett { get; set; }
        public decimal Vat { get; set; }
        public decimal Amount { get; set; }
    }

    // API Payload - Properties match the JSON casing required by your API docs
    public class InvoicePayload
    {
        [JsonPropertyName("reference_key")]
        public string ReferenceKey { get; set; }

        [JsonPropertyName("reference_type")]
        public string ReferenceType { get; set; }

        [JsonPropertyName("invoice_no")]
        public string InvoiceNo { get; set; }

        [JsonPropertyName("invoice_date")]
        public string InvoiceDate { get; set; }

        [JsonPropertyName("invoice_from_date")]
        public string InvoiceFromDate { get; set; }

        [JsonPropertyName("invoice_to_date")]
        public string InvoiceToDate { get; set; }

        [JsonPropertyName("opening_reading")]
        public decimal? OpeningReading { get; set; }

        [JsonPropertyName("closing_reading")]
        public decimal? ClosingReading { get; set; }

        [JsonPropertyName("opening_balance")]
        public decimal OpeningBalance { get; set; }

        [JsonPropertyName("this_period_charges")]
        public decimal ThisPeriodCharges { get; set; }

        [JsonPropertyName("total_payable")]
        public decimal TotalPayable { get; set; }

        [JsonPropertyName("minimum_payable")]
        public decimal MinimumPayable { get; set; }

        [JsonPropertyName("pay_by_date")]
        public string PayByDate { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }

        [JsonPropertyName("special_comment")]
        public string SpecialComment { get; set; }

        [JsonPropertyName("line_items")]
        public List<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    }

    public class InvoiceLineItem
    {
        [JsonPropertyName("serial_no")]
        public string SerialNo { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("unit_rate")]
        public decimal UnitRate { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("nett")]
        public decimal Nett { get; set; }

        [JsonPropertyName("vat")]
        public decimal Vat { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }
}