import {DestroyRef,inject,Injectable,signal} from '@angular/core';
import {Subscription} from 'rxjs';
import {AuthService} from '../../core/auth/auth-service';
import {can} from '../../core/auth/permissions';
import {ReportsApi} from '../../core/api/reports-api';
import {BlockCapacityDto,DailyGateTrafficDto,DashboardSummaryDto,DwellTimeDto,ExpiringDeliveryOrderDto} from '../../core/api/reports.dto';
import {collectPages} from '../../core/export/paged-export';
import {QueryInvalidation} from '../../core/api/query-invalidation';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
@Injectable()
export class DashboardFacade {
 private readonly api=inject(ReportsApi);private readonly auth=inject(AuthService);private requests=new Subscription();private abort=new AbortController();
 readonly summary=signal<DashboardSummaryDto|null>(null);readonly traffic=signal<DailyGateTrafficDto[]>([]);readonly capacity=signal<BlockCapacityDto[]>([]);readonly topDwell=signal<DwellTimeDto[]>([]);readonly expiring=signal<ExpiringDeliveryOrderDto[]>([]);readonly error=signal('');
 constructor(){inject(DestroyRef).onDestroy(()=>{this.requests.unsubscribe();this.abort.abort();});inject(QueryInvalidation).changes$.pipe(takeUntilDestroyed()).subscribe(keys=>{if(keys.includes('reports'))this.load();});}
 load():void{
  this.requests.unsubscribe();this.requests=new Subscription();this.abort.abort();this.abort=new AbortController();const abort=this.abort;this.error.set('');
  const error=(error:{detail?:string;message?:string})=>this.error.set(error.detail??error.message??'Không thể tải báo cáo.');
  this.requests.add(this.api.summary().subscribe({next:value=>this.summary.set(value),error}));
  this.requests.add(this.api.traffic(7).subscribe({next:value=>this.traffic.set(value),error}));
  this.requests.add(this.api.capacity().subscribe({next:value=>this.capacity.set(value),error}));
  if(can('readExpiringOrders',this.auth.user()?.roles??[]))this.requests.add(this.api.expiringOrders({days:7,pageSize:5}).subscribe({next:page=>this.expiring.set(page.items),error}));
  collectPages((pageIndex,pageSize)=>this.api.dwellTimes({pageIndex,pageSize}),abort.signal,()=>{}).then(rows=>{if(!abort.signal.aborted)this.topDwell.set(rows.sort((a,b)=>b.daysInDepot-a.daysInDepot||a.containerNumber.localeCompare(b.containerNumber)).slice(0,5));}).catch(reason=>{if(!abort.signal.aborted)error(reason);});
 }
}
