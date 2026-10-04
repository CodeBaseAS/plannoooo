namespace planno_API.Services
{
    public class InvoiceFileStore(IConfiguration configuration, IWebHostEnvironment env)
    {
        private string Directory_ => configuration["Invoices:Directory"] ?? Path.Combine(env.ContentRootPath, "App_Data", "invoices");

        public string PathFor(string invoiceNumber) => Path.Combine(Directory_, invoiceNumber + ".pdf");

        public async Task SaveAsync(string invoiceNumber, byte[] bytes)
        {
            Directory.CreateDirectory(Directory_);
            await File.WriteAllBytesAsync(PathFor(invoiceNumber), bytes);
        }

        public void Delete(string invoiceNumber)
        {
            string path = PathFor(invoiceNumber);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}