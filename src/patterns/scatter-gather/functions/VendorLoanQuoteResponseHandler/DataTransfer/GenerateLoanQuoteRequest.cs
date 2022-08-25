namespace VendorLoanQuoteResponseHandler.DataTransfer;

public class GenerateLoanQuoteRequest
{
    public string CustomerId { get; set; }

    public string CorrelationId { get; set; }

    public decimal LoanAmount { get; set; }
}