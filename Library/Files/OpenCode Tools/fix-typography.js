import { tool } from "@opencode-ai/plugin"
import { readFileSync, writeFileSync } from "fs"

// Typography corrections are point replacements of ASCII "typewriter" punctuation with
// their proper Unicode equivalents. For each allowed target code point, we know the
// single ASCII code point that is a plausible source. If the caller asks us to replace
// a character at a position whose existing code point is NOT a listed source for the
// target, we treat it as a caller error (usually a column miscount) and refuse that
// correction, rather than silently corrupting the file.
//
// Key: target code point (hex). Value: array of acceptable source code points.
// Identity replacements (same code point) are handled separately and reported as
// no-ops, so they do not need to appear here.
const allowedSources = {
  // Smart single quotes: replace ASCII apostrophe (U+0027)
  0x2018: [0x0027], // LEFT SINGLE QUOTATION MARK
  0x2019: [0x0027], // RIGHT SINGLE QUOTATION MARK
  // Smart double quotes: replace ASCII straight double quote (U+0022)
  0x201C: [0x0022], // LEFT DOUBLE QUOTATION MARK
  0x201D: [0x0022], // RIGHT DOUBLE QUOTATION MARK
  // Dashes: replace ASCII hyphen-minus (U+002D)
  0x2013: [0x002D], // EN DASH
  0x2014: [0x002D], // EM DASH
  // Ellipsis: replace ASCII period (U+002E)
  0x2026: [0x002E], // HORIZONTAL ELLIPSIS
}

function formatCp(cp) {
  return "U+" + cp.toString(16).toUpperCase().padStart(4, "0")
}

export default tool({
  description:
    "Replace characters at specific file positions with Unicode code points. " +
    "Use this to fix typography in human-language text (comments, string literals, etc.) " +
    "by replacing ASCII quotes/dashes with their proper Unicode equivalents. " +
    "Each correction specifies a 1-based line number, 1-based column number, and the " +
    "target code point as a hex string (e.g. \"2019\" for RIGHT SINGLE QUOTATION MARK). " +
    "The tool validates that the existing character at the specified position is a " +
    "plausible source for the requested target (e.g. an ASCII apostrophe U+0027 for a " +
    "smart-quote target). If the existing character is unrelated, the correction is " +
    "refused and reported without modifying the file at that position.",
  args: {
    filePath: tool.schema.string().describe("Absolute or relative path to the file"),
    corrections: tool.schema
      .array(
        tool.schema.object({
          line: tool.schema.number().int().min(1).describe("1-based line number"),
          column: tool.schema.number().int().min(1).describe("1-based column number"),
          codePoint: tool.schema
            .string()
            .regex(/^[0-9a-fA-F]{4,6}$/)
            .describe("Target Unicode code point as hex string, e.g. \"2019\""),
        }),
      )
      .min(1)
      .describe("Array of corrections to apply"),
  },
  async execute(args) {
    const content = readFileSync(args.filePath, "utf-8")
    const lines = content.split(/\r?\n/)
    const useCrlf = content.includes("\r\n")
    const results = []
    let appliedCount = 0
    let refusedCount = 0

    // Sort corrections by line descending, then column descending, so that
    // replacements don't shift column positions of earlier corrections on the
    // same line (in case of multi-byte differences).
    const sorted = [...args.corrections].sort((a, b) =>
      a.line !== b.line ? b.line - a.line : b.column - a.column,
    )

    for (const c of sorted) {
      const lineIdx = c.line - 1
      if (lineIdx < 0 || lineIdx >= lines.length) {
        results.push(`Line ${c.line}: out of range (file has ${lines.length} lines)`)
        refusedCount++
        continue
      }

      const line = lines[lineIdx]
      const colIdx = c.column - 1

      // Handle the column as a character index (not byte index), accounting
      // for surrogate pairs via Array.from.
      const chars = Array.from(line)
      if (colIdx < 0 || colIdx >= chars.length) {
        results.push(
          `Line ${c.line}, col ${c.column}: out of range (line has ${chars.length} characters)`,
        )
        refusedCount++
        continue
      }

      const targetCp = parseInt(c.codePoint, 16)
      const targetChar = String.fromCodePoint(targetCp)
      const originalChar = chars[colIdx]
      const originalCp = originalChar.codePointAt(0)

      if (originalCp === targetCp) {
        results.push(
          `Line ${c.line}, col ${c.column}: already ${formatCp(targetCp)} - no change`,
        )
        continue
      }

      // Validate that the existing character is a plausible source for the requested
      // target. This catches caller column miscounts that would otherwise silently
      // corrupt the file (e.g. turning a letter 'u' into U+2019 when the apostrophe
      // the caller intended to fix is several columns away).
      const allowed = allowedSources[targetCp]
      if (allowed === undefined) {
        results.push(
          `Line ${c.line}, col ${c.column}: refused - target ${formatCp(targetCp)} is not a supported typography replacement. Supported targets: ${Object.keys(
            allowedSources,
          )
            .map((k) => formatCp(parseInt(k, 10)))
            .join(", ")}.`,
        )
        refusedCount++
        continue
      }
      if (!allowed.includes(originalCp)) {
        const allowedStr = allowed.map((cp) => formatCp(cp)).join(" or ")
        const contextStart = Math.max(0, colIdx - 5)
        const contextEnd = Math.min(chars.length, colIdx + 6)
        const context = chars.slice(contextStart, contextEnd).join("")
        results.push(
          `Line ${c.line}, col ${c.column}: refused - existing character is ${formatCp(originalCp)} ('${originalChar}') but target ${formatCp(targetCp)} requires source ${allowedStr}. Context: "${context}". Likely a column miscount; verify the position and retry.`,
        )
        refusedCount++
        continue
      }

      chars[colIdx] = targetChar
      lines[lineIdx] = chars.join("")
      appliedCount++
      results.push(
        `Line ${c.line}, col ${c.column}: ${formatCp(originalCp)} -> ${formatCp(targetCp)}`,
      )
    }

    if (appliedCount > 0) {
      const newContent = lines.join(useCrlf ? "\r\n" : "\n")
      writeFileSync(args.filePath, newContent, "utf-8")
    }

    const header = `${appliedCount} correction${appliedCount === 1 ? "" : "s"} applied, ${refusedCount} refused.`
    return [header, ...results].join("\n")
  },
})
