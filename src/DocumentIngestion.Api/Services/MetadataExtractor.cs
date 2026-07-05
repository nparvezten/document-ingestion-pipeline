using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using DocumentIngestion.Api.Models;

namespace DocumentIngestion.Api.Services;

public class MetadataExtractor : IMetadataExtractor
{
    private static readonly Regex DateLabelRegex = new(
        @"(?i)(?:date|invoice\s+date|issue\s+date|date\s+of\s+issue|issued\s+on)[:\-\s]*([0-9a-zA-Z\s,\-/]{6,20})",
        RegexOptions.Compiled);

    private static readonly Regex DateFallbackRegex = new(
        @"\b(\d{4}[-/]\d{2}[-/]\d{2}|\d{2}[-/]\d{2}[-/]\d{4})\b",
        RegexOptions.Compiled);

    private static readonly Regex RefNumberRegex = new(
        @"(?i)(?:invoice\s+number|invoice\s+no|invoice\s+#|inv\s+no|inv#|reference\s+number|ref\s+number|ref\s+no|reference|ref)[:\-\s]*([a-zA-Z0-9\-#]{3,20})",
        RegexOptions.Compiled);

    private static readonly Regex TotalAmountRegex = new(
        @"(?i)(?:total\s+amount|grand\s+total|amount\s+due|total\s+due|total)[:\-\s]*\$?\s*([0-9,]+\.[0-9]{2})\b",
        RegexOptions.Compiled);

    // Matches line items ending with: Quantity (int), Unit Price (decimal), Total Price (decimal)
    // Supports spaces, tabs, and optional leading pipe or table chars
    private static readonly Regex LineItemRowRegex = new(
        @"^\s*\|?\s*(.*?)\s+(\d+)\s+([0-9,]+\.[0-9]{2})\s+([0-9,]+\.[0-9]{2})\s*\|?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ExtractedMetadata ExtractMetadata(string rawText)
    {
        var metadata = new ExtractedMetadata();
        if (string.IsNullOrWhiteSpace(rawText))
            return metadata;

        // Clean raw text (normalize newlines, remove redundant whitespaces)
        string cleanedText = CleanText(rawText);

        // 1. Extract Date
        metadata.DocumentDate = ExtractDate(cleanedText);

        // 2. Extract Reference Number
        metadata.ReferenceNumber = ExtractReferenceNumber(cleanedText);

        // 3. Extract Total Amount
        metadata.TotalAmount = ExtractTotalAmount(cleanedText);

        // 4. Extract Line Items
        metadata.LineItems = ExtractLineItems(cleanedText);

        return metadata;
    }

    private string CleanText(string text)
    {
        // Standardize newlines
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        return text;
    }

    private string? ExtractDate(string text)
    {
        // Try labeled date match first
        var labelMatch = DateLabelRegex.Match(text);
        if (labelMatch.Success)
        {
            string dateStr = labelMatch.Groups[1].Value.Trim();
            if (TryParseAndNormalizeDate(dateStr, out string normalized))
            {
                return normalized;
            }
        }

        // Try direct date pattern search
        var matches = DateFallbackRegex.Matches(text);
        foreach (Match match in matches)
        {
            if (TryParseAndNormalizeDate(match.Value, out string normalized))
            {
                return normalized;
            }
        }

        return null;
    }

    private bool TryParseAndNormalizeDate(string input, out string normalized)
    {
        normalized = string.Empty;
        string[] formats = {
            "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd", "dd-MM-yyyy", "MM-dd-yyyy",
            "d MMM yyyy", "dd MMM yyyy", "MMMM d, yyyy", "MMMM dd, yyyy"
        };

        // Clean input of brackets or trailing garbage
        string cleaned = input.Trim(',', '.', '(', ')', '[', ']');

        if (DateTime.TryParse(cleaned, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            normalized = date.ToString("yyyy-MM-dd");
            return true;
        }

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(cleaned, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                normalized = date.ToString("yyyy-MM-dd");
                return true;
            }
        }

        return false;
    }

    private string? ExtractReferenceNumber(string text)
    {
        var match = RefNumberRegex.Match(text);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Fallback: look for INV-XXXX pattern
        var invPattern = Regex.Match(text, @"\b(INV-\d+)\b", RegexOptions.IgnoreCase);
        if (invPattern.Success)
        {
            return invPattern.Groups[1].Value.Trim();
        }

        return null;
    }

    private double? ExtractTotalAmount(string text)
    {
        var matches = TotalAmountRegex.Matches(text);
        double? highestTotal = null;

        // Iterate through all total matches (often sub-totals or totals appear multiple times)
        // Find the maximum value that matches the "total" label to avoid matching "sub-total" if it is lower.
        foreach (Match match in matches)
        {
            string amountStr = match.Groups[1].Value.Replace(",", "");
            if (double.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
            {
                if (highestTotal == null || parsedValue > highestTotal)
                {
                    highestTotal = parsedValue;
                }
            }
        }

        return highestTotal;
    }

    private List<ExtractedLineItem> ExtractLineItems(string text)
    {
        var items = new List<ExtractedLineItem>();
        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            // Skip lines containing totals to avoid extracting subtotal/total rows as line items
            if (line.Contains("total", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("subtotal", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("tax", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("due", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = LineItemRowRegex.Match(line);
            if (match.Success)
            {
                string desc = match.Groups[1].Value.Trim();
                
                // Clean layout symbols (e.g. divider dots, dashes, pipes)
                desc = desc.Trim('-', '.', '_', ':', '|', '\t', ' ');

                // Skip header indicator lines (e.g. "Description Quantity Price")
                if (desc.Equals("description", StringComparison.OrdinalIgnoreCase) || 
                    desc.Equals("item", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(desc) || 
                    desc.Contains("---") || 
                    desc.Contains("==="))
                {
                    continue;
                }

                string qtyStr = match.Groups[2].Value;
                string unitPriceStr = match.Groups[3].Value.Replace(",", "");
                string totalPriceStr = match.Groups[4].Value.Replace(",", "");

                if (int.TryParse(qtyStr, out int qty) &&
                    double.TryParse(unitPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double unitPrice) &&
                    double.TryParse(totalPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double totalPrice))
                {
                    items.Add(new ExtractedLineItem
                    {
                        Description = desc,
                        Quantity = qty,
                        UnitPrice = unitPrice,
                        TotalPrice = totalPrice
                    });
                }
            }
        }

        return items;
    }
}
