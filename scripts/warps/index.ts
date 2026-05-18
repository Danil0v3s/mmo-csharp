// Warp registrations. Empty in Phase 1 — the 1,279 declarative warps from
// rAthena live in the `warp` DB table (Core.Database) and are loaded by
// WarpService at boot. registerWarp() is available for new content; bulk
// migration of the existing rows is deferred indefinitely.

export {};
