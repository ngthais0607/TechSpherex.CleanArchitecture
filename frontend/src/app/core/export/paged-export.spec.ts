import {describe,expect,it} from 'vitest';
import {NEVER,of,throwError} from 'rxjs';
import {collectPages} from './paged-export';
import {ApiPage} from '../api/api.types';
function page(index:number,total=201):ApiPage<number>{return {items:Array.from({length:Math.min(100,total-(index-1)*100)},(_,i)=>(index-1)*100+i),pageIndex:index,pageSize:100,totalCount:total,totalPages:Math.ceil(total/100),hasPreviousPage:index>1,hasNextPage:index<Math.ceil(total/100)};}
describe('paged export',()=>{
 it('exportsAll201RowsInThreePages',async()=>{const requested:number[]=[];const rows=await collectPages((index,size)=>{expect(size).toBe(100);requested.push(index);return of(page(index));},new AbortController().signal,()=>{});expect(rows).toHaveLength(201);expect(new Set(rows).size).toBe(201);expect(requested).toEqual([1,2,3]);});
 it('pageTwoFailureOrAbortDoesNotDownloadPartialWorkbook',async()=>{
   let downloaded=false;await expect(collectPages(index=>index===1?of(page(1)):throwError(()=>new Error('offline')),new AbortController().signal,()=>{}).then(()=>{downloaded=true;})).rejects.toThrow('offline');expect(downloaded).toBe(false);
   const abort=new AbortController();const promise=collectPages(()=>NEVER,abort.signal,()=>{});abort.abort();await expect(promise).rejects.toMatchObject({name:'AbortError'});
 });
 it('changedTotalsStopWithRetryMessage',async()=>{await expect(collectPages(index=>of(page(index,index===1?201:202)),new AbortController().signal,()=>{})).rejects.toThrow('thay đổi');});
 it('repeatedOrMissingPagesAreRejected',async()=>{await expect(collectPages(()=>of(page(1)),new AbortController().signal,()=>{})).rejects.toThrow('trang');});
});
