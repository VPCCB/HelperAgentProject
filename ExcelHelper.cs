using System;
using ClosedXML.Excel;

namespace HelperMAS
{
    public static class ExcelHelper
    {
        // Update the status of a signer in the Excel file.
        public static void UpdateSignerStatus(string excelFilePath, string signerName, string newStatus)
        {
            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheet(1);
                bool found = false;
                foreach (var row in worksheet.RowsUsed())
                {
                    string cellName = row.Cell("A").GetString();
                    if (cellName.Equals(signerName, StringComparison.OrdinalIgnoreCase))
                    {
                        row.Cell("B").Value = newStatus;
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    workbook.Save();
                    Console.WriteLine($"Status updated for {signerName}: {newStatus}");
                }
                else
                {
                    Console.WriteLine($"Signer '{signerName}' not found in the Excel file.");
                }
            }
        }

        // Add a new signer to the Excel file.
        public static void AddSigner(string excelFilePath, string signerName)
        {
            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheet(1);
                // Determine the next available row (assuming header is in row 1).
                int rowNumber = worksheet.LastRowUsed()?.RowNumber() + 1 ?? 2;
                worksheet.Cell(rowNumber, 1).Value = signerName;  // Column A: Signer Name
                worksheet.Cell(rowNumber, 2).Value = "active";      // Column B: Status
                workbook.Save();
                Console.WriteLine($"Signer '{signerName}' added with status 'active'.");
            }
        }
    }
}
