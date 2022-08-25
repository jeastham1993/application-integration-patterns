namespace VendorLoanQuoteGenerator.DataTransfer;

public class LoanQuoteResult
{
    public GenerateLoanQuoteRequest Request { get; set; }

    public string VendorName { get; set; }

    public double InterestRate { get; set; }
}