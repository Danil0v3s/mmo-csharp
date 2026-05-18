import type { FloatingNpcRegistration } from "@server/api";

export const eventManager: FloatingNpcRegistration = {
    name: "EventManager",
    onInit() {
        // Phase 5 will actually invoke this. No suspending operations here
        // (no dialog target without a player); just a synchronous stub.
    },
};
