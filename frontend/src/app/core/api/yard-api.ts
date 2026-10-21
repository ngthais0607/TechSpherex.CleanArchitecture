import {inject,Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {tap} from 'rxjs';
import {API_BASE_URL} from './api-config';
import {ApiPage} from './api.types';
import {QueryInvalidation} from './query-invalidation';
import {PlaceRequest,RelocateRequest,VirtualBlockContainerDto,YardBlockDto,YardBlockSizeRequest,YardLayoutDto,YardPositionDto,YardSearchResult} from './yard.dto';
@Injectable({providedIn:'root'})
export class YardApi {
  private readonly http=inject(HttpClient);private readonly invalidation=inject(QueryInvalidation);private readonly url=API_BASE_URL+'/yard';
  blocks(){return this.http.get<YardBlockDto[]>(this.url+'/blocks');}
  layout(idOrCode:string,tier=1){return this.http.get<YardLayoutDto>(this.url+'/blocks/'+encodeURIComponent(idOrCode)+'/layout',{params:{tier}});}
  search(containerNumber:string){return this.http.get<YardSearchResult>(this.url+'/search',{params:{containerNumber}});}
  virtualContainers(code:string){return this.http.get<VirtualBlockContainerDto[]>(this.url+'/virtual-blocks/'+encodeURIComponent(code)+'/containers');}
  freeSlots(blockId:string,size:string,pageIndex=1){return this.http.get<ApiPage<YardPositionDto>>(this.url+'/free-slots',{params:{blockId,size,pageIndex,pageSize:8}});}
  create(input:{code:string;name:string;bays:number;rows:number;tiers:number;isVirtual:boolean}){return this.http.post<{id:string}>(this.url+'/blocks',input).pipe(this.changed());}
  place(input:PlaceRequest){return this.http.post<void>(this.url+'/placements',input).pipe(this.changed());}
  remove(input:{blockId:string;bay:number;row:number;tier:number}){return this.http.post<void>(this.url+'/removals',input).pipe(this.changed());}
  relocate(input:RelocateRequest){return this.http.post<void>(this.url+'/relocations',input).pipe(this.changed());}
  placeFromVirtual(code:string,containerId:string,target:{targetBlockCode:string;bay:number;row:number;tier:number}){return this.http.post<void>(this.url+'/virtual-blocks/'+encodeURIComponent(code)+'/containers/'+containerId+'/place',target).pipe(this.changed());}
  expand(id:string,input:YardBlockSizeRequest){return this.http.post<void>(this.url+'/blocks/'+id+'/expand',input).pipe(this.changed());}
  shrink(id:string,input:YardBlockSizeRequest){return this.http.post<void>(this.url+'/blocks/'+id+'/shrink',input).pipe(this.changed());}
  private changed<T>(){return tap<T>(()=>this.invalidation.invalidate(['yard','containers','reports']));}
}
