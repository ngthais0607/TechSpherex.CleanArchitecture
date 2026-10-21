import {Component,inject,signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {DynamicForm,Field,Modal,Pagination} from '../../shared/ui';
import {PartnersFacade} from './partners-facade';
import {PartnerKind,PartnerView,partnerColor} from './partner.mapper';
import {PartnersApi} from '../../core/api/partners-api';
import {Notifications} from '../../core/notifications/notifications';
@Component({selector:'app-partners',imports:[FormsModule,DynamicForm,Modal,Pagination],providers:[PartnersFacade],templateUrl:'./partners-page.html'})
export class PartnersPage {
  readonly facade=inject(PartnersFacade);readonly api=inject(PartnersApi);readonly notify=inject(Notifications);
  readonly tab=signal<PartnerKind>('customers');readonly search=signal('');readonly showForm=signal(false);readonly editing=signal<PartnerView|null>(null);
  readonly ownerOperator=signal<PartnerView|null>(null);readonly ownerCodes=signal<string[]>([]);readonly newOwnerCode=signal('');readonly partnerColor=partnerColor;
  constructor(){this.reload();}
  reload(page=1):void{this.facade.setQuery(this.tab(),this.search(),page);}
  switchTab(kind:PartnerKind):void{this.tab.set(kind);this.search.set('');this.reload();}
  start(row:PartnerView|null):void{this.editing.set(row);this.facade.error.set(null);this.showForm.set(true);}
  fields():Field[]{return[{key:this.tab()==='customers'?'taxCode':'code',label:this.tab()==='customers'?'Mã số thuế':'Mã hãng',required:true,readonly:!!this.editing()},{key:'name',label:'Tên',required:true}];}
  value(){const row=this.editing();return row?{taxCode:row.code,code:row.code,name:row.name}:{};}
  save(value:Record<string,unknown>):void{this.facade.save(this.tab(),this.editing()?.id??null,value).subscribe({next:()=>{this.showForm.set(false);this.notify.success('Đã lưu danh mục trên máy chủ.');},error:()=>{}});}
  setActive(row:PartnerView):void{this.facade.setActive(this.tab(),row.id,!row.isActive).subscribe({next:()=>this.notify.success('Đã cập nhật trạng thái hoạt động.'),error:()=>{}});}
  openOwners(row:PartnerView):void{this.ownerOperator.set(row);this.newOwnerCode.set('');this.loadOwners();}
  loadOwners():void{const row=this.ownerOperator();if(row)this.api.ownerCodes(row.id).subscribe({next:codes=>this.ownerCodes.set(codes),error:error=>this.notify.error(error.detail)});}
  addOwner():void{const row=this.ownerOperator();if(!row)return;this.facade.addOwnerCode(row.id,this.newOwnerCode().trim().toUpperCase()).subscribe({next:()=>{this.newOwnerCode.set('');this.loadOwners();},error:()=>{}});}
  removeOwner(code:string):void{const row=this.ownerOperator();if(!row)return;this.facade.removeOwnerCode(row.id,code).subscribe({next:()=>this.loadOwners(),error:()=>{}});}
}
