import {Component,computed,inject} from '@angular/core';import {FormsModule} from '@angular/forms';import {Pagination} from '../../shared/ui';import {ReportsFacade,ReportMode} from './reports-facade';import {reportHeaders} from './report.mapper';import {AuthService} from '../../core/auth/auth-service';import {can} from '../../core/auth/permissions';import {normalizeContainerNumber} from '../../core/presentation/container-number';
@Component({selector:'app-reports',imports:[FormsModule,Pagination],providers:[ReportsFacade],templateUrl:'./reports-page.html'})
export class ReportsPage{
 readonly facade=inject(ReportsFacade);readonly auth=inject(AuthService);readonly canExpiring=computed(()=>can('readExpiringOrders',this.auth.user()?.roles??[]));readonly headers=computed(()=>reportHeaders[this.facade.mode()]);
 mode:ReportMode='dwell';lineOperator='';minDays=0;includeGatedOut=false;days=7;number='';page=1;
 constructor(){this.load();}
 load(page=1):void{this.page=page;this.facade.load(this.mode,{lineOperator:this.lineOperator||undefined,minDays:this.mode==='dwell'?Number(this.minDays):undefined,includeGatedOut:this.mode==='dwell'?this.includeGatedOut:undefined,days:this.mode==='expiring'?Number(this.days):undefined,pageIndex:page},normalizeContainerNumber(this.number));}
 export():void{this.facade.export(this.mode==='dwell'?'Lưu bãi':this.mode==='expiring'?'Lệnh sắp hết hạn':'Lịch sử container',this.headers());}
}

