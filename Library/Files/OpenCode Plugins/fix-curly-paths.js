import { existsSync } from "fs"
import { join, parse } from "path"

const QUOTES = ["'", "\u2018", "\u2019"]

// Yield every combination of quote characters for a path component. The first
// yielded value is the original (all positions unchanged), so an existing path
// with straight quotes is found immediately.
function* quotePermutations(name) {
  const positions = []
  for (let i = 0; i < name.length; i++) {
    if (QUOTES.includes(name[i])) positions.push(i)
  }
  if (positions.length === 0) {
    yield name
    return
  }
  function* permute(chars, depth) {
    if (depth === positions.length) {
      yield chars.join("")
      return
    }
    for (const q of QUOTES) {
      chars[positions[depth]] = q
      yield* permute(chars, depth + 1)
    }
  }
  yield* permute(Array.from(name), 0)
}

// Walk the path component by component, resolving each segment that contains a
// quote character against the filesystem. Components that don't exist on disk
// (e.g. a new file being written) are left as the LLM provided them.
function resolveQuotes(inputPath) {
  if (!QUOTES.some((q) => inputPath.includes(q))) return inputPath

  const { root } = parse(inputPath)
  const components = inputPath.slice(root.length).split(/[/\\]/).filter(Boolean)

  let resolved = root
  let changed = false

  for (const component of components) {
    if (!QUOTES.some((q) => component.includes(q))) {
      resolved = join(resolved, component)
      continue
    }

    let matched = false
    for (const variant of quotePermutations(component)) {
      if (existsSync(join(resolved, variant))) {
        if (variant !== component) changed = true
        resolved = join(resolved, variant)
        matched = true
        break
      }
    }
    if (!matched) {
      resolved = join(resolved, component)
    }
  }

  return changed ? resolved : inputPath
}

export const FixCurlyPathsPlugin = async ({ $ }) => ({
  "tool.execute.before": async (input, output) => {
    const pathParams = {
      read: ["filePath"],
      write: ["filePath"],
      edit: ["filePath"],
      glob: ["path"],
      grep: ["path"],
      "ewl-fix-typography": ["filePath"],
    }

    const keys = pathParams[input.tool]
    // Not a tool we handle; bail out to avoid iterating undefined.
    if (!keys) return

    for (const key of keys) {
      const original = output.args[key]
      // The path parameter may be optional (e.g. glob and grep), so it could be undefined.
      if (typeof original !== "string") continue
      output.args[key] = resolveQuotes(original)
    }
  },
})
