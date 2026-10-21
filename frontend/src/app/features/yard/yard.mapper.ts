import {YardLayoutDto} from '../../core/api/yard.dto';
export function rowLabel(row:number):string {let label='';while(row>0){row--;label=String.fromCharCode(65+row%26)+label;row=Math.floor(row/26);}return label;}
export function physicalCapacity(layout:YardLayoutDto):number{return layout.isVirtual?0:layout.oddBays*layout.maxRows*layout.maxTiers;}
export function mapYardContainers(layout:YardLayoutDto){
  return layout.containers.map(container=>{
    const footprint=layout.slots.filter(slot=>slot.containerId===container.id).sort((a,b)=>a.bay-b.bay);
    const first=footprint[0],last=footprint[footprint.length-1];
    return {...container,footprint,bay:first&&last?(first.bay+last.bay)/2:0,row:first?.row??0,tier:first?.tier??0};
  });
}
