const all = ['Admin', 'YardPlanner', 'OrderClerk', 'GateClerk'];
const policies = {
  admin: ['Admin'],
  readMasterData: all,
  readContainers: all,
  registerContainer: ['Admin', 'GateClerk'],
  manageYard: ['Admin', 'YardPlanner'],
  readYard: ['Admin', 'YardPlanner', 'GateClerk'],
  manageOrders: ['Admin', 'OrderClerk'],
  readOrders: ['Admin', 'OrderClerk', 'GateClerk'],
  manageGate: ['Admin', 'GateClerk'],
  readReports: all,
  readExpiringOrders: ['Admin', 'OrderClerk', 'GateClerk'],
} as const;
export type Capability = keyof typeof policies;
export function can(capability: Capability, roles: readonly string[]): boolean {
  return roles.some((role) => (policies[capability] as readonly string[]).includes(role));
}
