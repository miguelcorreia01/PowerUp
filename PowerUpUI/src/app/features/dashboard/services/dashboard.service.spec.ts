import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';

import { DashboardService } from './dashboard.service';

describe('DashboardService', () => {
  let service: DashboardService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [DashboardService]
    });
    service = TestBed.inject(DashboardService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return membership data', (done) => {
    service.getMembership().subscribe(data => {
      expect(data).toEqual({
        type: 'Premium',
        expiresIn: 15,
        progress: 75
      }); 
      done();
    });
  });

  it('should return group classes', (done) => {
    service.getAllGroupClasses().subscribe(classes => {
      expect(classes.length).toBeGreaterThan(0);
      done();
    });
  });

  it('should return personal training sessions', (done) => {
    service.getAllPtSessions().subscribe(sessions => {
      expect(sessions.length).toBeGreaterThan(0);
      done();
    });
  });
});
