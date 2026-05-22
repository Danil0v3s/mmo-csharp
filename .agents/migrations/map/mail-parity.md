# mail.cpp parity · 2026-05-22 (T9.G — per-fn rollup; T7.6 inbox close-out)

`src/map/mail.cpp` (535 lines, 10 functions) — map-side mail
composition (draft / attach / send). Persistence + cross-server
delivery already live in the char-server `MailSendAsync` /
`MailReceiveAsync` RPCs.

Canonical entry points: [IMailService](/Map.Server/Mail/IMailService.cs) /
[MailService](/Map.Server/Mail/MailService.cs).

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `mail_openmail` | ✅ | `IMailService.OpenMail` (inbox display) |
| `mail_removeitem` | ⚠️ | `RemoveItem` — draft state pending |
| `mail_removezeny` | ⚠️ | `RemoveZeny` — draft state pending |
| `mail_setitem` | ⚠️ | `SetItem` — draft state pending |
| `mail_setattachment` | ⚠️ | `SetAttachment` — draft state pending |
| `mail_send` | ✅ | T7.6 — forwards via `ICharServerIpcServiceMail.MailSendAsync` |
| `mail_retrieve_attachment` | ✅ | T7.6 — `MailReceiveAsync` |
| `mail_deleteattach` | ✅ | T7.6 — char-side delete |
| `mail_clean_attach` | ⚠️ | Implicit on send/cancel |
| `do_init_mail` / `do_final_mail` | ✅ | DI lifecycle |

Per-session draft state (item attachments + zeny payload) is the
gap on 4 of the 10 — pending PlayerEntity mail session model.

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Mail open / send / retrieve / lifecycle | 6 | 4 | 0 | 10 |
| **Totals** | **6** | **4** | **0** | **10** |

## History

### 2026-05-22 — T9.G per-fn rollup

Per-function audit. Baseline: **6 ✅ / 4 ⚠️ / 0 ❌**. Open / send /
retrieve / delete-attach / DI lifecycle all ✅ (T7.6 wave wired
the char-server IPC). 4 ⚠️ are draft session state (setitem /
setzeny / removeitem / removezeny / setattachment) — pending
per-session draft model on PlayerEntity.

### 2026-05-20 — initial audit + service
- 10 functions covered. Draft session + IPC dispatch data-pending.
