import { readFileSync, writeFileSync } from "node:fs"
import { extname, resolve } from "node:path"

export function normalizeFile(path) {
  const original = readFileSync(path)
  const content = new TextDecoder("utf-8", { fatal: true, ignoreBOM: true }).decode(original)
  const bom = content.startsWith("\uFEFF") || [".cs", ".csproj", ".cshtml"].includes(extname(path).toLowerCase())
  const text = content.replace(/^\uFEFF/, "").replace(/\r\n|\r|\n/g, "\r\n")
  const normalized = Buffer.from((bom ? "\uFEFF" : "") + text, "utf8")
  if (!original.equals(normalized)) writeFileSync(path, normalized)
}

export async function registerFileNormalization(ctx) {
  await ctx.tool.hook("execute.after", (event) => {
    if (event.status !== "completed") return
    let paths = []
    if (event.tool === "patch") {
      paths = (event.result.output?.applied ?? []).filter(item => item.type !== "delete").map(item => item.target)
    } else if (event.tool === "write") {
      paths = [event.result.output?.target]
    } else if (event.tool === "edit") {
      paths = (event.result.output?.files ?? []).map(file => resolve(ctx.location.directory, file.file))
    }
    for (const path of new Set(paths.filter(Boolean))) normalizeFile(path)
  })
}
