export const Utf8BomPlugin = async ({ $ }) => {
  return {
    "tool.execute.after": async (input, output) => {
      const filePath =
        input.tool === "write" ? output.metadata?.filepath :
        input.tool === "edit" ? output.metadata?.filediff?.file :
        null
      const extensions = [".cs", ".csproj"]
      if (!filePath || !extensions.some(ext => filePath.endsWith(ext))) return

      try {
        const script = `${import.meta.dirname}/ensure-utf8-bom.ps1`
        await $`powershell -File ${script} "${filePath}"`
      } catch (error) {
        console.error("Failed to ensure UTF-8 BOM for", filePath, error)
      }
    },
  }
}
