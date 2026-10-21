import {Component,computed,effect,inject,signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute,Router} from '@angular/router';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {DynamicForm,Field,Modal} from '../../shared/ui';
import {YardFacade} from './yard-facade';
import {YardApi} from '../../core/api/yard-api';
import {AuthService} from '../../core/auth/auth-service';
import {can} from '../../core/auth/permissions';
import {Notifications} from '../../core/notifications/notifications';
import {mapYardContainers,rowLabel} from './yard.mapper';
import {normalizeContainerNumber} from '../../core/presentation/container-number';
import {ContainersApi} from '../../core/api/containers-api';
import {finalize,switchMap} from 'rxjs';
@Component({selector:'app-yard',imports:[FormsModule,DynamicForm,Modal],providers:[YardFacade],templateUrl:'./yard-page.html',styleUrl:'./yard-page.scss'})
export class YardPage {
  readonly facade=inject(YardFacade);readonly api=inject(YardApi);readonly auth=inject(AuthService);
  readonly router=inject(Router);readonly route=inject(ActivatedRoute);readonly notify=inject(Notifications);
  readonly canManage=computed(()=>can('manageYard',this.auth.user()?.roles??[]));
  readonly cards=computed(()=>this.facade.layout()?mapYardContainers(this.facade.layout()!):[]);
  readonly selected=computed(()=>this.cards().find(card=>card.id===this.facade.selectedContainer()));
  readonly block=computed(()=>this.facade.blocks().find(block=>block.id===this.facade.blockId()));
  readonly tiers=computed(()=>Array.from({length:this.block()?.maxTiers??1},(_,i)=>i+1));
  readonly mode=signal<'expand'|'shrink'|null>(null);readonly moving=signal(false);readonly search=signal('');
  readonly placing=signal(false);readonly resolving=signal(false);private readonly containers=inject(ContainersApi);
  readonly highlight=signal('');private readonly params=signal<Record<string,string|null>>({});
  readonly rowLabel=rowLabel;
  constructor(){
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params=>this.params.set(Object.fromEntries(params.keys.map(key=>[key,params.get(key)]))));
    this.facade.loadBlocks();
    effect(()=>{
      const blocks=this.facade.blocks(),params=this.params();if(!blocks.length)return;
      const block=blocks.find(block=>block.id===params['block']||block.code===params['block'])??blocks[0];
      const raw=Number(params['tier']??1),tier=Math.min(block.maxTiers||1,Number.isInteger(raw)&&raw>0?raw:1);
      if(block.id!==this.facade.blockId()||tier!==this.facade.tier())this.facade.select(block.id,tier);
    });
  }
  choose(block:string,tier=1):void{this.router.navigate([],{queryParams:{block,tier},queryParamsHandling:'merge',replaceUrl:true});}
  find():void{this.api.search(normalizeContainerNumber(this.search())).subscribe({next:result=>{
    if(result.location?.blockId){this.highlight.set(normalizeContainerNumber(this.search()));this.choose(result.location.blockId,result.location.tier||1);}
    else this.notify.error(result.status==='OutOfYard'?'Container hiện không ở trong bãi.':'Không tìm thấy vị trí container.');
  },error:error=>this.notify.error(error.detail)});}
  resizeFields:Field[]=[{key:'newBays',label:'Bay lớn nhất',type:'number',required:true,min:1,hint:'Dùng chỉ số bay của block, không phải số ô 20ft.'},{key:'newRows',label:'Số Row',type:'number',required:true,min:1},{key:'newTiers',label:'Số Tier',type:'number',required:true,min:1}];
  resizeValue(){const block=this.block();return{newBays:block?.maxBays,newRows:block?.maxRows,newTiers:block?.maxTiers};}
  placeFields:Field[]=[{key:'number',label:'Số container chưa xếp vị trí',required:true},{key:'bay',label:'Bay',type:'number',required:true,min:1},{key:'row',label:'Row (số)',type:'number',required:true,min:1},{key:'tier',label:'Tier',type:'number',required:true,min:1}];
  place(value:Record<string,unknown>):void{
    if(!this.canManage()||this.resolving())return;this.resolving.set(true);
    this.containers.byNumber(normalizeContainerNumber(String(value['number']))).pipe(switchMap(container=>this.facade.place({blockId:this.facade.blockId(),containerId:container.id,bay:Number(value['bay']),row:Number(value['row']),tier:Number(value['tier'])})),finalize(()=>this.resolving.set(false)))
      .subscribe({next:()=>{this.placing.set(false);this.notify.success('Đã phân vị trí container.');},error:error=>this.facade.error.set(error)});
  }
  remove():void{const card=this.selected();if(!card||!this.canManage()||!confirm('Dỡ container khỏi vị trí hiện tại?'))return;
    this.facade.remove({blockId:this.facade.blockId(),bay:card.bay,row:card.row,tier:card.tier}).subscribe({next:()=>this.notify.success('Đã dỡ container khỏi vị trí.'),error:()=>{}});
  }
  resize(value:Record<string,unknown>):void{const mode=this.mode();if(!mode||!this.canManage())return;
    this.facade.resize(mode,{newBays:Number(value['newBays']),newRows:Number(value['newRows']),newTiers:Number(value['newTiers'])}).subscribe({next:()=>{this.mode.set(null);this.notify.success(mode==='expand'?'Đã mở rộng bãi.':'Đã thu hẹp bãi.');},error:()=>{}});
  }
  moveFields():Field[]{return[{key:'containerId',label:'Container',required:true,options:this.block()?.isVirtual?this.facade.virtual().map(c=>({value:c.id,label:c.containerNumber})):this.cards().map(c=>({value:c.id,label:c.number}))},
    {key:'targetBlock',label:'Block đích',required:true,options:this.facade.blocks().filter(b=>!b.isVirtual).map(b=>({value:b.id,label:b.code+' · '+b.name}))},
    {key:'bay',label:'Bay đích',type:'number',required:true,min:1},{key:'row',label:'Row đích (số)',type:'number',required:true,min:1},{key:'tier',label:'Tier đích',type:'number',required:true,min:1}];}
  moveValue(){return{containerId:this.facade.selectedContainer(),targetBlock:this.block()?.isVirtual?'':this.facade.blockId(),bay:1,row:1,tier:1};}
  move(value:Record<string,unknown>):void{
    if(!this.canManage())return;
    const id=String(value['containerId']),target=this.facade.blocks().find(block=>block.id===value['targetBlock']);
    if(!target)return;
    const position={bay:Number(value['bay']),row:Number(value['row']),tier:Number(value['tier'])};
    const source=this.cards().find(card=>card.id===id),virtual=this.block()?.isVirtual;
    if(!virtual&&(!source||target.id!==this.facade.blockId())){this.notify.error('Di chuyển container vật lý trong cùng block. Với container chưa xếp, dùng phân vị trí.');return;}
    const command=virtual?this.facade.fromVirtual(this.block()!.code,id,{targetBlockCode:target.code,...position})
      :this.facade.relocate({blockId:target.id,sourceBay:source!.bay,sourceRow:source!.row,sourceTier:source!.tier,targetBay:position.bay,targetRow:position.row,targetTier:position.tier});
    command.subscribe({next:()=>{this.moving.set(false);this.notify.success('Đã cập nhật vị trí.');this.choose(target.id,position.tier);},error:()=>{}});
  }
}
