import {DestroyRef,inject,Injectable,signal} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {catchError,debounceTime,EMPTY,finalize,map,Observable,Subject,switchMap,tap,throwError} from 'rxjs';
import {ReportsApi} from '../../core/api/reports-api';
import {ApiPage,ApiParams} from '../../core/api/api.types';
import {AuthService} from '../../core/auth/auth-service';
import {can} from '../../core/auth/permissions';
import {ApiError} from '../../core/api/api-error';
import {QueryInvalidation} from '../../core/api/query-invalidation';
import {dwellRow,expiringRow,historyRow,ReportRow} from './report.mapper';
import {collectPages} from '../../core/export/paged-export';
import {downloadWorkbook} from '../../core/export/workbook-export';
export type ReportMode='dwell'|'expiring'|'history';
@Injectable()
export class ReportsFacade {
 private readonly api=inject(ReportsApi);private readonly auth=inject(AuthService);private readonly queries=new Subject<void>();private abort?:AbortController;
 readonly mode=signal<ReportMode>('dwell');readonly query=signal<ApiParams>({pageIndex:1,pageSize:8});readonly number=signal('');readonly rows=signal<ReportRow[]>([]);readonly totalCount=signal(0);readonly loading=signal(false);readonly error=signal('');readonly exporting=signal(false);readonly progress=signal('');
 constructor(){this.queries.pipe(debounceTime(300),switchMap(()=>{this.loading.set(true);this.error.set('');return this.page(this.mode(),this.query(),this.number()).pipe(tap(page=>{this.rows.set(page.items);this.totalCount.set(page.totalCount);}),catchError(error=>{this.error.set(error.detail??error.message);return EMPTY;}),finalize(()=>this.loading.set(false)));}),takeUntilDestroyed()).subscribe();inject(QueryInvalidation).changes$.pipe(takeUntilDestroyed()).subscribe(keys=>{if(keys.includes('reports'))this.queries.next();});inject(DestroyRef).onDestroy(()=>this.abort?.abort());}
 load(mode:ReportMode,query:ApiParams,number=''):void{this.mode.set(mode);this.query.set({...query,pageSize:8});this.number.set(number);this.queries.next();}
 private page(mode:ReportMode,query:ApiParams,number:string):Observable<ApiPage<ReportRow>>{
  if(mode==='history')return number?this.api.history(number,Number(query['pageIndex']??1),Number(query['pageSize']??8)).pipe(map(page=>({...page,items:page.items.map(historyRow)}))):throwError(()=>new Error('Nhập số container để xem lịch sử.'));
  if(mode==='expiring')return can('readExpiringOrders',this.auth.user()?.roles??[])?this.api.expiringOrders(query).pipe(map(page=>({...page,items:page.items.map(expiringRow)}))):throwError(()=>new ApiError(403,'Auth.Forbidden','Bạn không có quyền đọc lệnh sắp hết hạn.'));
  return this.api.dwellTimes(query).pipe(map(page=>({...page,items:page.items.map(dwellRow)})));
 }
 async export(title:string,headers:string[]):Promise<void>{if(this.exporting())return;this.abort=new AbortController();const abort=this.abort,mode=this.mode(),query={...this.query()},number=this.number();this.exporting.set(true);this.error.set('');
  try{const rows=await collectPages((pageIndex,pageSize)=>this.page(mode,{...query,pageIndex,pageSize},number),abort.signal,(done,total)=>this.progress.set(done+'/'+total));await downloadWorkbook(title,headers,rows.map(row=>row.cells),JSON.stringify({...query,containerNumber:number})+'; dữ liệu có thể thay đổi đồng thời',abort.signal);}catch(error){if(!abort.signal.aborted)this.error.set(error instanceof Error?error.message:'Không thể xuất báo cáo.');}finally{this.exporting.set(false);}
 }
 cancelExport():void{this.abort?.abort();}
}
