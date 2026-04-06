export const NormalizeLineEndingsPlugin = async ({ $ }) => {
  return {
    "tool.execute.after": async (input, output) => {
	    const filePaths =
	      input.tool === "apply_patch"
	        ? (output.metadata?.files ?? [])
	            .filter(file => file.type !== "delete")
	            .map(file => file.movePath ?? file.filePath)
	        : [
	            input.tool === "write" ? output.metadata?.filepath :
	            input.tool === "edit" ? output.metadata?.filediff?.file :
	            null,
	          ].filter(Boolean)
	    if (!filePaths.length) return

	    const script = `${import.meta.dirname}/normalize-line-endings.ps1`
	    for (const filePath of [...new Set(filePaths)]) {
	      try {
	        await $`powershell -File ${script} "${filePath}"`
	      } catch (error) {
	        console.error("Failed to normalize line endings for", filePath, error)
	      }
	    }
    },
  }
}
