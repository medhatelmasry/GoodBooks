namespace AccountGoWeb.Models.Financial;


public class JournalEntryViewModel
{
    public DateTime JournalDate { get; set; }
    public string? Memo { get; set; }
    public string? ReferenceNo { get; set; }
    public List<JournalEntryLineViewModel> JournalEntryLines { get; set; } = new();
}

public class JournalEntryLineViewModel
{
    public int? AccountId { get; set; }
    public decimal? Amount { get; set; }
    public int DrCr { get; set; } // 0 = Debit, 1 = Credit
    public string? Memo { get; set; }
}
