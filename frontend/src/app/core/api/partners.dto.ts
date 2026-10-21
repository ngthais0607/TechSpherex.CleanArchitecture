export interface CustomerDto { id: string; taxCode: string; name: string; isActive: boolean; orderCount: number; activeOrderCount: number; }
export interface LineOperatorDto { id: string; code: string; name: string; isActive: boolean; containersInYard: number; orderCount: number; activeOrderCount: number; }
export type OwnerCodeDto = string;
