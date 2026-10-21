import {Component,computed,inject} from '@angular/core';import {DatePipe,DecimalPipe} from '@angular/common';import {RouterLink} from '@angular/router';import {DashboardFacade} from './dashboard-facade';import {AuthService} from '../../core/auth/auth-service';import {can} from '../../core/auth/permissions';
@Component({selector:'app-dashboard',imports:[DatePipe,DecimalPipe,RouterLink],providers:[DashboardFacade],templateUrl:'./dashboard-page.html'})
export class DashboardPage{readonly facade=inject(DashboardFacade);readonly auth=inject(AuthService);readonly canReadExpiring=computed(()=>can('readExpiringOrders',this.auth.user()?.roles??[]));constructor(){this.facade.load();}}

