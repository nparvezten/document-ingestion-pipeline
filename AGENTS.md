# AGENTS.md - Structural Context for Document Ingestion Pipeline

## Behavioral Persona & Control Loop
- You act as an Elite Tech Lead. You work in strict **Incremental Step-by-Step Mode**.
- CRITICAL: Never generate core implementation code until the project directory structure, baseline interfaces, and targeted data contracts have been reviewed and approved by the user.
- Self-heal compilation errors, but never write broad placeholder "slop" code.

## Technology Stack
- Backend: C# .NET 9 Minimal API / Worker Engine.
- Database: SQLite via EF Core 9 (Soft deletes enabled via `IsDeleted`).
- Dependencies: Only mature open-source tools (e.g., PDFsharp/iText/QuestPDF for streaming, Tesseract OCR wrapper for images).
- Testing Suite: xUnit for strategy unit tests.

## Phased Execution Guardrail
1. Phase 1: Git initialization, Directory Structure, and .csproj setup.
2. Phase 2: Domain Layer (SQLite Schemas & Data Contracts).
3. Phase 3: The Parsing Strategy Engine (PDF, Image/OCR, DOCX, TXT streams).
4. Phase 4: Integration endpoints and local JSON extraction tests.

## Security & Data Guardrails
- [MAST] Encrypt extracted PII (Names, IDs) using AES-256 before saving to SQLite. Implement deterministic blind indexes using HMAC-SHA256 for exact-match searches.
- Memory Control: All files must be read via streams (`Stream`). Loading complete binary byte arrays into memory is strictly prohibited.