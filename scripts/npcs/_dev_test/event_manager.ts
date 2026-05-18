import type { FloatingNpcRegistration } from "@server/api";

export const eventManager: FloatingNpcRegistration = {
    name: "EventManager",
    *onInit() {
        // Phase 5 will actually invoke this. An empty generator is a
        // valid no-op handler.
    },
};
