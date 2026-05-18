import type { FloatingNpcRegistration } from "@server/api";

export const eventManager: FloatingNpcRegistration = {
    name: "EventManager",
    onInit() {
        // Phase 5 will actually invoke this. For now it just proves the
        // floating-NPC registration path works.
    },
};
