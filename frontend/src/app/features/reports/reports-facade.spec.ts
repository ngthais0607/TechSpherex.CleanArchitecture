import {TestBed} from '@angular/core/testing';import {signal} from '@angular/core';import {provideHttpClient} from '@angular/common/http';import {provideHttpClientTesting,HttpTestingController} from '@angular/common/http/testing';import {afterEach,describe,expect,it} from 'vitest';
import {AuthService} from '../../core/auth/auth-service';import {DashboardFacade} from '../dashboard/dashboard-facade';import {inventoryView} from './report.mapper';
describe('report sources',()=>{
 afterEach(()=>TestBed.resetTestingModule());
 it('groupTotalIsNotContainerTotal',()=>{const mapped=inventoryView({totalContainers:20,groups:{items:[],totalCount:4} as never});expect(mapped.totalContainers).toBe(20);expect(mapped.totalGroups).toBe(4);});
 it('dashboardDoesNotCountSingleContainerPageAndYardPlannerNeverRequestsExpiringOrders',()=>{
 TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting(),DashboardFacade,{provide:AuthService,useValue:{user:signal({roles:['YardPlanner']})}}]});const facade=TestBed.inject(DashboardFacade);const http=TestBed.inject(HttpTestingController);facade.load();
 http.expectOne('/api/reports/summary').flush({totalContainersInYard:20,totalTeuInYard:20,activeOrdersCount:3,todayGateTransactionsCount:2});http.expectOne('/api/reports/gate-traffic?days=7').flush([]);http.expectOne('/api/reports/block-capacity').flush([]);
 http.expectOne(request=>request.url==='/api/reports/dwell-times').flush({items:[],pageIndex:1,pageSize:100,totalCount:0,totalPages:0});
 http.expectNone('/api/containers');http.expectNone(request=>request.url.includes('expiring-orders'));expect(facade.summary()?.totalContainersInYard).toBe(20);http.verify();
 });
});
