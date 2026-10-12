import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";
import { groqPlugin } from "./server/groq.ts";

export default defineConfig({
  plugins: [react(), groqPlugin()],
  server: {
    port: 5173,
    strictPort: true,
    watch: {
      ignored: ["**/tools/**"],
    },
  },
  test: {
    environment: "node",
  },
});
