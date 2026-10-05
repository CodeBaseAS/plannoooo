namespace Models.Models

{
    public class Invoice
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        public Payment Payment { get; set; } = null!;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string PdfUrl { get; set; } = string.Empty;
    }
}
