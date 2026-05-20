# mail.cpp parity · 2026-05-20

`src/map/mail.cpp` (535 lines, 10 functions) — map-side mail
composition (draft / attach / send). Persistence + cross-server
delivery already live in the char-server `MailSendAsync` /
`MailReceiveAsync` RPCs.

All 10 entries covered by [IMailService](/Map.Server/Mail/IMailService.cs) /
[MailService](/Map.Server/Mail/MailService.cs). Per-session draft state +
the actual char-server forward are data-pending on the mail
session model.

## History

### 2026-05-20 — initial audit + service
- 10 functions covered. Draft session + IPC dispatch data-pending.
