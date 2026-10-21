export interface YardBlockDto {id:string;code:string;name:string;isVirtual:boolean;blockType:string;maxBays:number;oddBays:number;maxRows:number;maxTiers:number;totalSlots:number;occupiedSlots:number;}
export interface YardSlotDto {id:string;bay:number;row:number;tier:number;containerId:string|null;isOccupied:boolean;}
export interface BlockContainerDto {id:string;number:string;isoCode:string;size:string;type:string;grade:string;condition:string;ownerCode:string;orderNumber:string|null;orderExpiry:string|null;}
export interface YardLayoutDto {blockId:string;code:string;name:string;isVirtual:boolean;maxBays:number;oddBays:number;maxRows:number;maxTiers:number;slots:YardSlotDto[];containers:BlockContainerDto[];}
export interface VirtualBlockContainerDto {id:string;containerNumber:string;isoCode:string;size:string;type:string;grade:string;condition:string;gateInTime:string;}
export interface YardPositionDto {bay:number;row:number;tier:number;}
export interface YardSearchResult {status:string;location:{blockCode:string|null;blockId:string|null;bay:number|null;row:number|null;tier:number|null;isVirtual:boolean}|null;}
export interface YardBlockSizeRequest {newBays:number;newRows:number;newTiers:number;}
export interface PlaceRequest extends YardPositionDto {blockId:string;containerId:string;}
export interface RelocateRequest {blockId:string;sourceBay:number;sourceRow:number;sourceTier:number;targetBay:number;targetRow:number;targetTier:number;}
