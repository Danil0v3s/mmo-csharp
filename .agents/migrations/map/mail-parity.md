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
| `mail_removeitem` | ✅ | AT-D2 — `MailService.RemoveItem` mutates `PlayerEntity.MailDraftItems` |
| `mail_removezeny` | ✅ | AT-D2 — `MailService.RemoveZeny` clamps against MAX_ZENY |
| `mail_setitem` | ✅ | AT-D2 — `MailService.SetAttachment` (rAthena alias; draft items map) |
| `mail_setattachment` | ✅ | AT-D2 — `MailService.SetAttachment` writes to `MailDraftItems` |
| `mail_send` | ✅ | T7.6 — forwards via `ICharServerIpcServiceMail.MailSendAsync` |
| `mail_retrieve_attachment` | ✅ | T7.6 — `MailReceiveAsync` |
| `mail_deleteattach` | ✅ | T7.6 — char-side delete |
| `mail_clean_attach` | ✅ | AT-D2 — `MailService.Clear` wipes draft items/zeny (called from Send + InvalidOperation + DeliveryFail) |
| `do_init_mail` / `do_final_mail` | ✅ | DI lifecycle |

Draft state landed via AT-D2 wave (PlayerEntity.MailDraftItems +
MailDraftZeny + MailOpened flag). Mail parity complete on the public
map-side surface.

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Mail open / send / retrieve / lifecycle | 10 | 0 | 0 | 10 |
| **Totals** | **10** | **0** | **0** | **10** |

## History

### 2026-05-24 — P2.1 doc-resync close-out (5 stale ⚠️ → ✅; 0 genuine gaps remain)

All 5 draft-state ⚠️ rows flipped to ✅ — AT-D2 wave landed
`PlayerEntity.MailDraftItems` + `MailDraftZeny` + `MailOpened`; `MailService`
has real `SetAttachment` / `RemoveItem` / `RemoveZeny` / `Clear` bodies that
mutate this state. `mail_clean_attach` is called from Send + InvalidOperation
+ DeliveryFail. Rollup: 6/4/0 → 10/0/0.

### 2026-05-22 — T9.G per-fn rollup

Per-function audit. Baseline: **6 ✅ / 4 ⚠️ / 0 ❌**. Open / send /
retrieve / delete-attach / DI lifecycle all ✅ (T7.6 wave wired
the char-server IPC). 4 ⚠️ are draft session state (setitem /
setzeny / removeitem / removezeny / setattachment) — pending
per-session draft model on PlayerEntity.

### 2026-05-20 — initial audit + service
- 10 functions covered. Draft session + IPC dispatch data-pending.
