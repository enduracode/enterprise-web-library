export const Utf8BomPlugin = async ({ $ }) => {
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
	    const extensions = [".cs", ".csproj", ".cshtml"]
	    const csFilePaths = [...new Set(filePaths)].filter(filePath => extensions.some(ext => filePath.endsWith(ext)))
	    if (!csFilePaths.length) return

	    const script = `${import.meta.dirname}/ensure-utf8-bom.ps1`
	    for (const filePath of csFilePaths) {
	      try {
	        await $`powershell -File ${script} "${filePath}"`
	      } catch (error) {
	        console.error("Failed to ensure UTF-8 BOM for", filePath, error)
	      }
	    }
    },
  }
}
