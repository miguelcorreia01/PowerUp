import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';

import { Dashboard } from './dashboard';
import { AuthService } from '../../../../core/auth/auth.service';
import { DashboardService } from '../../../dashboard/services/dashboard.service';

describe('Dashboard', () => {
  let component: Dashboard;
  let fixture: ComponentFixture<Dashboard>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockDashboardService: jasmine.SpyObj<DashboardService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['currentUser$']);
    authServiceSpy.currentUser$.and.returnValue(of({ name: 'John Doe', email: 'john@example.com', role: 'Member', userId: '123', token: 'token123' }));

    const dashboardServiceSpy = jasmine.createSpyObj('DashboardService', [
      'getMembership', 
      'getAllGroupClasses', 
      'getAllPtSessions'
    ]);
    
    dashboardServiceSpy.getMembership.and.returnValue(of({
      type: 'Premium',
      expiresIn: 15,
      progress: 75
    }));
    
    dashboardServiceSpy.getAllGroupClasses.and.returnValue(of([]));
    dashboardServiceSpy.getAllPtSessions.and.returnValue(of([]));

    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: DashboardService, useValue: dashboardServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Dashboard);
    component = fixture.componentInstance;
    mockAuthService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    mockDashboardService = TestBed.inject(DashboardService) as jasmine.SpyObj<DashboardService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default values', () => {
    expect(component.membership()).toEqual({
      type: 'Premium',
      expiresIn: 15,
      progress: 75
    });
  });

  it('should compute userName from auth service', () => {
    expect(component.userName()).toBe('John Doe');
  });

  it('should have empty arrays initially', () => {
    expect(component.todaySchedule()).toEqual([]);
    expect(component.todayGroupClasses()).toEqual([]);
  });

  it('should compute group classes summary', () => {
    expect(component.groupClasses()).toEqual({
      enrolled: 0,
      nextClass: '—'
    });
  });

  it('should compute personal training summary', () => {
    expect(component.personalTraining()).toEqual({
      booked: 0,
      nextSession: '—'
    });
  });
});