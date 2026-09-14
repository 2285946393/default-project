import { defineConfig } from "vite";
import { viteSingleFile } from "vite-plugin-singlefile";

export default defineConfig({
  base: "./",
  server: { host: true },
  build: { target: "es2021", assetsInlineLimit: 100000000, cssCodeSplit: false },
  plugins: [viteSingleFile()],
});
