export function truncateId(id: string, length = 6): string {
  return id.slice(-length);
}
