import {Component,inject,signal} from '@angular/core';import {FormsModule} from '@angular/forms';import {Pagination} from '../../shared/ui';import {InventoryFacade} from './inventory-facade';import {ReferenceData} from '../../core/api/reference-data';import {LookupsDto} from '../../core/api/lookups.dto';import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
@Component({selector:'app-inventory',imports:[FormsModule,Pagination],providers:[InventoryFacade],templateUrl:'./inventory-page.html'})
export class InventoryPage{
 readonly facade=inject(InventoryFacade);readonly lookups=signal<LookupsDto|null>(null);lineOperator='';size='';type='';condition='';page=1;
 constructor(){inject(ReferenceData).lookups().pipe(takeUntilDestroyed()).subscribe({next:value=>this.lookups.set(value),error:error=>this.facade.error.set(error.detail)});this.load();}
 load(page=1):void{this.page=page;this.facade.load({lineOperator:this.lineOperator,size:this.size,type:this.type,condition:this.condition,pageIndex:page});}
 options(key:keyof LookupsDto){return Object.entries(this.lookups()?.[key]??{}).map(([value,label])=>({value,label}));}
}

