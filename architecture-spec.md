# Specification: Extensible Document Ingestion & Metadata Extraction Pipeline

## 1. Project Objective
Build a lightweight, highly efficient backend pipeline that ingests messy, unstructured files (PDFs, Images/Receipts, DOCX, TXT), strips layout overhead, extracts specific structural metadata (Dates, Reference IDs, Financial Tables), and flattens the output into minified JSON and a clean database schema.

## 2. Technical Design & Strategy Pattern
The application must use a strict **Strategy Pattern** to handle file routing. The ingestion engine must inspect the file's MIME type and invoke the correct extraction strategy dynamically without bloating memory.

### File Strategy Matrix
*   **PDF Strategy:** Read streams using a streaming library. Avoid loading the entire binary into memory.
*   **Image/Receipt Strategy:** Route through a lightweight open-source OCR engine to extract raw text coordinates.
*   **DOCX Strategy:** Process XML structural document nodes to parse text and inline tables.
*   **TXT/CSV Strategy:** Direct stream reader parsing clean string buffers.

## 3. Data Flow & Core Architecture
1. **Ingress:** Multi-part file upload endpoint accepting a raw file stream.
2. **Detection & Strategy Route:** File validation checks file headers (not just extensions) and allocates the job to the dedicated parser strategy.
3. **Extraction & Minification:** 
    *   Text chunks are stripped of styling, margins, fonts, and rendering layout bloat.
    *   Regex and structural heuristics isolate Key-Value metadata fields: `document_date`, `reference_number`, `total_amount`, and `line_items`.
4. **Data Normalization:** Maps the raw strings into strongly-typed objects matching our target schema.
5. **Egress:** Simultaneously flattens the data into a minified `.json` layout on disk and updates a lightweight SQLite database instance.

## 4. Database Schema (SQLite)
Maintain a lean relational schema containing two central tables:

### Table: `documents`
*   `id` (TEXT, Primary Key, UUID)
*   `file_name` (TEXT)
*   `file_type` (TEXT)
*   `extracted_date` (TEXT/DATE)
*   `reference_number` (TEXT, Indexed)
*   `total_amount` (REAL)
*   `processed_at` (TIMESTAMP)

### Table: `document_line_items`
*   `id` (INTEGER, Primary Key, Autoincrement)
*   `document_id` (TEXT, Foreign Key -> documents.id)
*   `description` (TEXT)
*   `quantity` (INTEGER)
*   `unit_price` (REAL)
*   `total_price` (REAL)

## 5. Implementation Rules for the AI Agent
*   **Language & Stack:** Use C# (.NET Core) for Backend and Angular for frontend if necessary. Keep external dependencies secure, free and open-source and use microsoft related tools if possible.
*   **Memory Efficiency:** All file access operations must be implemented via streams/chunks. Processing a 50MB file must not spike system memory allocations.
*   **Robust Error Isolation:** A single malformed file or a corrupted PDF structure must catch gracefully, logging the failure to a processing ledger without crashing the server process.
*   **Test Environment Setup:** Provide automated mocked payloads or test files (e.g., sample text invoices, mock PDFs) inside a `/tests` folder to verify parsing pipelines immediately.