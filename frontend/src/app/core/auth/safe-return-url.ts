export function safeReturnUrl(raw: string | null): string {
  if (!raw || /[\\\u0000-\u001f]/.test(raw)) return '/dashboard';
  return /^\/(dashboard|containers|delivery-orders|gate|yard|partners|reports|inventory)([/?#]|$)/.test(
    raw,
  )
    ? raw
    : '/dashboard';
}
