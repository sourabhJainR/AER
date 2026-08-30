export function scalar(value: unknown): string {
  if (value === null || value === undefined) return "-";
  if (typeof value === "boolean") return String(value);
  if (typeof value === "number") return String(value);
  if (typeof value === "string") return /[,;:\n"{}\[\]]/.test(value) ? JSON.stringify(value) : value;
  return JSON.stringify(value);
}

export function encode(value: Record<string, unknown>, indent = ""): string {
  const lines: string[] = [];
  for (const [key, item] of Object.entries(value)) {
    if (Array.isArray(item) && item.length && item.every(x => x && typeof x === "object" && !Array.isArray(x))) {
      const columns: string[] = [];
      for (const row of item as Record<string, unknown>[]) for (const column of Object.keys(row)) if (!columns.includes(column)) columns.push(column);
      lines.push(`${indent}${key}[${item.length}]{${columns.join(",")}}:`);
      for (const row of item as Record<string, unknown>[]) lines.push(`${indent}  ${columns.map(c => scalar(row[c])).join(",")}`);
    } else if (item && typeof item === "object" && !Array.isArray(item)) {
      lines.push(`${indent}${key}:`);
      lines.push(...encode(item as Record<string, unknown>, indent + "  ").split("\n"));
    } else if (Array.isArray(item)) {
      lines.push(`${indent}${key}[${item.length}]:${item.map(scalar).join(",")}`);
    } else {
      lines.push(`${indent}${key}:${scalar(item)}`);
    }
  }
  return lines.join("\n");
}
