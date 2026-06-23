# AGENTS.md

## Project Overview

This repository contains the Windows desktop application named **Yönetim Finansal İşlem Takip Sistemi**.

Purpose:
Track internal financial transactions, user actions, cash balances, permissions, reporting, audit logs, exchange rates, and update flow for a small business environment.

Primary stack:
- C#
- .NET 9
- WPF
- PostgreSQL
- Entity Framework Core
- Npgsql
- ClickOnce
- Git / GitHub

## Architecture Rules

Follow layered architecture.

Projects must be organized under:
- UI
- Application
- Domain
- Infrastructure

Rules:
- Do not put SQL queries in the UI layer.
- Do not mix business logic into XAML code-behind unless it is purely UI-specific.
- Keep domain models clean and focused on business meaning.
- Infrastructure handles database, audit, update, and external integrations.

See:
- `docs/architecture.md`
- `docs/database.md`

## Business Rules

Core rules:
- PostgreSQL is centralized and shared by all users.
- Users are created manually by an administrator.
- Authorization is user-based.
- Supported transaction types in V1:
  - Tahsilat
  - Ödeme
  - Avans
  - Özel Harcama
  - Transfer
- Cash balances are tracked separately by currency:
  - TL
  - USD
  - EUR
- Users do not enter exchange rates during transaction entry.
- Exchange rates will later be fetched from TCMB.

See:
- `docs/database.md`
- `docs/roadmap.md`

## Audit Rules

Audit logging is mandatory for critical actions.

Audit records should capture:
- who did it
- what was done
- when it was done
- from which computer it was done
- old value
- new value

Examples:
- TransactionCreated
- TransactionUpdated
- TransactionDeleted
- UserCreated
- PermissionUpdated

See:
- `docs/audit-log.md`

## UI Rules

Use a custom dialog system instead of default MessageBox for business flows.

Dialog types:
- Info
- Success
- Warning
- Error
- Question

Each dialog should include:
- title
- icon
- color
- message
- action buttons when needed

See:
- `docs/dialog-system.md`

## Update Rules

Use ClickOnce for V1 deployment and update flow.

Requirements:
- Check updates on app startup.
- Provide a manual “Check for Updates” action.
- Ask user confirmation before installing an available update.

See:
- `docs/update-flow.md`

## Coding Rules

Before major implementation, present a short plan.

For changes affecting multiple files:
1. Explain the approach.
2. List the files to change.
3. Then implement.

Comments:
- Write clear and short comments.
- Prefer comments that explain why, not obvious what.
- Add comments especially for:
  - business rules
  - audit triggers
  - balance calculations
  - update flow
  - dialog decisions
  - database access boundaries

Do not:
- add unnecessary comments to trivial code
- introduce large architectural changes without warning
- silently rename important folders, files, or services

## Workflow

When starting a task:
1. Read `README.md`
2. Read `AGENTS.md`
3. Check related files under `docs/`
4. Inspect the affected code before editing
5. Propose a plan first for non-trivial tasks

If project rules and implementation conflict, flag the conflict before coding.