import { registerFileNormalization } from "./normalize-files.js"
import { registerCleanupPermissions } from "./cleanup-permissions.js"
import { registerShellEncoding } from "./shell-encoding.js"

// OpenCode supplies ctx; no SDK dependency or npm install is needed.
export default {
  id: "ewl",
  async setup(ctx) {
    await registerFileNormalization(ctx)
    await registerCleanupPermissions(ctx)
    await registerShellEncoding(ctx)
  },
}
