import { Component, computed, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Badge, Modal, Pagination } from '../../shared/ui';
import { AuthService } from '../../core/auth/auth-service';
import { can } from '../../core/auth/permissions';
import { ApiError } from '../../core/api/api-error';
import { ReferenceData } from '../../core/api/reference-data';
import { LookupsDto } from '../../core/api/lookups.dto';
import { ContainerTypeCode } from '../../core/api/containers.dto';
import { Notifications } from '../../core/notifications/notifications';
import { MasterChoice, MasterPicker } from '../../core/presentation/master-picker';
import { orderStatusLabels } from '../../core/presentation/container-labels';
import { containerTypes } from '../containers/container.mapper';
import { normalizeContainerNumber } from '../../core/presentation/container-number';
import { OrdersFacade } from './orders-facade';
@Component({
  selector: 'app-orders', imports: [FormsModule, DatePipe, Badge, Modal, Pagination, MasterPicker],
  providers: [OrdersFacade], templateUrl: './orders-page.html',
})
export class OrdersPage {
  readonly facade=inject(OrdersFacade);
  readonly auth=inject(AuthService);
  readonly router=inject(Router);
  readonly route=inject(ActivatedRoute);
  readonly notify=inject(Notifications);
  readonly canManage=computed(()=>can('manageOrders',this.auth.user()?.roles??[]));
  readonly lookups=signal<LookupsDto|null>(null);
  readonly search=signal('');
  readonly status=signal('');
  readonly sort=signal('createdAt');
  readonly selected=signal('');
  readonly showCreate=signal(false);
  readonly createError=signal('');
  readonly selectedCustomer=signal<MasterChoice|null>(null);
  readonly selectedOperator=signal<MasterChoice|null>(null);
  readonly assignItem=signal('');
  readonly candidateSearch=signal('');
  readonly manualNumbers=signal('');
  readonly types=containerTypes;
  readonly statusLabels=orderStatusLabels;
  readonly statuses=['Active','ExpiringSoon','Expired','FullyDelivered'];
  createForm={orderNumber:'',expiryDate:'',outboundVessel:''};
  lines:{size:'Ft20'|'Ft40';type:ContainerTypeCode;quantity:number}[]=[{size:'Ft20',type:'Dry',quantity:1}];
  constructor(){
    inject(ReferenceData).lookups().pipe(takeUntilDestroyed()).subscribe({next:value=>this.lookups.set(value),error:error=>this.notify.error(error.detail)});
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params=>{
      const query=Object.fromEntries(params.keys.map(key=>[key,params.get(key)]));
      this.facade.setQuery(query);
      this.search.set(params.get('search')??'');this.status.set(params.get('status')??'');this.sort.set(params.get('sort')??'createdAt');
      const id=params.get('order')??'';
      if(id!==this.selected()){this.selected.set(id);this.facade.select(id);}
      this.manualNumbers.set(params.get('container')??'');
    });
    effect(()=>{
      const order=this.facade.detail();
      if(order&&!order.items.some(item=>item.id===this.assignItem()))this.assignItem.set(order.items[0]?.id??'');
    });
    effect(()=>{const id=this.assignItem();if(id&&this.canManage())this.facade.loadCandidates(id,this.candidateSearch());});
  }
  update():void{this.router.navigate([],{queryParams:{search:this.search()||null,status:this.status()||null,sort:this.sort(),pageIndex:1},queryParamsHandling:'merge',replaceUrl:true});}
  next(pageIndex:number):void{this.router.navigate([],{queryParams:{pageIndex},queryParamsHandling:'merge',replaceUrl:true});}
  open(id:string):void{this.router.navigate([],{queryParams:{order:id},queryParamsHandling:'merge'});}
  close():void{this.router.navigate([],{queryParams:{order:null},queryParamsHandling:'merge'});}
  toggle(number:string):void{this.facade.selectedNumbers.update(values=>values.includes(number)?values.filter(value=>value!==number):[...values,number]);}
  assign():void{
    const numbers=[...new Set([...this.facade.selectedNumbers(),...this.manualNumbers().split(/[\s,;]+/).map(normalizeContainerNumber).filter(Boolean)])];
    if(!numbers.length||this.facade.pending())return;
    this.facade.selectedNumbers.set(numbers);
    this.facade.assign(numbers).subscribe({next:result=>{
      this.manualNumbers.set(result.failedContainerNumbers.join('\n'));
      if(result.failedContainerNumbers.length)this.notify.error(this.facade.message());else this.notify.success(this.facade.message());
    },error:()=>{}});
  }
  release(id:string):void{if(!this.facade.pending())this.facade.release(id).subscribe({next:()=>this.notify.success('Đã giải phóng giữ chỗ.'),error:()=>{}});}
  startCreate():void{this.createError.set('');this.showCreate.set(true);}
  create():void{
    const customer=this.selectedCustomer(),operator=this.selectedOperator();
    if(!customer||!operator){this.createError.set('Vui lòng chọn khách hàng và hãng khai thác.');return;}
    if(this.facade.pending())return;
    this.facade.create({orderNumber:this.createForm.orderNumber.trim(),customerTaxCode:customer.code,customerName:customer.name,lineOperator:operator.code,
      expiryDate:this.createForm.expiryDate+'T00:00:00Z',outboundVessel:this.createForm.outboundVessel.trim(),
      items:this.lines.map(line=>({...line,quantity:Number(line.quantity)}))}).subscribe({
        next:result=>{this.showCreate.set(false);this.notify.success('Đã tạo lệnh giao trên máy chủ.');this.open(result.id);},
        error:(error:ApiError)=>this.createError.set(error.detail),
      });
  }
  count(itemId:string){return this.facade.bookings()?.itemCounts.find(item=>item.itemId===itemId);}
  candidatesNext(pageIndex:number):void{this.facade.loadCandidates(this.assignItem(),this.candidateSearch(),pageIndex);}
}

