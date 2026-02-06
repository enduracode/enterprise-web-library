export const NormalizeLineEndingsPlugin = async ({ $ }) => {
  return {
    "tool.execute.after": async (input, output) => {
      const filePath =
        input.tool === "write" ? output.metadata?.filepath :
        input.tool === "edit" ? output.metadata?.filediff?.file :
        null
      if (!filePath) return

      try {
        const script = `${import.meta.dirname}/normalize-line-endings.ps1`
        await $`powershell -File ${script} "${filePath}"`
      } catch (error) {
        console.error("Failed to normalize line endings for", filePath, error)
      }
    },
  }
}
