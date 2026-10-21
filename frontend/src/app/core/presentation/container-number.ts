export function normalizeContainerNumber(value: string): string {
  return value.replace(/\s/g, '').toUpperCase();
}
export function formatContainerNumber(value: string): string {
  return value.slice(0, 4) + ' ' + value.slice(4);
}
